---
page_type: sample
languages:
- csharp
products:
- azure
- azure-communication-services
---


# Recognize Dtmf Call Sample

This sample application shows how the Azure Communication Services  - Call Automation SDK can be used to build IVR related solutions. This sample makes an outbound call to a phone number or a communication identifier and plays an audio message. Application plays next audio based on the key pressed by the callee. This sample application configured for accepting tone 1 (tone1), 2 (tone2) and 3 (tone3), If the callee pressed any other key than expected, an invalid audio tone will be played and then call will be disconnected.
This sample application is also capable of making multiple concurrent outbound calls. The application is a console-based application built on .Net Framework 4.8.

# Design

![design](./Media/recognizeDtmfDesign.png)

## Prerequisites

- Create an Azure account with an active subscription. For details, see [Create an account for free](https://azure.microsoft.com/free/)
- [Visual Studio (2019 and above)](https://visualstudio.microsoft.com/vs/)
- [.NET Framework 4.8](https://dotnet.microsoft.com/download/dotnet-framework/net48) (Make sure to install version that corresponds with your visual studio instance, 32 vs 64 bit)
- Create an Azure Communication Services resource. For details, see [Create an Azure Communication Resource](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource). You will need to record your resource **connection string** for this sample.
- Get a phone number for your new Azure Communication Services resource. For details, see [Get a phone number](https://learn.microsoft.com/en-us/azure/communication-services/quickstarts/telephony/get-phone-number?tabs=windows&pivots=programming-language-csharp)
- Download and install [Ngrok](https://www.ngrok.com/download). As the sample is run locally, Ngrok will enable the receiving of all the events.
- (Optional) Create Azure Speech resource for generating custom message to be played by application. Follow [here](https://docs.microsoft.com/azure/cognitive-services/speech-service/overview#try-the-speech-service-for-free) to create the resource.

> Note: the samples make use of the Microsoft Cognitive Services Speech SDK. By downloading the Microsoft Cognitive Services Speech SDK, you acknowledge its license, see [Speech SDK license agreement](https://aka.ms/csspeech/license201809).

## Before running the sample for the first time

1. Open an instance of PowerShell, Windows Terminal, Command Prompt or equivalent and navigate to the directory that you would like to clone the sample to.
2. git clone `https://github.com/Azure-Samples/Communication-Services-dotnet-quickstarts.git`.
3. Navigate to `RecognizeDtmf` folder and open `RecognizeDtmf.sln` file.

### Configuring application

- Open the app.config file to configure the following settings

	- `ConnectionString`: Azure Communication Service resource's connection string.
	- `SourcePhone`: Phone number associated with the Azure Communication Service resource.
	- `DestinationIdentities`: Multiple sets of phone number targets. These sets are separated by a semi-colon.

    	Format: "OutboundTarget1(Phone Number); OutboundTarget2(Phone Number);OutboundTarget3(Phone Number)".

	  	For e.g. "+1425XXXAAAA; +1425XXXBBBB; +1425XXXCCCC"

	- `NgrokExePath`: Folder path where ngrok.exe is installed.
	- `SecretPlaceholder`: Secret/Password that would be part of callback and will be use to validate incoming requests.
	- `CognitiveServiceKey`: (Optional) Cognitive service key used for generating custom messages.
	- `CognitiveServiceRegion`: (Optional) Region associated with cognitive service.
	- `CustomMessage`: (Optional) Text for the custom message to be converted to speech.
	- `SalesCustomMessage`: (Optional) Text for the custom message when callee presses 1.
	- `MarketingCustomMessage`: (Optional) Text for the custom message when callee presses 2.
	- `CustomerCustomMessage`: (Optional) Text for the custom message when callee presses 3.
	- `InvalidCustomMessage`: (Optional) Text for the custom message when callee presses invalid tone.
