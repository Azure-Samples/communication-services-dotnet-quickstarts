---
page_type: sample
languages:
- csharp
products:
- azure
- azure-communication-services
---

# Obtain Azure Communication Services access token for Microsoft Entra ID user

This code sample walks you through the process of obtaining an Azure Communication Token for a Microsoft Entra ID user.

This sample application utilizes the [Azure.Identity](https://docs.microsoft.com/dotnet/api/overview/azure/identity-readme?view=azure-dotnet) package for authentication against Microsoft Entra ID and acquisition of a token with delegated permissions. The [Azure.Communication.Common](https://learn.microsoft.com/en-us/dotnet/api/overview/azure/communication.common-readme?view=azure-dotnet) package provides the functionality required to obtain Azure Communication Services access tokens for Microsoft Entra ID users.

## Prerequisites
- Install [Visual Studio](https://visualstudio.microsoft.com/downloads/)
- The latest version .NET Core SDK for your operating system.
- An active Communication Services resource and endpoint URI. [Create a Communication Services resource](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource/).
- A Microsoft Entra ID instance.  For more information, see [Microsoft Entra ID overview](https://learn.microsoft.com//entra/fundamentals/whatis).
- [Azure Identify SDK for .Net](https://www.nuget.org/packages/Azure.Identity) to authenticate with Microsoft Entra ID.
- [Azure Communication Services Common SDK for .Net](https://www.nuget.org/packages/Azure.Communication.Common/) to obtain Azure Communication Services access tokens for Microsoft Entra ID user.

## Code Structure

- **./EntraIdUsersSupportQuickstart/Program.cs:** Core application code with operations to obtain Azure Communication Services access token for Microsoft Entra ID user.
- **./EntraIdUsersSupportQuickstart/EntraIdUsersSupportQuickstart.csproj:** Project configuration file.

## Before running sample code

1. Complete the `Administrator actions` from the [Quickstart: Set up and obtain access tokens for Microsoft Entra ID users](https://docs.microsoft.com/azure/communication-services/quickstarts/entra-id-authentication-integration).
1. On the Authentication pane of your Entra ID App, add a new platform of the mobile and desktop application type with the Redirect URI of `http://localhost`.
1. Open an instance of PowerShell, Windows Terminal, Command Prompt or equivalent and navigate to the directory that you'd like to clone the sample to.
1. `git clone https://github.com/Azure-Samples/Communication-Services-dotnet-quickstarts.git`
1. With the Communication Services procured in pre-requisites, add resource endpoint URI, an Entra ID client ID and tenant ID to environment variable using below commands:

```console
setx COMMUNICATION_SERVICES_RESOURCE_ENDPOINT <Azure Communication Services resource endpoint URI>
setx ENTRA_CLIENT_ID <application (client) ID of the Entra ID App>
setx ENTRA_TENANT_ID <tenant ID of the Entra ID>
```

## Run Locally

1. Open `EntraIdUsersSupportQuickstart.csproj`
2. Run the `EntraIdUsersSupportQuickstart` project

You will be prompted with a browser window to sign in with your Entra ID credentials. After successful authentication, the application will obtain an Azure Communication Services access token.