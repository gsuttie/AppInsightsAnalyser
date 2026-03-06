namespace AppInsightsAnalyser.Models;

public record AppInsightsInstance(
    string Name,
    string ResourceId,
    string? WorkspaceResourceId,
    string Location);
