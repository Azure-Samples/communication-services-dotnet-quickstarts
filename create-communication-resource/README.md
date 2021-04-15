---
page_type: sample
languages:
- csharp
products:
- azure
- azure-communication-services
---

# Create and manage Communication Services resources

For full instructions on how to build this code sample from scratch, look at [Quickstart: Create and manage Communication Services resources](https://docs.microsoft.com/en-us/azure/communication-services/quickstarts/create-communication-resource?tabs=windows&pivots=platform-net)

## Prerequisites

- An Azure account with an active subscription. Create an account for free.
- The latest version .NET Core SDK for your operating system.
- Get the latest version of the .NET Identity SDK.
- Get the latest version of the .NET Management SDK.

## Code Structure

- **./create-communication-resource/Program.cs:** Core application code with chat operations implementation.
- **./create-communication-resource/create-communication-resource.csproj:** Project configuration file.

## Before running sample code

1. Open an instance of PowerShell, Windows Terminal, Command Prompt or equivalent and navigate to the directory that you'd like to clone the sample to.
2. `git clone https://github.com/Azure-Samples/Communication-Services-dotnet-quickstarts.git`
3. Add subscription id to environment variable using below command

setx AZURE_SUBSCRIPTION_ID <AZURE_SUBSCRIPTION_ID>

## Run Locally

1. Open `create-communication-resource.csproj`
2. Run the `create-communication-resource` project