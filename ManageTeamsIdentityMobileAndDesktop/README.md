---
page_type: sample
languages:
- csharp
products:
- azure
- azure-communication-services
---

# Create and manage Communication access tokens for Teams users in mobile and desktop applications

This code sample walks you through the process of acquiring a Communication Token Credential by exchanging an Azure AD token of a user with a Teams license for a valid Communication access token.

This sample application utilizes the [Microsoft.Identity.Client](https://docs.microsoft.com/dotnet/api/microsoft.identity.client?view=azure-dotnet) package for authentication against the Azure AD and acquisition of a token with delegated permissions. The token exchange itself is facilitated by the `Azure.Communication.Identity` package.

The initialization of a Communication credential object that can be used for Calling is achieved by the `Azure.Communication.Common` package.


## Prerequisites

- An Azure account with an active subscription. [Create an account for free](https://azure.microsoft.com/free/?WT.mc_id=A261C142F).
- Install [Visual Studio](https://visualstudio.microsoft.com/downloads/)
- The latest version .NET Core SDK for your operating system.
- An active Communication Services resource and connection string. [Create a Communication Services resource](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource/).
- Azure Active Directory tenant with users that have a Teams license.

## Code Structure

- **./ManageTeamsIdentityMobileAndDesktop/Program.cs:** Core application code with operations to exchange an Azure AD token of a user with a Teams license for a valid Communication access token.
- **./ManageTeamsIdentityMobileAndDesktop/ManageTeamsIdentityMobileAndDesktop.csproj:** Project configuration file.

## Before running sample code

1. Complete the [Administrator actions](https://docs.microsoft.com/azure/communication-services/quickstarts/manage-teams-identity?pivots=programming-language-csharp) from the [Manage access tokens for Teams users quickstart](https://docs.microsoft.com/azure/communication-services/quickstarts/manage-teams-identity).
   - Take a not of Fabrikam's Azure AD Tenant ID and Contoso's Azure AD App Client ID. You'll need the values in the following steps.
1. On the Authentication pane of your Azure AD App, add a new platform of the mobile and desktop application type with the Redirect URI of `http://localhost`.
1. Open an instance of PowerShell, Windows Terminal, Command Prompt or equivalent and navigate to the directory that you'd like to clone the sample to.
1. `git clone https://github.com/Azure-Samples/Communication-Services-dotnet-quickstarts.git`
1. With the Communication Services procured in pre-requisites, add connection string,an AAD client ID and tenant ID to environment variable using below commands:

setx COMMUNICATION_SERVICES_CONNECTION_STRING <COMMUNICATION_SERVICES_CONNECTION_STRING>
setx AAD_CLIENT_ID <CONTOSO_AZURE_AD_CLIENT_ID>
setx AAD_TENANT_ID <FABRIKAM_AZURE_AD_TENANT_ID>

## Run Locally

1. Open `ManageTeamsIdentityMobileAndDesktop.csproj`
2. Run the `ManageTeamsIdentityMobileAndDesktop` project

You should be navigated from your browser to a standard OAuth flow with Microsoft Authentication Library (MSAL). If authentication is successful, the application receives an Azure AD access token and will be redirected to `http://localhost` where the Azure AD access token will be exchanged for a Communication access token.