---
page_type: sample
languages:
- csharp
products:
- azure
- azure-communication-services
---

# Outbound Azure Function Sample

This azure function sample send SMS messages and use Azure Communication Services Server Calling SDK to make an outbound phone call and for playing audio, on the basis of json input given in the following format.

- `OutboundNumber`: Target phone number where we need to send notification.
- `SourceNumber`: Phone number associated with the Azure Communication Service resource.
- `SMS.Send`: value should be true/false as we want to send SMS or not.
- `SMS.Message`: Message want to send as SMS.
- `PhoneCall.Send`:  value should be true/false as we want to make a phone call or not.
- `PhoneCall.PlayAudioUrl`: wav audio URL (should be a blob URL), playing on outbound phone call. If the value is null, then the sample going to use the default URL stored in the configuration.

The azure function is built on .NET Framework 4.7.2.

```
{
  "SourceNumber":"+18xxxxxxxxxx",
  "OutboundNumber": "+18xxxxxxxxxx",
  "SMS": {
    "Send": "true",
    "Message": "notification message"
  },
  "PhoneCall": {
    "Send": "true",
    "PlayAudioUrl": "audio file URL function app can able to access"
  }
}
```

## Prerequisites

- Create an Azure account with an active subscription. For details, see [Create an account for free](https://azure.microsoft.com/free/)
- [Visual Studio (2019 and above)](https://visualstudio.microsoft.com/vs/)
- [.NET Framework 4.7.2](https://dotnet.microsoft.com/download/dotnet-framework/net472) (Make sure to install version that corresponds with your visual studio instance, 32 vs 64 bit)
- Create an Azure Communication Services resource. For details, see [Create an Azure Communication Resource](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource). You'll need to record your resource **connection string** for this sample.
- Get a phone number for your new Azure Communication Services resource. For details, see [Get a phone number](https://docs.microsoft.com/azure/communication-services/quickstarts/telephony-sms/get-phone-number?pivots=platform-azp)

## Code structure

- ./OutboundFunction/SendNotification.cs : Azure function to send notification using phone call and SMS.
- ./OutboundFunction/OutboundController.cs : Azure function to handle outbound callbacks
- ./OutboundFunction/Phonecall.cs : class for handling outbound phone call.
- ./OutboundFunction/SendSms.cs : class for sending SMS notification.

## Before running the sample for the first time

1. Open an instance of PowerShell, Windows Terminal, Command Prompt or equivalent and navigate to the directory that you'd like to clone the sample to.
2. git clone `https://github.com/Azure-Samples/communication-services-dotnet-quickstarts.git`.
3. Once you get the config keys add the keys as an environment variable.
	- Input your connection string in variable: `Connectionstring`
	- Input you Secret/Password that would be part of callback and will be use to validate incoming requests in variable `SecretPlaceholder`
	- Input URL of default wav file going to play in outbound phone call in variable `CallbackUri`

## Locally deploying the sample app

1. Go to OutboundFunction folder and open `OutboundFunction.sln` solution in Visual Studio
2. Run `OutboundFunction` project.
3. Use postman or any debugging tool and use function URL - http://localhost:7071/api/SendNotification with the json request.

## Publish to Azure

1. [Publish the project to Azure](https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-your-first-function-visual-studio#publish-the-project-to-azure)

**Note**: While you may use http://localhost for local testing, the sample when deployed will only work when served over https. The SDK [does not support http](https://docs.microsoft.com/azure/communication-services/concepts/voice-video-calling/calling-sdk-features#user-webrtc-over-https).

### Configuring Azure Function Sample

- After publishing your function App, add following configuration in your function App's `configuration` section:

	- Connectionstring: Azure Communication Service resource's connection string.
	- SourcePhone: Phone number associated with the Azure Communication Service resource.
	- SecretPlaceholder: Secret/Password that would be part of callback and will be use to validate incoming requests.

  - AudioFileUrl: Url of default wav file going to play in outbound phone call (should be a blob URL).
