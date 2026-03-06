# AppInsights Analyser

A .NET 10 Blazor Server application that connects to your Azure Application Insights instances and renders analytics dashboards — without leaving your browser.

## Features

- Browse all Azure subscriptions and Application Insights instances you have access to
- 9-tab analytics dashboard: Requests, Dependencies, Exceptions, Page Views, Availability, Traces, Top Failures, and Performance
- Drilldown panels: click a row to see individual occurrences, failed requests, exception details, and performance percentiles
- Sortable tables on every tab
- Time range selector (1h, 6h, 24h, 7d, 30d)

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli)
- An Azure account with access to one or more subscriptions containing Application Insights resources

## Getting Started

1. **Login to Azure:**
   ```bash
   az login
   ```

2. **Clone and run:**
   ```bash
   git clone https://github.com/your-username/AppInsightsAnalyser.git
   cd AppInsightsAnalyser
   dotnet run
   ```

3. Open your browser at `https://localhost:7049` (or the URL shown in the terminal).

4. Select a subscription, pick an Application Insights instance, and click **Open Dashboard**.

## Authentication

The app uses `DefaultAzureCredential`, prioritising `AzureCliCredential`. No secrets or connection strings are required — access is granted through your existing `az login` session.

## Tech Stack

| Library | Version |
|---|---|
| .NET / Blazor Server | 10.0 |
| MudBlazor | 7.15.0 |
| Blazor-ApexCharts | 2.0.0 |
| Azure.Identity | 1.13.2 |
| Azure.Monitor.Query | 1.7.1 |
| Azure.ResourceManager | 1.13.0 |

## License

MIT
