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

- `SendNotification`: value should be `true`, for sending the notification.
- `OutboundNumber`: Target phone number where we need to send notification.
- `SourceNumber`: Phone number associated with the Azure Communication Service resource.
- `SMS.Send`: value should be true/false as we want to send SMS or not.
- `SMS.Message`: Message want to send as SMS.
- `PhoneCall.Send`:  value should be true/false as we want to make a phone call or not.
- `PhoneCall.PlayAudioUrl`: wav audio URL (should be a blob URL), playing on outbound phone call. If the value is null, then the sample going to use the default URL stored in the configuration.

The azure function is built on .NET Framework 4.7.2.

```
{
  "SendNotification": "true",
  "SourceNumber":"+18xxxxxxxxxx",
  "OutboundNumber": "+18xxxxxxxxxx",
  "SMS": {
    "Send": "true",
    "Message": "notification message"
  },
  "PhoneCall": {
    "Send": "true",
    "PlayAudioUrl": "wav audio URL as Blob URL "
  }
}
```

## Prerequisites

- Create an Azure account with an active subscription. For details, see [Create an account for free](https://azure.microsoft.com/free/)
- [Visual Studio (2019 and above)](https://visualstudio.microsoft.com/vs/)
- [.NET Framework 4.7.2](https://dotnet.microsoft.com/download/dotnet-framework/net472) (Make sure to install version that corresponds with your visual studio instance, 32 vs 64 bit)
- Create an Azure Communication Services resource. For details, see [Create an Azure Communication Resource](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource). You'll need to record your resource **connection string** for this sample.
- Get a phone number for your new Azure Communication Services resource. For details, see [Get a phone number](https://docs.microsoft.com/azure/communication-services/quickstarts/telephony-sms/get-phone-number?pivots=platform-azp)
- An Azure storage account and container, for details, see [Create a storage account](https://docs.microsoft.com/azure/storage/common/storage-account-create?tabs=azure-portal). For storing sample audio file **AudioFileUrl**.

## Code structure

- ./SendNotification/run.csx : Azure function to send notification using phone call and SMS and to handle outbound callbacks.
- ./SendNotification/Phonecall.csx : class for handling outbound phone call.
- ./SendNotification/SendSms.csx : class for sending SMS notification.
- ./SendNotification/EventHandler/*.csx : Code for managing callbacks and request authorization.
- ./SendNotification/function.proj : Contains all the package references.

## Create Function App using Azure portal

1. [Create a Function App](https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-function-app-portal#create-a-function-app)
2. [Create a HTTP Trigger function `SendNotification` with Authorization level `Anonymous`](https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-function-app-portal#create-function).
3. After creating the function edit `run.csx` and add other code files.

##  Add code files using `kudu` Tool

1. Under Function app menu, select `Advanced tool` under `Development tools` section.
2. Click on the link `Go`, `Kudu` web got opened in new tab.
3. Select `CMD` option under `Debug Console` menu.
4. Add files under `/site/wwwroot/SendNotification/` folder.

### Configuring Azure Function Sample

- After publishing your function App, add following configuration in your function App's `configuration` section:

	- Connectionstring: Azure Communication Service resource's connection string.
	- SourcePhone: Phone number associated with the Azure Communication Service resource.
	- SecretPlaceholder: Secret/Password that would be part of callback and will be use to validate incoming requests.
  - AudioFileUrl: Url of default wav file going to play in outbound phone call (should be a blob URL).
