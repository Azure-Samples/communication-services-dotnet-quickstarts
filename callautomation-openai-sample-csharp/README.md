---
page_type: sample
languages:
- csharp
products:
- azure
- azure-functions
- azure-communication-services
---

# ACS Call Automation and Azure OpenAI Service

This is a sample application demonstrated during Microsoft /Build 2023. It highlights an integration of Azure Communication Services with Azure OpenAI Service to enable intelligent conversational agents.

## Prerequisites

- Create an Azure account with an active subscription. For details, see [Create an account for free](https://azure.microsoft.com/free/)
- Create an Azure Communication Services resource. For details, see [Create an Azure Communication Resource](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource). You'll need to record your resource **connection string** for this sample.
- An Calling-enabled telephone number. [Get a phone number](https://learn.microsoft.com/en-us/azure/communication-services/quickstarts/telephony/get-phone-number?tabs=windows&pivots=platform-azp).

- Azure Dev Tunnels CLI. For details, see  [Enable dev tunnel](https://docs.tunnels.api.visualstudio.com/cli)
- Create an Azure Cognitive Services resource. For details, see [Create an Azure Cognitive Services Resource](https://learn.microsoft.com/en-us/azure/cognitive-services/cognitive-services-apis-create-account)
- An Azure OpenAI Resource and Deployed Model. See [instructions](https://learn.microsoft.com/en-us/azure/cognitive-services/openai/how-to/create-resource?pivots=web-portal).


## Setup Instructions

Before running this sample, you'll need to setup the resources above with the following configuration updates:

1. Startup your Azure DevTunnel instance and note your DevTunnel URI, add it to the `devTunnelUri` variable in `Program.cs`
3. Add a Managed Identity to the ACS Resource that connects to the Cognitive Services resource. See [instructions](https://learn.microsoft.com/en-us/azure/communication-services/concepts/call-automation/azure-communication-services-azure-cognitive-services-integration).
4. Add the required API Keys and endpoints to appsettings.json


## Running the application

1. Azure DevTunnel: Ensure your AzureDevTunnel URI is active and points to the correct port of your localhost application
2. Run `dotnet run` to build and run the sample application
3. Register an EventGrid Webhook for the IncomingCall Event that points to your DevTunnel URI. Instructions [here](https://learn.microsoft.com/en-us/azure/communication-services/concepts/call-automation/incoming-call-notification).


Once that's completed you should have a running application. The best way to test this is to place a call to your ACS phone number and talk to your intelligent agent.
