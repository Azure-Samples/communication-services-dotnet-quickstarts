---
page_type: sample
languages:
- csharp
products:
- azure-cognitive-services
- azure-communication-services
---

# ACS Call Automation and Azure Cognitive Services with Bot Integration

This is a sample application that demonstrates the integration of **Azure Communication Services (ACS)** with **Azure Cognitive Services** and a bot using the **Direct Line API**. It enables real-time transcription of calls and interaction with a bot, with responses played back to the caller using SSML (Speech Synthesis Markup Language).

## Prerequisites

- **Azure Account**: Create an Azure account with an active subscription. For details, see [Create an account for free](https://azure.microsoft.com/free/).
- **Azure Communication Services Resource**: Create an ACS resource. For details, see [Create an Azure Communication Resource](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource). Record your resource **connection string** for this sample.
- **Calling-Enabled Phone Number**: Obtain a phone number. For details, see [Get a phone number](https://learn.microsoft.com/en-us/azure/communication-services/quickstarts/telephony/get-phone-number?tabs=windows&pivots=platform-azp).
- **Azure Cognitive Services Resource**: Set up a Cognitive Services resource. For details, see [Create a Cognitive Services resource](https://learn.microsoft.com/en-us/azure/cognitive-services/cognitive-services-apis-create-account).
- **Bot Framework**: Create a bot and enable the **Direct Line channel**. Obtain the **Direct Line secret**.
- **Azure Dev Tunnels CLI**: Install and configure Azure Dev Tunnels. For details, see [Enable dev tunnel](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/get-started?tabs=windows).

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
Update the following values in the `appsettings.json` file or `Program.cs`:

- `AcsConnectionString`: The connection string for your Azure Communication Services resource.
- `CognitiveServiceEndpoint`: The endpoint for your Azure Cognitive Services resource.
- `AgentPhoneNumber`: The phone number associated with your ACS resource.
- `DirectLineSecret`: The Direct Line secret for your bot.
- `BaseUri`: The DevTunnel URI (e.g., `https://{DevTunnelUri}`).

## Running the application

1. Azure DevTunnel: Ensure your AzureDevTunnel URI is active and points to the correct port of your localhost application
2. Run `dotnet run` to build and run the sample application
3. Register an EventGrid Webhook for the IncomingCall Event that points to your DevTunnel URI. Instructions [here](https://learn.microsoft.com/en-us/azure/communication-services/concepts/call-automation/incoming-call-notification).


Once that's completed you should have a running application. The best way to test this is to place a call to your ACS phone number and talk to your intelligent agent.