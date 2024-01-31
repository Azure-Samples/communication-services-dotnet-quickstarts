---
page_type: sample
languages:
- csharp
products:
- azure
- azure-communication-services
---

# Email Sample Send Email With Managed Identity

[Microsoft Managed Identities](https://learn.microsoft.com/entra/identity/managed-identities-azure-resources/overview) are a secure way for Azure services and resources to authenticate to other Azure services, eliminating the need for explicit credentials or secrets. It provides an automatically managed identity in Microsoft Entra for applications to use when connecting to resources that support Microsoft Entra authentication. Resources such as [Azure Functions](https://learn.microsoft.com/azure/azure-functions/functions-overview) and [Azure Logic Apps](https://learn.microsoft.com/azure/logic-apps/logic-apps-overview) can be given a managed identity. The managed identity can then be given access to an Azure Communication Services resource.

This sample sends an email to the selected recipients of any domain using an [Email Communication Services resource](https://docs.microsoft.com/azure/communication-services/quickstarts/email/create-email-communication-resource).
using an [Azure Function configured to use identies instead of secrets](https://learn.microsoft.com/en-us/azure/azure-functions/functions-identity-access-azure-sql-with-managed-identity#enable-system-assigned-managed-identity-on-azure-function). Additional documentation for this sample can be found on [Microsoft Docs](https://docs.microsoft.com/azure/communication-services/concepts/email/email-overview).

## Prerequisites

- Create an Azure account with an active subscription. For details, see [Create an account for free](https://azure.microsoft.com/free/).
- [Visual Studio (2019 and above)](https://visualstudio.microsoft.com/vs/).
- [.NET 6.0](https://dotnet.microsoft.com/download/dotnet/6.0) (Make sure to install version that corresponds with your Visual Studio instance, 32 vs 64 bit).
- Create an Azure Communication Services resource. For details, see [Create an Azure Communication Resource](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource).
- Create an [Azure Function](https://learn.microsoft.com/azure/azure-functions/functions-overview?pivots=programming-language-csharp).

> Note: We can send an email from our own verified domain also [Add custom verified domains to Email Communication Service](https://docs.microsoft.com/azure/communication-services/quickstarts/email/add-custom-verified-domains).

## Granting the system-assigned identity for the Azure Function access to the Azure Communication Services Resource

To grant an Azure Function access to an Azure Communication Services resource using managed identities, the Azure Function should first by assigned a [system-assigned managed identity](https://learn.microsoft.com/en-us/azure/azure-functions/functions-identity-access-azure-sql-with-managed-identity#enable-system-assigned-managed-identity-on-azure-function).

Next, in the Azure portal, navigate to the Azure Communication Services resource.

1. Select Access Control (IAM). This is where you can view and configure who has access to the resource.
2. Click Add and select add role assignment. The supported roles are 'Contributor' or a custom role that includes both the 'Microsoft.Communication/CommunicationServices/Read' and 'Microsoft.Communication/CommunicationServices/Write' permissions.
3. On the Members tab, under Assign access to, choose Managed Identity
4. Click Select members to open the Select managed identities panel.
5. Confirm that the Subscription is the one in which you created the resources earlier.
6. In the Managed identity selector, choose Function App from the System-assigned managed identity category. The label "Function App" may have a number in parentheses next to it, indicating the number of apps in the subscription with system-assigned identities.
7. Your app should appear in a list below the input fields. If you don't see it, you can use the Select box to filter the results with your app's name.
8. Click on your application. It should move down into the Selected members section. Click Select.
9. Back on the Add role assignment screen, click Review + assign. Review the configuration, and then click Review + assign.

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
