---
page_type: sample
languages:
- csharp
products:
- azure
- azure-communication-services
---


# Outbound Reminder Call Sample

This is a sample web app service, shows how the Azure Communication Services Server Calling SDK can be used to build IVR related solutions. This sample receives an incoming call request whenever a call made to a phone number or a communication identifier then API first answers the call and plays an audio message. If the callee presses 1 (tone1), to reschedule an appointment, then the application transfer a call to the target participant and then leaves the call. If the callee presses any other key then the application ends the call. This sample application is also capable of handling multiple concurrent incoming calls.
The application is an app service application built on .Net Framework 4.7.2.

## Prerequisites

- Create an Azure account with an active subscription. For details, see [Create an account for free](https://azure.microsoft.com/free/)
- [Visual Studio (2019 and above)](https://visualstudio.microsoft.com/vs/)
- [.NET Framework 4.7.2](https://dotnet.microsoft.com/download/dotnet-framework/net472) (Make sure to install version that corresponds with your visual studio instance, 32 vs 64 bit)
- Create an Azure Communication Services resource. For details, see [Create an Azure Communication Resource](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource). You'll need to record your resource **connection string** for this sample.
- Configuring the webhook for 'Microsoft.Communication.IncomingCall' event.
- (Optional) Create Azure Speech resource for generating custom message to be played by application. Follow [here](https://docs.microsoft.com/azure/cognitive-services/speech-service/overview#try-the-speech-service-for-free) to create the resource.

> Note: the samples make use of the Microsoft Cognitive Services Speech SDK. By downloading the Microsoft Cognitive Services Speech SDK, you acknowledge its license, see [Speech SDK license agreement](https://aka.ms/csspeech/license201809).

## Before running the sample for the first time

1. Open an instance of PowerShell, Windows Terminal, Command Prompt or equivalent and navigate to the directory that you'd like to clone the sample to.
2. git clone https://github.com/Azure-Samples/Communication-Services-dotnet-quickstarts.git.

### Locally deploying the sample app

1. Go to IncomingCallRouting folder and open `IncomingCallRouting.sln` solution in Visual Studio
2. Open the appsetting.json file to configure the following settings

	- ResourceConnectionString: Azure Communication Service resource's connection string.
	- AppCallBackUri: URI of the deployed app service
	- audioFileUri: public url of wav audio file
	- TargetParticipant: Target participant to transfer the call.

3. Run `IncomingCallRouting` project.
4. Use postman or any debugging tool and open url - https://localhost:5001

### Publish to Azure

1. Right click the `IncomingCallRouting` project and select Publish.
2. Create a new publish profile and select your app name, Azure subscription, resource group and etc.
3. After publishing, add the following configurations on azure portal (under app service's configuration section).

	- ResourceConnectionString: Azure Communication Service resource's connection string.
	- AppCallBackUri: URI of the deployed app service
	- audioFileUri: public url of wav audio file
	- TargetParticipant: Target participant to transfer the call.


4. Detailed instructions on publishing the app to Azure are available at [Publish a Web app](https://docs.microsoft.com/visualstudio/deployment/quickstart-deploy-to-azure?view=vs-2019).

**Note**: While you may use http://localhost for local testing, the sample when deployed will only work when served over https. The SDK [does not support http](https://docs.microsoft.com/azure/communication-services/concepts/voice-video-calling/calling-sdk-features#user-webrtc-over-https).

### Troubleshooting

1. Solution doesn\'t build, it throws errors during build

	Clean/rebuild the C# solution
