---
page_type: sample
languages:
- csharp
products:
- azure
- azure-communication-services
- azure-communication-callAutomation
---

# Incoming call Media Streaming Sample

Get started with audio media streaming out, through Azure Communication Services. This QuickStart assumes you’ve already used the calling automation APIs to build an automated call routing solution, please refer [Incoming call Sample](https://link_of_incoming_call_sample/).

In this sample a WebApp receives an incoming call request whenever a call is made to a phone number or a communication identifier. API first answers the call and makes PSTN external user say something.
The audio is streamed to WebSocket server and generates log events to show media streaming is happening on the server.
It supports Audio streaming only (mixed/unmixed format).

This sample has 3 parts:
1. ACS Resource IncomingCall Hook Settings, and ACS Phone Number.
2. IncomingCall WebApp - for accepting the incoming call with Media Options settings.
3. WebSocketListener – Listen to media stream on websocket.

The application is an app service application built on .NET6.0.

## Prerequisites

- Create an Azure account with an active subscription. For details, see [Create an account for free](https://azure.microsoft.com/free/)
- [Visual Studio (2022 and above)](https://visualstudio.microsoft.com/vs/)
- [.NET6](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48) (Make sure to install version that corresponds with your visual studio instance, 32 vs 64 bit)
- Create an Azure Communication Services resource. For details, see [Create an Azure Communication Resource](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource). You'll need to record your resource **connection string** for this sample.
- [Configuring the webhook](https://docs.microsoft.com/en-us/azure/devops/service-hooks/services/webhooks?view=azure-devops) for **Microsoft.Communication.IncomingCall** event.


## Before running the sample for the first time

1. Open an instance of PowerShell, Windows Terminal, Command Prompt or equivalent and navigate to the directory that you'd like to clone the sample to.
2. git clone https://github.com/Azure-Samples/Communication-Services-dotnet-quickstarts.git.

### Locally running the media streaming WebSocket app
1. Go to IncomingCallMediaStreaming folder and open `IncomingCallMediaStreaming.sln` solution in Visual Studio.
2. Select and run the `WebSocketListener` project, an application for listening media stream on websocket.
3. Used the ngrok URl, get from `WebSocketListener` app, as a websocket URL needed for `MediaStreamingTransportURI` configuration.

### Publish  the Incoming call media streaming to Azure WebApp

1. Right click the `IncomingCallMediaStreaming` project and select Publish.
2. Create a new publish profile and select your app name, Azure subscription, resource group etc. (choose any unique name, as this URL needed for `AppCallBackUri` configuration settings)
3. After publishing, add the following configurations on azure portal (under app service's configuration section).

	- ResourceConnectionString: Azure Communication Service resource's connection string.
	- AppCallBackUri: URI of the deployed app service.
	- SecretValue: Query string for callback URL.
	- MediaStreamingTransportURI: websocket URL got from `WebSocketListener`, format "wss://{ngrok-URL}",(Notice the url, it should wss:// and not https://)

### Create Webhook for incoming call event
1. Configure webhook from ACS events tab for incoming call event.
 	- Manually configuring the webhook using this command. use above published "'https://<IncomingCallMediaStreaming-URL>/OnIncomingCall" URL as webhook URL.

	```
	armclient put "/subscriptions/<Subscriptin-ID>/resourceGroups/<resource group name>/providers/Microsoft.Communication/CommunicationServices/<CommunicationService Name>/providers/Microsoft.EventGrid/eventSubscriptions/<WebHookName>?api-version=2020-06-01" "{'properties':{'destination':{'properties':{'endpointUrl':'<webhookurl>'},'endpointType':'WebHook'},'filter':{'includedEventTypes': ['Microsoft.Communication.IncomingCall']}}}" -verbose

	```



4. Detailed instructions on publishing the app to Azure are available at [Publish a Web app](https://docs.microsoft.com/visualstudio/deployment/quickstart-deploy-to-azure?view=vs-2019).

**Note**: While you may use http://localhost for local testing, the sample when deployed will only work when served over https. The SDK [does not support http](https://docs.microsoft.com/azure/communication-services/concepts/voice-video-calling/calling-sdk-features#user-webrtc-over-https).

### Troubleshooting

1. Solution doesn't build, it throws errors during build

	Clean/rebuild the C# solution
