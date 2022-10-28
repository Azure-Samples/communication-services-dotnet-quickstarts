---
page_type: sample
languages:
- csharp
products:
- azure
- azure-communication-services
---

# Incoming call Media Streaming Sample

This is a sample web app service, shows how the Azure Communication Services, Call automation SDK can be used to build IVR related solutions. This sample receives an incoming call request whenever a call made to a phone number or a communication identifier. API first answers the call and make PSTN external user say something.
The audio is streamed to WebSocket server and generate log events to show media streaming is happening on the server.
It supports Audio streaming only (mixed/unmixed format).

The application is an app service application built on .NET Framework 6.0.

## Prerequisites

- Create an Azure account with an active subscription. For details, see [Create an account for free](https://azure.microsoft.com/free/)
- [Visual Studio (2022 and above)](https://visualstudio.microsoft.com/vs/)
- [.NET Framework 4.8](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48) (Make sure to install version that corresponds with your visual studio instance, 32 vs 64 bit)
- Create an Azure Communication Services resource. For details, see [Create an Azure Communication Resource](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource). You'll need to record your resource **connection string** for this sample.
- [Configuring the webhook](https://docs.microsoft.com/en-us/azure/devops/service-hooks/services/webhooks?view=azure-devops) for **Microsoft.Communication.IncomingCall** event.
- (Optional) Create Azure Speech resource for generating custom message to be played by application. Follow [here](https://docs.microsoft.com/azure/cognitive-services/speech-service/overview#try-the-speech-service-for-free) to create the resource.

> Note: the samples make use of the Microsoft Cognitive Services Speech SDK. By downloading the Microsoft Cognitive Services Speech SDK, you acknowledge its license, see [Speech SDK license agreement](https://aka.ms/csspeech/license201809).

## Before running the sample for the first time

1. Open an instance of PowerShell, Windows Terminal, Command Prompt or equivalent and navigate to the directory that you'd like to clone the sample to.
2. git clone https://github.com/Azure-Samples/Communication-Services-dotnet-quickstarts.git.

### Locally running the media streaming WebSocket app
1. Go to IncomingCallMediaStreaming folder and open `IncomingCallMediaStreaming.sln` solution in Visual Studio
Run the WebSocketListener project, copy the ngrok URl for "MediaStreamingTransportURI" configuration.

### Locally deploying the Incoming call media streaming app

1. Go to IncomingCallMediaStreaming folder and open `IncomingCallMediaStreaming.sln` solution in Visual Studio
2. Open the appsetting.json file to configure the following settings

	- ResourceConnectionString: Azure Communication Service resource's connection string.
	- AppCallBackUri: URI of the deployed app service
	- SecretValue: Query string for callback URL
	- MediaStreamingTransportURI: websocket URL in format "wss://{ngrok-URL of media streaming WebSocket sample}"

3. Run `IncomingCallMediaStreaming` project.
4. Use postman or any debugging tool and open url - https://localhost:5001

### Publish to Azure

1. Right click the `IncomingCallMediaStreaming` project and select Publish.
2. Create a new publish profile and select your app name, Azure subscription, resource group and etc.
3. After publishing, add the following configurations on azure portal (under app service's configuration section).

	- ResourceConnectionString: Azure Communication Service resource's connection string.
	- AppCallBackUri: URI of the deployed app service.
	- SecretValue: Query string for callback URL.
	- MediaStreamingTransportURI: websocket URL in format "wss://{ngrok-URL}"

### Create Webhook for incoming call event
1. Configure webhook from ACS events tab for incoming call event.
 	- Manually configuring the webhook using this command. use above published "'https://<IncomingCallMediaStreaming-URL>/OnIncomingCall" URL as webhook URL.

	```
	armclient put "/subscriptions/<Subscriptin-ID>/resourceGroups/<resource group name>/providers/Microsoft.Communication/CommunicationServices/<CommunicationService Name>/providers/Microsoft.EventGrid/eventSubscriptions/<WebHookName>?api-version=2020-06-01" "{'properties':{'destination':{'properties':{'endpointUrl':'<webhookurl>'},'endpointType':'WebHook'},'filter':{'includedEventTypes': ['Microsoft.Communication.IncomingCall']}}}" -verbose

	```



4. Detailed instructions on publishing the app to Azure are available at [Publish a Web app](https://docs.microsoft.com/visualstudio/deployment/quickstart-deploy-to-azure?view=vs-2019).

**Note**: While you may use http://localhost for local testing, the sample when deployed will only work when served over https. The SDK [does not support http](https://docs.microsoft.com/azure/communication-services/concepts/voice-video-calling/calling-sdk-features#user-webrtc-over-https).

### Troubleshooting

1. Solution doesn\'t build, it throws errors during build

	Clean/rebuild the C# solution
