---
page_type: sample
languages:
- csharp
products:
- azure
- azure-communication-services
---

# Email Sample Send Email With Managed Identity

This sample sends an email to the selected recipients of any domain using an [Email Communication Services resource](https://docs.microsoft.com/azure/communication-services/quickstarts/email/create-email-communication-resource).
An [Azure Function](https://learn.microsoft.com/azure/azure-functions/functions-overview) that has been given permission to the Email Communication Services resource has been used.

Additional documentation for this sample can be found on [Microsoft Docs](https://docs.microsoft.com/azure/communication-services/concepts/email/email-overview).
Additional documentation on using Azure functions with Managed Identities can be found on [Microsoft Docs](https://learn.microsoft.com/azure/azure-functions/functions-identity-based-connections-tutorial).

## Prerequisites

- Create an Azure account with an active subscription. For details, see [Create an account for free](https://azure.microsoft.com/free/).
- [Visual Studio (2019 and above)](https://visualstudio.microsoft.com/vs/).
- [.NET 6.0](https://dotnet.microsoft.com/download/dotnet/6.0) (Make sure to install version that corresponds with your Visual Studio instance, 32 vs 64 bit).
- Create an Azure Communication Services resource. For details, see [Create an Azure Communication Resource](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource). You will need to record your resource **connection string** for this sample.
- Create an [Azure Email Communication Services resource](https://docs.microsoft.com/azure/communication-services/quickstarts/email/create-email-communication-resource) to start sending emails.

> Note: We can send an email from our own verified domain also [Add custom verified domains to Email Communication Service](https://docs.microsoft.com/azure/communication-services/quickstarts/email/add-custom-verified-domains).

## Code structure

This sample uses an [Azure Functions project created using Visual Studio](https://learn.microsoft.com/azure/azure-functions/functions-create-your-first-function-visual-studio).

- SendEmailWithManagedIdentityFunction.cs: Entry point for sending an email using a function app with a managed identity.

## Before running the sample for the first time

1. Open an instance of PowerShell, Windows Terminal, Command Prompt or equivalent program and navigate to the directory that you would like to clone the sample to.
2. `git clone https://github.com/Azure-Samples/communication-services-dotnet-quickstarts.git`

### Locally configuring the application

1. Navigate to the SendEmailAdvanced folder and open the `SendMailAdvanced.sln` solution in Visual Studio.
1. Open the program.cs file of each project in code structure to configure the following settings:

   - `endpoint`: Replace `<ACS_RESOURCE_ENDPOINT>` with the Azure Communication Service resource's endpoint.
   - `sender`: Replace `<SENDER_EMAIL>` with the sender email obtained from Azure Communication Service.
   - `recipient`: Replace `<RECIPIENT_EMAIL>` with the recipient email.
1. Run respective project.

## ❤️ Feedback

We appreciate your feedback and energy in helping us improve our services. [Please let us know if you are satisfied with ACS through this survey](https://microsoft.qualtrics.com/jfe/form/SV_5dtYL81xwHnUVue).
