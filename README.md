# AppInsights Analyser

A .NET 10 Blazor Server application that connects to your Azure Application Insights instances and renders rich analytics dashboards — directly in your browser, with no API keys or connection strings required.

![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)
![Blazor Server](https://img.shields.io/badge/Blazor-Server-7B2FBE)
![MudBlazor](https://img.shields.io/badge/MudBlazor-7.15.0-594AE2)
![License](https://img.shields.io/badge/license-MIT-green)

---

## Features

- **Subscription & instance picker** — browse all Azure subscriptions and Application Insights workspaces you have access to
- **Left-hand sidebar navigation** — jump between dashboard sections instantly without page reloads
- **Time range selector** — analyse data over 1 hour, 6 hours, 24 hours, 7 days, or 30 days
- **Sortable tables** — every column on every table is sortable
- **Drilldown panels** — click rows or counts to see individual occurrences inline

### Dashboard Sections

| Section | What it shows |
|---|---|
| **Overview** | Summary cards: total requests, exceptions, availability %, page views, dependencies |
| **Requests** | Request volume, failure count, average duration per operation. Click failed count to see individual failed requests |
| **Dependencies** | Outbound dependency calls grouped by name and type with average duration |
| **Exceptions** | Exception types ranked by frequency. Click a row to see individual occurrence details |
| **Page Views** | Page view counts and average load times per page |
| **Page Speed** | Side-by-side comparison of today's vs yesterday's page load times with % change |
| **Availability** | Availability test pass rates across all configured tests |
| **Traces** | Trace/log volume broken down by severity level |
| **Top Failures** | Most frequent failed request + result code combinations. Click failure count to drill into occurrences |
| **Performance** | Slowest operations ranked by P95 duration. Click a row for percentiles (P50–P99), recent occurrences, and dependency breakdown |

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli)
- An Azure account with read access to one or more subscriptions containing Application Insights resources

---

## Getting Started

### 1. Login to Azure

If your account belongs to a specific tenant, include your tenant ID:

```bash
az login --tenant <your-tenant-id>
```

For example:

```bash
az login --tenant 00000000-0000-0000-0000-000000000000
```

If you only have one tenant (or want to use your default), you can omit it:

```bash
az login
```

### 2. Clone the repository

```bash
git clone https://github.com/gsuttie/AppInsightsAnalyser.git
cd AppInsightsAnalyser
```

### 3. Run the application

```bash
dotnet run
```

### 4. Open in your browser

Navigate to the URL shown in the terminal output, typically:

```
https://localhost:7049
```

> **Note:** The app uses a self-signed development certificate. If your browser warns about the certificate, you can trust it by running `dotnet dev-certs https --trust` once.

### 5. Select your instance

1. Choose a **subscription** from the dropdown
2. Choose an **Application Insights instance**
3. Click **Open Dashboard**
4. Use the left-hand sidebar to navigate between sections

---

## Authentication

The app uses a `ChainedTokenCredential` that tries authentication methods in this order:

1. **Azure CLI** (`AzureCliCredential`) — uses your active `az login` session
2. **DefaultAzureCredential** (excluding Azure CLI, to avoid duplication) — covers Managed Identity, Visual Studio, environment variables, workload identity, etc.

No secrets, connection strings, or API keys are stored anywhere. Access is entirely driven by your Azure RBAC permissions.

### Required Azure Permissions

Your account needs the following permissions on each subscription / resource you want to analyse:

| Permission | Purpose |
|---|---|
| `Reader` on the subscription | List subscriptions and Application Insights resources |
| `Monitoring Reader` on the Application Insights resource | Execute KQL log queries |

---

## Project Structure

```
AppInsightsAnalyser/
├── Components/
│   ├── App.razor                   # HTML shell, MudBlazor CSS, ApexCharts JS
│   ├── _Imports.razor              # Global usings and MudBlazor aliases
│   ├── Layout/
│   │   └── MainLayout.razor        # App shell with sidebar navigation
│   ├── Pages/
│   │   ├── Home.razor              # Subscription + instance picker
│   │   └── Dashboard.razor         # All 10 dashboard sections
│   └── Shared/
│       ├── SummaryCard.razor       # Overview metric card component
│       └── EmptyState.razor        # No-data placeholder component
├── Models/
│   ├── AppInsightsInstance.cs      # Azure resource model
│   ├── SubscriptionInfo.cs         # Subscription model
│   └── QueryResults.cs             # All KQL result record types
├── Services/
│   ├── AppStateService.cs          # Scoped state: selected instance + active section
│   ├── AzureResourceService.cs     # Lists subscriptions and AI instances via ARM
│   └── AppInsightsQueryService.cs  # All KQL queries via Azure Monitor Query SDK
├── Program.cs                      # Service registration and app startup
├── appsettings.json
└── AppInsightsAnalyser.csproj
```

---

## Tech Stack

| Library | Version | Purpose |
|---|---|---|
| .NET / Blazor Server | 10.0 | Application framework |
| MudBlazor | 7.15.0 | UI component library |
| Blazor-ApexCharts | 2.0.0 | Charts and graphs |
| Azure.Identity | 1.13.2 | Authentication via Azure CLI / DefaultAzureCredential |
| Azure.Monitor.Query | 1.7.1 | KQL log queries against Application Insights |
| Azure.ResourceManager | 1.13.0 | ARM client for listing subscriptions |
| Azure.ResourceManager.ApplicationInsights | 1.0.1 | Listing Application Insights instances |

---

## Troubleshooting

**"No subscriptions found"**
- Make sure you are logged in: `az account list`
- If you have multiple tenants, re-login with the correct tenant: `az login --tenant <tenant-id>`

**"No Application Insights instances found"**
- Ensure the selected subscription contains Application Insights resources
- Check you have at least `Reader` role on the subscription

**"Failed to load data" errors on dashboard**
- Ensure your account has `Monitoring Reader` on the Application Insights resource
- Some sections (e.g. Availability) will show no data if no availability tests are configured

**Certificate warning in browser**
```bash
dotnet dev-certs https --trust
```

**Port already in use**
- Edit `Properties/launchSettings.json` (excluded from git) to change the port, or run:
```bash
dotnet run --urls "https://localhost:7050"
```

---

## Contributing

Contributions are welcome. Please open an issue before submitting a pull request so the change can be discussed first.

---

## License

MIT — see [LICENSE](LICENSE) for details.
