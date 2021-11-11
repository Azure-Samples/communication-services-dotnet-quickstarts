---
page_type: sample
languages:
- csharp
products:
- azure
- azure-communication-services
---

# Outbound Azure Function Sample

This azure function sample send SMS, make an outbound call and play audio using ACS SMS and Server Calling SDKs on the basis of json input given in the following format.

- `SendNotification`: value should be `true`, for sending the notification.
- `OutboundNumber`: Target phone number where we need to send notification.
- `SourceNumber`: Phone number associated with the Azure Communication Service resource.
- `SMS.Send`: value should be true/false as we want to send SMS or not.
- `SMS.Message`: Message want to send as SMS.
- `PhoneCall.Send`:  value should be true/false as we want to make a phone call or not.
- `PhoneCall.PlayAudioUrl`: The wav file url which accessible by the function app. If the value is empty, then the sample will use configured url through azure portal.

```
{
  "SendNotification": "true",
  "SourceNumber":"+18xxxxxxxxxx",
  "OutboundNumber": "+18xxxxxxxxxx",
  "SMS": {
    "Send": "true",
    "Message": "message to be sent"
  },
  "PhoneCall": {
    "Send": "true",
    "PlayAudioUrl": "audio file URL function app can able to access"
  }
}
```

## Prerequisites

- Create an Azure account with an active subscription. For details, see [Create an account for free](https://azure.microsoft.com/free/)
- Create an Azure Communication Services resource. For details, see [Create an Azure Communication Resource](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource). You'll need to record your resource **connection string** for this sample.
- Get a phone number for your new Azure Communication Services resource. For details, see [Get a phone number](https://docs.microsoft.com/azure/communication-services/quickstarts/telephony-sms/get-phone-number?pivots=platform-azp)

## Code structure

- ./SendNotification/run.csx : Azure function to send notification using phone call and SMS and to handle outbound callbacks.
- ./SendNotification/Phonecall.csx : class for handling outbound phone call.
- ./SendNotification/SendSms.csx : class for sending SMS notification.
- ./SendNotification/EventHandler/*.csx : Code for managing callbacks and request authorization.
- ./SendNotification/function.proj : Contains nuget package references.

## Create Function App using Azure portal

- [Create a Function App](https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-function-app-portal#create-a-function-app)
- [Create a HTTP Trigger function `SendNotification` with Authorization level `Anonymous`](https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-function-app-portal#create-function).
- After creating the function edit `run.csx` and add other code files.

##  Add code files using `kudu` Tool

- Under Function app menu, select `Advanced tool` under `Development tools` section.
- Click on the link `Go`, `Kudu` web got opened in new tab.
- Select `CMD` option under `Debug Console` menu.
- Add files under `/site/wwwroot/SendNotification/` folder.

### Configuring Azure Function Sample

After publishing your function App, add following configuration in your function App's `configuration` section:

- Connectionstring: Azure Communication Service resource's connection string.
- SourcePhone: Phone number associated with the Azure Communication Service resource.
- SecretPlaceholder: Secret/Password that would be part of callback and will be use to validate incoming requests.
- AudioFileUrl: Url of default wav file going to play in outbound phone call which is accessible by function app.
