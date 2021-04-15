---
page_type: sample
languages:
- csharp
products:
- azure
- azure-communication-services
---

# Create and manage access tokens

For full instructions on how to build this code sample from scratch, look at [Quickstart: Create and manage access tokens](https://docs.microsoft.com/en-us/azure/communication-services/quickstarts/access-tokens?pivots=programming-language-csharp)

## Prerequisites

- An Azure account with an active subscription. [Create an account for free](https://azure.microsoft.com/free/?WT.mc_id=A261C142F). 
- Install [Visual Studio](https://visualstudio.microsoft.com/downloads/)
- The latest version .NET Core SDK for your operating system.
- Create an Azure Communication Services resource. For details, see [Create a Communication Services resource](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource). You'll need to record your connection string for this quickstart.

## Code Structure

- **./AccessTokensQuickstart/Program.cs:** Core application code with chat operations implementation.
- **./AccessTokensQuickstart/AccessTokensQuickstart.csproj:** Project configuration file.

## Before running sample code

1. Open an instance of PowerShell, Windows Terminal, Command Prompt or equivalent and navigate to the directory that you'd like to clone the sample to.
2. `git clone https://github.com/Azure-Samples/Communication-Services-dotnet-quickstarts.git`
3. With the Communication Services procured in pre-requisites, add connection string to environment variable using below command

setx COMMUNICATION_SERVICES_CONNECTION_STRING <CONNECTION_STRING>

## Run Locally

1. Open `AccessTokensQuickstart.csproj`
2. Run the `AccessTokensQuickstart` project