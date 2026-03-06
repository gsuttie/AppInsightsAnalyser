using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.ApplicationInsights;
using AppInsightsAnalyser.Models;

namespace AppInsightsAnalyser.Services;

public class AzureResourceService
{
    private readonly ArmClient _armClient;

    public AzureResourceService()
    {
        // Use AzureCliCredential first (az login), then fall back to other sources.
        // DefaultAzureCredential on Windows often picks up SharedTokenCacheCredential
        // or VisualStudioCredential before reaching AzureCliCredential, silently
        // authenticating against a different tenant that has no subscriptions.
        var credential = new ChainedTokenCredential(
            new AzureCliCredential(),
            new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ExcludeAzureCliCredential = true
            }));
        _armClient = new ArmClient(credential);
    }

    public async Task<List<SubscriptionInfo>> GetSubscriptionsAsync()
    {
        var subscriptions = new List<SubscriptionInfo>();
        await foreach (var sub in _armClient.GetSubscriptions().GetAllAsync())
        {
            subscriptions.Add(new SubscriptionInfo(sub.Data.SubscriptionId!, sub.Data.DisplayName));
        }
        return subscriptions.OrderBy(s => s.Name).ToList();
    }

    public async Task<List<AppInsightsInstance>> GetAppInsightsInstancesAsync(string subscriptionId)
    {
        var subscription = await _armClient.GetSubscriptions().GetAsync(subscriptionId);
        var instances = new List<AppInsightsInstance>();

        await Task.Run(() =>
        {
            foreach (var component in subscription.Value.GetApplicationInsightsComponents())
            {
                instances.Add(new AppInsightsInstance(
                    component.Data.Name,
                    component.Id!.ToString(),
                    component.Data.WorkspaceResourceId?.ToString(),
                    component.Data.Location.ToString()));
            }
        });

        return instances.OrderBy(i => i.Name).ToList();
    }
}
