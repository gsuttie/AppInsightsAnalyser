using Azure.Core;
using Azure.Identity;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using AppInsightsAnalyser.Models;

namespace AppInsightsAnalyser.Services;

public class AppInsightsQueryService
{
    private readonly LogsQueryClient _logsClient;

    public AppInsightsQueryService()
    {
        var credential = new ChainedTokenCredential(
            new AzureCliCredential(),
            new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ExcludeAzureCliCredential = true
            }));
        _logsClient = new LogsQueryClient(credential);
    }

    private static QueryTimeRange GetQueryTimeRange(string range) => range switch
    {
        "1h"  => new QueryTimeRange(TimeSpan.FromHours(1)),
        "6h"  => new QueryTimeRange(TimeSpan.FromHours(6)),
        "24h" => new QueryTimeRange(TimeSpan.FromHours(24)),
        "7d"  => new QueryTimeRange(TimeSpan.FromDays(7)),
        "30d" => new QueryTimeRange(TimeSpan.FromDays(30)),
        _     => new QueryTimeRange(TimeSpan.FromHours(24)),
    };

    private static string ToKqlTimeRange(string range) => range switch
    {
        "1h"  => "1h",
        "6h"  => "6h",
        "24h" => "24h",
        "7d"  => "7d",
        "30d" => "30d",
        _     => "24h",
    };

    private async Task<LogsQueryResult?> QueryAsync(string resourceId, string kql, string timeRange)
    {
        var options = new LogsQueryOptions { ServerTimeout = TimeSpan.FromSeconds(60) };
        var response = await _logsClient.QueryResourceAsync(
            new ResourceIdentifier(resourceId),
            kql,
            GetQueryTimeRange(timeRange),
            options);
        return response.Value;
    }

    private async Task<LogsQueryResult?> QueryAsync(string resourceId, string kql, QueryTimeRange timeRange)
    {
        var options = new LogsQueryOptions { ServerTimeout = TimeSpan.FromSeconds(60) };
        var response = await _logsClient.QueryResourceAsync(
            new ResourceIdentifier(resourceId),
            kql,
            timeRange,
            options);
        return response.Value;
    }

    public async Task<List<DependencyResult>> GetDependenciesAsync(string resourceId, string timeRange, bool excludeSql = true)
    {
        var sqlFilter = excludeSql ? "| where type !~ \"SQL\"" : "";
        var kql = $@"
dependencies
| where timestamp > ago({ToKqlTimeRange(timeRange)})
{sqlFilter}
| summarize count=count(), avgDuration=avg(duration) by name, type
| top 20 by count desc";

        var result = await QueryAsync(resourceId, kql, timeRange);
        if (result?.Table is null) return [];

        return result.Table.Rows.Select(row => new DependencyResult(
            row.GetString("name") ?? "",
            row.GetString("type") ?? "",
            row.GetInt64("count") ?? 0,
            row.GetDouble("avgDuration") ?? 0)).ToList();
    }

    public async Task<List<PageViewResult>> GetPageViewsAsync(string resourceId, string timeRange)
    {
        var kql = $@"
pageViews
| where timestamp > ago({ToKqlTimeRange(timeRange)})
| summarize count=count(), avgDuration=avg(duration) by name
| top 20 by count desc";

        var result = await QueryAsync(resourceId, kql, timeRange);
        if (result?.Table is null) return [];

        return result.Table.Rows.Select(row => new PageViewResult(
            row.GetString("name") ?? "",
            row.GetInt64("count") ?? 0,
            row.GetDouble("avgDuration") ?? 0)).ToList();
    }

    public async Task<List<PageSpeedResult>> GetPageSpeedComparisonAsync(string resourceId)
    {
        var window = new QueryTimeRange(TimeSpan.FromHours(49));

        var todayTask = QueryAsync(resourceId, @"
pageViews
| where timestamp > ago(24h)
| summarize TodayAvg=avg(duration) by name", window);

        var yesterdayTask = QueryAsync(resourceId, @"
pageViews
| where timestamp between(ago(48h) .. ago(24h))
| summarize YesterdayAvg=avg(duration) by name", window);

        await Task.WhenAll(todayTask, yesterdayTask);

        var today = todayTask.Result?.Table?.Rows
            .ToDictionary(r => r.GetString("name") ?? "", r => r.GetDouble("TodayAvg") ?? 0)
            ?? new Dictionary<string, double>();

        var yesterday = yesterdayTask.Result?.Table?.Rows
            .ToDictionary(r => r.GetString("name") ?? "", r => r.GetDouble("YesterdayAvg") ?? 0)
            ?? new Dictionary<string, double>();

        return today.Keys.Union(yesterday.Keys).OrderBy(n => n).Select(name =>
        {
            var t = today.TryGetValue(name, out var tv) ? (double?)tv : null;
            var y = yesterday.TryGetValue(name, out var yv) ? (double?)yv : null;
            var change = t.HasValue && y.HasValue ? t - y : (double?)null;
            var pct = t.HasValue && y.HasValue && y > 0 ? (t - y) / y * 100 : (double?)null;
            return new PageSpeedResult(name, t, y, change, pct);
        }).ToList();
    }

    public async Task<List<RequestResult>> GetRequestsAsync(string resourceId, string timeRange)
    {
        var kql = $@"
requests
| where timestamp > ago({ToKqlTimeRange(timeRange)})
| summarize count=count(), failed=countif(success==false), avgDuration=avg(duration) by name
| top 20 by count desc";

        var result = await QueryAsync(resourceId, kql, timeRange);
        if (result?.Table is null) return [];

        return result.Table.Rows.Select(row => new RequestResult(
            row.GetString("name") ?? "",
            row.GetInt64("count") ?? 0,
            row.GetInt64("failed") ?? 0,
            row.GetDouble("avgDuration") ?? 0)).ToList();
    }

    public async Task<List<FailedRequestDetail>> GetFailedRequestDetailsAsync(string resourceId, string timeRange, string requestName)
    {
        var escaped = requestName.Replace("'", "''");
        var kql = $@"
requests
| where timestamp > ago({ToKqlTimeRange(timeRange)}) and name == '{escaped}' and success == false
| project timestamp, resultCode, duration, url, operation_Id
| order by timestamp desc
| take 50";

        var result = await QueryAsync(resourceId, kql, timeRange);
        if (result?.Table is null) return [];

        return result.Table.Rows.Select(row => new FailedRequestDetail(
            row.GetDateTimeOffset("timestamp") ?? DateTimeOffset.MinValue,
            row.GetString("resultCode") ?? "",
            row.GetDouble("duration") ?? 0,
            row.GetString("url") ?? "",
            row.GetString("operation_Id") ?? "")).ToList();
    }

    public async Task<List<ExceptionResult>> GetExceptionsAsync(string resourceId, string timeRange)
    {
        var kql = $@"
exceptions
| where timestamp > ago({ToKqlTimeRange(timeRange)})
| summarize count=count() by type, outerMessage
| top 20 by count desc";

        var result = await QueryAsync(resourceId, kql, timeRange);
        if (result?.Table is null) return [];

        return result.Table.Rows.Select(row => new ExceptionResult(
            row.GetString("type") ?? "",
            row.GetString("outerMessage") ?? "",
            row.GetInt64("count") ?? 0)).ToList();
    }

    public async Task<List<ExceptionDetail>> GetExceptionDetailAsync(string resourceId, string timeRange, string exceptionType, string message)
    {
        var escapedType = exceptionType.Replace("'", "''");
        var escapedMsg  = message.Replace("'", "''");
        var kql = $@"
exceptions
| where timestamp > ago({ToKqlTimeRange(timeRange)})
| where type == '{escapedType}' and outerMessage == '{escapedMsg}'
| project timestamp, operation_Name, innermostType, innermostMessage, outerMessage
| top 25 by timestamp desc";

        var result = await QueryAsync(resourceId, kql, timeRange);
        if (result?.Table is null) return [];

        return result.Table.Rows.Select(row => new ExceptionDetail(
            row.GetDateTimeOffset("timestamp") ?? DateTimeOffset.MinValue,
            row.GetString("operation_Name") ?? "",
            row.GetString("innermostType") ?? "",
            row.GetString("innermostMessage") ?? "",
            row.GetString("outerMessage") ?? "")).ToList();
    }

    public async Task<List<AvailabilityResult>> GetAvailabilityAsync(string resourceId, string timeRange)
    {
        var kql = $@"
availabilityResults
| where timestamp > ago({ToKqlTimeRange(timeRange)})
| summarize passed=countif(success==1), total=count() by name";

        var result = await QueryAsync(resourceId, kql, timeRange);
        if (result?.Table is null) return [];

        return result.Table.Rows.Select(row =>
        {
            var passed = row.GetInt64("passed") ?? 0;
            var total = row.GetInt64("total") ?? 0;
            var pct = total > 0 ? Math.Round(100.0 * passed / total, 2) : 0;
            return new AvailabilityResult(
                row.GetString("name") ?? "",
                passed,
                total,
                pct);
        }).ToList();
    }

    public async Task<List<TraceResult>> GetTracesAsync(string resourceId, string timeRange)
    {
        var kql = $@"
traces
| where timestamp > ago({ToKqlTimeRange(timeRange)})
| summarize count=count() by severityLevel
| order by severityLevel asc";

        var result = await QueryAsync(resourceId, kql, timeRange);
        if (result?.Table is null) return [];

        return result.Table.Rows.Select(row =>
        {
            var level = (int)(row.GetInt64("severityLevel") ?? 0);
            var name = level switch
            {
                0 => "Verbose",
                1 => "Information",
                2 => "Warning",
                3 => "Error",
                4 => "Critical",
                _ => $"Level {level}"
            };
            return new TraceResult(level, name, row.GetInt64("count") ?? 0);
        }).ToList();
    }

    public async Task<List<FailureResult>> GetTopFailuresAsync(string resourceId, string timeRange)
    {
        var kql = $@"
requests
| where timestamp > ago({ToKqlTimeRange(timeRange)}) and success == false
| summarize failCount=count() by name, resultCode
| top 10 by failCount desc";

        var result = await QueryAsync(resourceId, kql, timeRange);
        if (result?.Table is null) return [];

        return result.Table.Rows.Select(row => new FailureResult(
            row.GetString("name") ?? "",
            row.GetString("resultCode") ?? "",
            row.GetInt64("failCount") ?? 0)).ToList();
    }

    public async Task<List<FailedRequestDetail>> GetFailureOccurrencesAsync(string resourceId, string timeRange, string requestName, string resultCode)
    {
        var escapedName = requestName.Replace("'", "''");
        var escapedCode = resultCode.Replace("'", "''");
        var kql = $@"
requests
| where timestamp > ago({ToKqlTimeRange(timeRange)}) and name == '{escapedName}' and resultCode == '{escapedCode}' and success == false
| project timestamp, resultCode, duration, url, operation_Id
| order by timestamp desc
| take 50";

        var result = await QueryAsync(resourceId, kql, timeRange);
        if (result?.Table is null) return [];

        return result.Table.Rows.Select(row => new FailedRequestDetail(
            row.GetDateTimeOffset("timestamp") ?? DateTimeOffset.MinValue,
            row.GetString("resultCode") ?? "",
            row.GetDouble("duration") ?? 0,
            row.GetString("url") ?? "",
            row.GetString("operation_Id") ?? "")).ToList();
    }

    public async Task<List<PerformanceResult>> GetTopPerformanceAsync(string resourceId, string timeRange)
    {
        var kql = $@"
requests
| where timestamp > ago({ToKqlTimeRange(timeRange)})
| summarize avgDuration=avg(duration), p95=percentile(duration, 95) by name
| top 10 by avgDuration desc";

        var result = await QueryAsync(resourceId, kql, timeRange);
        if (result?.Table is null) return [];

        return result.Table.Rows.Select(row => new PerformanceResult(
            row.GetString("name") ?? "",
            row.GetDouble("avgDuration") ?? 0,
            row.GetDouble("p95") ?? 0)).ToList();
    }

    public async Task<PerformanceDrilldown> GetPerformanceDrilldownAsync(string resourceId, string timeRange, string requestName)
    {
        var tr = ToKqlTimeRange(timeRange);
        var escaped = requestName.Replace("'", "''");

        var occurrencesTask = QueryAsync(resourceId, $@"
requests
| where timestamp > ago({tr}) and name == '{escaped}'
| project timestamp, duration, resultCode, successInt=tolong(success), url
| order by duration desc
| take 25", timeRange);

        var percentilesTask = QueryAsync(resourceId, $@"
requests
| where timestamp > ago({tr}) and name == '{escaped}'
| summarize P50=percentile(duration,50), P75=percentile(duration,75), P90=percentile(duration,90), P95=percentile(duration,95), P99=percentile(duration,99), TotalCount=count()", timeRange);

        var depsTask = QueryAsync(resourceId, $@"
dependencies
| where timestamp > ago({tr}) and operation_Name == '{escaped}'
| summarize Count=count(), AvgDuration=avg(duration), MaxDuration=max(duration), P95=percentile(duration,95) by type, name
| order by AvgDuration desc
| take 15", timeRange);

        await Task.WhenAll(occurrencesTask, percentilesTask, depsTask);

        var occurrences = occurrencesTask.Result?.Table?.Rows.Select(row => new PerformanceOccurrence(
            row.GetDateTimeOffset("timestamp") ?? DateTimeOffset.MinValue,
            row.GetDouble("duration") ?? 0,
            row.GetString("resultCode") ?? "",
            (row.GetInt64("successInt") ?? 1) == 1,
            row.GetString("url") ?? "")).ToList() ?? [];

        PerformancePercentiles percentiles = new(0, 0, 0, 0, 0, 0);
        var pRow = percentilesTask.Result?.Table?.Rows.FirstOrDefault();
        if (pRow is not null)
            percentiles = new(
                pRow.GetDouble("P50") ?? 0,
                pRow.GetDouble("P75") ?? 0,
                pRow.GetDouble("P90") ?? 0,
                pRow.GetDouble("P95") ?? 0,
                pRow.GetDouble("P99") ?? 0,
                pRow.GetInt64("TotalCount") ?? 0);

        var deps = depsTask.Result?.Table?.Rows.Select(row => new PerformanceDependencySummary(
            row.GetString("type") ?? "",
            row.GetString("name") ?? "",
            row.GetInt64("Count") ?? 0,
            row.GetDouble("AvgDuration") ?? 0,
            row.GetDouble("MaxDuration") ?? 0,
            row.GetDouble("P95") ?? 0)).ToList() ?? [];

        return new PerformanceDrilldown(percentiles, occurrences, deps);
    }

    public async Task<DashboardSummary> GetSummaryAsync(string resourceId, string timeRange)
    {
        var tr = ToKqlTimeRange(timeRange);

        var reqTask = QueryAsync(resourceId,
            $"requests | where timestamp > ago({tr}) | summarize TotalRequests=count(), TotalFailed=countif(success==false)",
            timeRange);
        var excTask = QueryAsync(resourceId,
            $"exceptions | where timestamp > ago({tr}) | summarize TotalExceptions=count()",
            timeRange);
        var pvTask = QueryAsync(resourceId,
            $"pageViews | where timestamp > ago({tr}) | summarize TotalPageViews=count()",
            timeRange);
        var avTask = QueryAsync(resourceId,
            $"availabilityResults | where timestamp > ago({tr}) | summarize AvailabilityPct=round(100.0*countif(success==1)/count(), 2)",
            timeRange);
        var depTask = QueryAsync(resourceId,
            $"dependencies | where timestamp > ago({tr}) | summarize TotalDependencies=count()",
            timeRange);

        await Task.WhenAll(reqTask, excTask, pvTask, avTask, depTask);

        long totalRequests = 0, totalExceptions = 0, totalPageViews = 0, totalDependencies = 0;
        double availabilityPct = 0;

        var reqResult = reqTask.Result;
        if (reqResult?.Table?.Rows.Count > 0)
            totalRequests = reqResult.Table.Rows[0].GetInt64("TotalRequests") ?? 0;

        var excResult = excTask.Result;
        if (excResult?.Table?.Rows.Count > 0)
            totalExceptions = excResult.Table.Rows[0].GetInt64("TotalExceptions") ?? 0;

        var pvResult = pvTask.Result;
        if (pvResult?.Table?.Rows.Count > 0)
            totalPageViews = pvResult.Table.Rows[0].GetInt64("TotalPageViews") ?? 0;

        var avResult = avTask.Result;
        if (avResult?.Table?.Rows.Count > 0)
            availabilityPct = avResult.Table.Rows[0].GetDouble("AvailabilityPct") ?? 0;

        var depResult = depTask.Result;
        if (depResult?.Table?.Rows.Count > 0)
            totalDependencies = depResult.Table.Rows[0].GetInt64("TotalDependencies") ?? 0;

        return new DashboardSummary(totalRequests, totalExceptions, availabilityPct, totalPageViews, totalDependencies);
    }
}
