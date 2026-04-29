---
page_type: sample
languages:
- csharp
products:
- open ai
- azure-communication-services
---

# ACS Call Automation and Azure OpenAI Service

This is a sample application demonstrated during Microsoft Ignite 2024. It highlights an integration of Azure Communication Services with Azure OpenAI Service to enable intelligent conversational agents.

## Prerequisites

- Create an Azure account with an active subscription. For details, see [Create an account for free](https://azure.microsoft.com/free/)
- Create an Azure Communication Services resource. For details, see [Create an Azure Communication Resource](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource). You'll need to record your resource **connection string** for this sample.
- An Calling-enabled telephone number. [Get a phone number](https://learn.microsoft.com/en-us/azure/communication-services/quickstarts/telephony/get-phone-number?tabs=windows&pivots=platform-azp).
- Azure OpenAI Resource: Set up an Azure OpenAI resource by following the instructions in [Create and deploy an Azure OpenAI Service resource.](https://learn.microsoft.com/en-us/azure/ai-services/openai/how-to/create-resource?pivots=web-portal).
- Azure OpenAI Service Model: To use this sample, you must have the GPT-4o-Realtime-Preview model deployed. Follow the instructions at [GPT-4o Realtime API for speech and audio (Preview)](https://learn.microsoft.com/en-us/azure/ai-services/openai/realtime-audio-quickstart?tabs=keyless%2Cwindows&pivots=ai-foundry-portal) to set it up. 

## Setup Instructions

Before running this sample, you'll need to setup the resources above with the following configuration updates:

### Option A: Deploy to Azure App Service (Recommended for Production)

No Dev Tunnel is needed — Azure App Service provides a public URL that receives EventGrid webhooks directly.

##### 1. Provision Azure Resources

Use the included provisioning script (requires [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli)):

```powershell
az login

cd CallAutomation_AzOpenAI_Voice/deploy

.\create-azure-resources.ps1 `
    -ResourceGroupName "rg-callautomation" `
    -Location "eastus" `
    -AppName "my-call-bot"
```

This creates a Resource Group, Windows App Service Plan (B1), and App Service with:
- 64-bit platform, WebSockets enabled, Always On
- All required Application Settings as placeholders
- Health check configured at `/api/health`

> **Note:** Must use Windows App Service — the Sydney Media SDK requires Windows x64 native binaries. Minimum tier is B1 (free/shared tiers don't support WebSockets or 64-bit).

##### 2. Configure Application Settings

Update the placeholder values with your real credentials:

```powershell
az webapp config appsettings set `
    --name "my-call-bot" `
    --resource-group "rg-callautomation" `
    --settings `
        AcsConnectionString="endpoint=https://..." `
        MediaSDKConnectionString="your-media-sdk-connection-string" `
        AzureOpenAIServiceKey="your-openai-key" `
        AzureOpenAIServiceEndpoint="https://{AI_RESOURCE_NAME}.services.ai.azure.com/" `
        AzureOpenAIDeploymentModelName="gpt-4o-realtime-preview"
```

Or set them in the Azure Portal: App Service > Configuration > Application settings.

| Setting | Description |
|---------|-------------|
| `AppBaseUrl` | Your App Service URL (auto-set by the script) |
| `AcsConnectionString` | Azure Communication Service resource's connection string |
| `MediaSDKConnectionString` | Media SDK connection string |
| `AzureOpenAIServiceKey` | Azure OpenAI Service Key |
| `AzureOpenAIServiceEndpoint` | Azure OpenAI endpoint |
| `AzureOpenAIDeploymentModelName` | Azure OpenAI model name |
| `SystemPrompt` | (Optional) Custom system prompt for the AI assistant |

##### 3. Deploy the Application

**Option 1: Manual deploy (one command)**

```powershell
cd CallAutomation_AzOpenAI_Voice/deploy

.\deploy.ps1 -AppName "my-call-bot" -ResourceGroupName "rg-callautomation"
```

This publishes for win-x64, creates a zip, and deploys to App Service.

**Option 2: GitHub Actions CI/CD**

1. Download a publish profile from Azure Portal: App Service > Deployment Center > Manage publish profile
2. Add it as a GitHub secret named `AZURE_WEBAPP_PUBLISH_PROFILE`
3. Update `AZURE_WEBAPP_NAME` in `.github/workflows/deploy.yml`
4. Push to `main` branch — the workflow builds and deploys automatically

**Option 3: Manual dotnet publish**

```bash
dotnet publish -c Release -r win-x64 --self-contained false
```

Then deploy the output folder via Azure Portal, VS Code, or `az webapp deploy`.

##### 4. Register EventGrid Webhook

Register an EventGrid Webhook for the IncomingCall Event:

1. Go to your **ACS resource** in the Azure Portal
2. Navigate to **Events** > **+ Event Subscription**
3. Set:
   - **Event Types**: Select `Incoming Call`
   - **Endpoint Type**: Web Hook
   - **Endpoint URL**: `https://my-call-bot.azurewebsites.net/api/incomingCall`
4. Click **Create**

The webhook validation handshake is handled automatically by the application.

For detailed instructions, see [Incoming Call notification](https://learn.microsoft.com/en-us/azure/communication-services/concepts/call-automation/incoming-call-notification).

### Option B: Local Development (with Dev Tunnel)

##### 1. Setup and host your Azure DevTunnel

[Azure DevTunnels](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/overview) is an Azure service that enables you to share local web services hosted on the internet. Use the commands below to connect your local development environment to the public internet. This creates a tunnel with a persistent endpoint URL and which allows anonymous access. We will then use this endpoint to notify your application of calling events from the ACS Call Automation service.

```bash
devtunnel create --allow-anonymous
devtunnel port create -p 49412
devtunnel host
```

##### 2. Add the required API Keys and endpoints
Open the appsettings.json file to configure the following settings:

    - `AppBaseUrl`: your dev tunnel endpoint (or App Service URL when deployed)
    - `AcsConnectionString`: Azure Communication Service resource's connection string.
    - `MediaSDKConnectionString`: Media SDK connection string.
    - `AzureOpenAIServiceKey`: Open AI's Service Key. Refer to prerequisites section.
    - `AzureOpenAIServiceEndpoint`: OpenAI's service endpoint. Your endpoint should be like https://{AI_RESOURCE_NAME}.services.ai.azure.com/. Refer to the prerequisites section.
    - `AzureOpenAIDeploymentModelName`: Open AI's Model name. Refer to prerequisites section.

## Running the application

1. If running locally: Ensure your AzureDevTunnel URI is active and points to the correct port of your localhost application
2. Run `dotnet run` to build and run the sample application
3. Register an EventGrid Webhook for the IncomingCall Event that points to your AppBaseUrl. Instructions [here](https://learn.microsoft.com/en-us/azure/communication-services/concepts/call-automation/incoming-call-notification).

Once that's completed you should have a running application. The best way to test this is to place a call to your ACS phone number and talk to your intelligent agent.

## Web Dashboard

The application includes a built-in web dashboard:

| Page | URL | Description |
|------|-----|-------------|
| **Active Calls** | `/` | Real-time view of ongoing calls with auto-refresh |
| **Call Logs** | `/Logs` | History of completed calls with transcripts |
| **Call Detail** | `/CallDetail?id={connectionId}` | Live transcript view for a specific call |
| **Health** | `/Health` | Service connectivity status (ACS, Media SDK, OpenAI) |
| **Health API** | `/api/health` | JSON health check endpoint for monitoring |