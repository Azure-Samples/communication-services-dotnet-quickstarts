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
- Azure Dev Tunnels CLI. For details, see  [Enable dev tunnel](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/get-started?tabs=windows)
- Azure OpenAI Resource: Set up an Azure OpenAI resource by following the instructions in [Create and deploy an Azure OpenAI Service resource.](https://learn.microsoft.com/en-us/azure/ai-services/openai/how-to/create-resource?pivots=web-portal).
- Azure OpenAI Service Model: To use this sample, you must have the GPT-4o-Realtime-Preview model deployed. Follow the instructions at [GPT-4o Realtime API for speech and audio (Preview)](https://learn.microsoft.com/en-us/azure/ai-services/openai/realtime-audio-quickstart?tabs=keyless%2Cwindows&pivots=ai-foundry-portal) to set it up. 

## Setup Instructions

Before running this sample, you'll need to setup the resources above with the following configuration updates:

##### 1. Setup and host your Azure DevTunnel

[Azure DevTunnels](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/overview) is an Azure service that enables you to share local web services hosted on the internet. Use the commands below to connect your local development environment to the public internet. This creates a tunnel with a persistent endpoint URL and which allows anonymous access. We will then use this endpoint to notify your application of calling events from the ACS Call Automation service.

```bash
devtunnel create --allow-anonymous
devtunnel port create -p 49412
devtunnel host
```

##### 2. Add the required API Keys and endpoints
Open the appsettings.json file to configure the following settings:

    - `DevTunnelUri`: your dev tunnel endpoint
    - `AcsConnectionString`: Azure Communication Service resource's connection string.
    - `AzureOpenAIServiceKey`: Open AI's Service Key. Refer to prerequisites section.
    - `AzureOpenAIServiceEndpoint`: Open AI's Service Endpoint. Refer to prerequisites section.
    - `AzureOpenAIDeploymentModelName`: Open AI's Model name. Refer to prerequisites section.

## Running the application

1. Azure DevTunnel: Ensure your AzureDevTunnel URI is active and points to the correct port of your localhost application
2. Run `dotnet run` to build and run the sample application
3. Register an EventGrid Webhook for the IncomingCall Event that points to your DevTunnel URI. Instructions [here](https://learn.microsoft.com/en-us/azure/communication-services/concepts/call-automation/incoming-call-notification).


Once that's completed you should have a running application. The best way to test this is to place a call to your ACS phone number and talk to your intelligent agent.