---
page_type: sample
languages:
- csharp
products:
- azure
- azure-communication-services
---

# Email Sample

This sample application shows how the Azure Communication Services Email SDK can be used to build an email experience. This sample sends an email to the required recipients of any domain using [Email Communication Services resource](https://docs.microsoft.com/en-us/azure/communication-services/quickstarts/email/create-email-communication-resource)

Additional documentation for this sample can be found on [Microsoft Docs](https://docs.microsoft.com/en-us/azure/communication-services/concepts/email/email-overview).

## Prerequisites

- Create an Azure account with an active subscription. For details, see [Create an account for free](https://azure.microsoft.com/free/)
- [Visual Studio (2019 and above)](https://visualstudio.microsoft.com/vs/)
- [.NET core 3.1](https://dotnet.microsoft.com/en-us/download/dotnet/3.1) (Make sure to install version that corresponds with your visual studio instance, 32 vs 64 bit)
- Create an Azure Communication Services resource. For details, see [Create an Azure Communication Resource](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource). You'll need to record your resource **connection string** for this sample.
- Create an [Azure Email Communication Services resource](https://docs.microsoft.com/en-us/azure/communication-services/quickstarts/email/create-email-communication-resource) to start sending emails.

> Note: We can send an email from our own verified domain also [Add custom verified domains to Email Communication Service](https://docs.microsoft.com/en-us/azure/communication-services/quickstarts/email/add-custom-verified-domains).

## Code structure

- ./SendEmail/Program.cs: Entry point for sending email.

## Before running the sample for the first time

1. Open an instance of PowerShell, Windows Terminal, Command Prompt or equivalent and navigate to the directory that you'd like to clone the sample to.
2. `git clone https://github.com/Azure-Samples/communication-services-dotnet-quickstarts.git`


### Locally configuring the application

1. Go to SendEmail folder and open `SendEmail.sln` solution in Visual Studio
2. Open the program.cs file to configure the following settings
  - `connectionstring`: Replace `<ACS_CONNECTION_STRING>` with Azure Communication Service resource's connection string.
  - `sender`: Replace `<SENDER_EMAIL>` with sender email obtained from Azure Communication Service.
  - `Line 26 - <RECIPIENT_EMAIL>`: Replace with recipient email.
  - `content`: Either use PlainText or Html to set the message content.
3. Run `SendEmail` project.


## ❤️ Feedback
We appreciate your feedback and energy helping us improve our services. [Please let us know if you are satisfied with ACS through this survey](https://microsoft.qualtrics.com/jfe/form/SV_5dtYL81xwHnUVue).
