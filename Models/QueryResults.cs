namespace AppInsightsAnalyser.Models;

public record DependencyResult(string Name, string Type, long Count, double AvgDuration);

public record PageViewResult(string Name, long Count, double AvgDuration);

public record PageSpeedResult(string Name, double? TodayMs, double? YesterdayMs, double? ChangeMs, double? ChangePct);

public record RequestResult(string Name, long Count, long FailedCount, double AvgDuration);

public record ExceptionResult(string Type, string Message, long Count);

public record ExceptionDetail(
    DateTimeOffset Timestamp,
    string OperationName,
    string InnermostType,
    string InnermostMessage,
    string OuterMessage);

public record AvailabilityResult(string TestName, long PassedCount, long TotalCount, double AvailabilityPct);

public record TraceResult(int SeverityLevel, string SeverityName, long Count);


public record FailedRequestDetail(DateTimeOffset Timestamp, string ResultCode, double Duration, string Url, string OperationId);

public record FailureResult(string Name, string ResultCode, long FailCount);

public record PerformanceResult(string Name, double AvgDuration, double P95Duration);

public record PerformanceOccurrence(DateTimeOffset Timestamp, double Duration, string ResultCode, bool Success, string Url);
public record PerformanceDependencySummary(string Type, string Name, long Count, double AvgDuration, double MaxDuration, double P95Duration);
public record PerformancePercentiles(double P50, double P75, double P90, double P95, double P99, long TotalCount);
public record PerformanceDrilldown(PerformancePercentiles Percentiles, List<PerformanceOccurrence> Occurrences, List<PerformanceDependencySummary> Dependencies);

public record DashboardSummary(
    long TotalRequests,
    long TotalExceptions,
    double AvailabilityPct,
    long TotalPageViews,
    long TotalDependencies);
