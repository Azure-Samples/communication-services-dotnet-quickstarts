---
page_type: sample
languages:
- csharp
products:
- azure ai foundry
- azure-communication-services
---

# ACS Call Automation and Azure AI Voice Live API 

This sample, discussed at the Microsoft Build 2025 session, demonstrates the integration of Azure Communication Services Call Automation bidirectional streaming with the newly announced Azure AI Voice Live API (Preview). This integration unlocks powerful capabilities for creating next-generation AI voice agents that can be deployed effectively. By combining Azure Communication Services' robust telephony and communication infrastructure with the sophisticated natural language processing and real-time voice capabilities of Azure AI, companies can provide seamless and efficient customer service, automate routine tasks, and deliver personalized experiences. This integration enables scalable solutions that handle diverse communication scenarios, offering significant improvements in customer engagement and operational efficiency.

## Prerequisites

- Create an Azure account with an active subscription. For details, see [Create an account for free](https://azure.microsoft.com/free/)
- Create an Azure Communication Services resource. For details, see [Create an Azure Communication Resource](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource). You'll need to record your resource **connection string** for this sample.
- An Calling-enabled telephone number. [Get a phone number](https://learn.microsoft.com/en-us/azure/communication-services/quickstarts/telephony/get-phone-number?tabs=windows&pivots=platform-azp).
- Azure Dev Tunnels CLI. For details, see  [Enable dev tunnel](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/get-started?tabs=windows)
- Create an [Azure AI Foundry](https://ai.azure.com/) resource.

>[!NOTE]
> ### Azure AI Foundry endpoint
> The Voice Live API is only supported in certain regions. See [supported regions](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/regions?tabs=voice-live#regions) for more details.
>#### Regional endpoints
>If you are using a regional endpoint for your Azure AI Services resource, the VA WebSocket endpoint would be `https://<region>.api.cognitive.microsoft.com/`.
>#### Custom domains
>If you have an Azure AI Foundry resource with a custom domain, where the endpoint shown in Azure portal is `https://<custom-domain>.cognitiveservices.azure.com/`.

## Setup Instructions

Before running this sample, you'll need to setup the resources above with the following configuration updates:

### 1. Setup and host your Azure DevTunnel

[Azure DevTunnels](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/overview) is an Azure service that enables you to share local web services hosted on the internet. Use the commands below to connect your local development environment to the public internet. This creates a tunnel with a persistent endpoint URL and which allows anonymous access. We will then use this endpoint to notify your application of calling events from the ACS Call Automation service.

```bash
devtunnel create --allow-anonymous
devtunnel port create -p 49412
devtunnel host
```

### 2. Add the required API Keys and endpoints
Open the appsettings.json file to configure the following settings:

    - `DevTunnelUri`: your dev tunnel endpoint
    - `AcsConnectionString`: Azure Communication Service resource's connection string.
    - `AzureVoiceLiveApiKey`: Azure AI Foundry Key. Refer to prerequisites section.
    - `AzureVoiceLiveEndpoint`: Azure AI Foundry endpoint. Your endpoint should be like https://{AI_RESOURCE_NAME}.services.ai.azure.com/. Refer to the prerequisites section.
    - `VoiceLiveModel`: The model name. Refer to prerequisites section.

## Running the application

1. Azure DevTunnel: Ensure your AzureDevTunnel URI is active and points to the correct port of your localhost application
2. Run `dotnet run` to build and run the sample application
3. Register an EventGrid Webhook for the IncomingCall Event that points to your DevTunnel URI. Instructions [here](https://learn.microsoft.com/en-us/azure/communication-services/concepts/call-automation/incoming-call-notification).


Once that's completed you should have a running application. The best way to test this is to place a call to your ACS phone number and talk to your intelligent agent.

