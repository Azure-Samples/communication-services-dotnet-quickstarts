---
page_type: sample
languages:
- csharp
products:
- azure
- azure-communication-services
---

# Job Router quick start

For full instructions on how to build this code sample from scratch, look at [Quickstart: Create a worker and job](https://learn.microsoft.com/azure/communication-services/quickstarts/jobrouter/quickstart)

## Prerequisites

- An Azure account with an active subscription. [Create an account for free](https://azure.microsoft.com/free/?WT.mc_id=A261C142F).
- An active Communication Services resource. [Create a Communication Services resource](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource).
- The latest version [.NET client library](https://dotnet.microsoft.com/download/dotnet) for your operating system.

## Code Structure

- **./JobRouterQuickStart/Program.cs:** Core application code.
- **./JobRouterQuickStart/JobRouterQuickStart.csproj:** Project configuration file.
- **./JobRouterQuickStart/JobRouterQuickStart.sln:** Visual Studio solution.

## Before running sample code

1. Open an instance of PowerShell, Windows Terminal, Command Prompt or equivalent and navigate to the directory that you'd like to clone the sample to.
2. `git clone https://github.com/Azure-Samples/Communication-Services-dotnet-quickstarts.git`
3. In Program.cs, replace `<ConnectionString>` with Azure Communication Resource connection string.

## Run Locally

1. Open `JobRouterQuickStart.sln`
2. Run the `JobRouterQuickStart` project
