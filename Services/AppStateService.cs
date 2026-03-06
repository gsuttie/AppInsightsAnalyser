using AppInsightsAnalyser.Models;

namespace AppInsightsAnalyser.Services;

public class AppStateService
{
    public string? SelectedSubscriptionId { get; private set; }
    public string? SelectedSubscriptionName { get; private set; }
    public AppInsightsInstance? SelectedInstance { get; private set; }

    public event Action? OnChange;

    public void SetSubscription(string id, string name)
    {
        SelectedSubscriptionId = id;
        SelectedSubscriptionName = name;
        SelectedInstance = null;
        NotifyStateChanged();
    }

    public string ActiveSection { get; private set; } = "overview";

    public void SetSection(string section)
    {
        ActiveSection = section;
        NotifyStateChanged();
    }

    public void SetInstance(AppInsightsInstance instance)
    {
        SelectedInstance = instance;
        ActiveSection = "overview";
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
