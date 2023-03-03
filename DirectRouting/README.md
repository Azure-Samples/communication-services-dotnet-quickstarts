---
page_type: sample
languages:
- csharp
products:
- azure
- azure-communication-services
---


# Direct Routing Configuration

For full instructions on how to build this code sample from scratch, look at [Quickstart: Direct Routing](https://docs.microsoft.com/azure/communication-services/quickstarts/telephony-sms/voice-routing-sdk-config?pivots=programming-language-csharp)

## Prerequisites

- An Azure account with an active subscription. [Create an account for free](https://azure.microsoft.com/free/?WT.mc_id=A261C142F).
- Install [Visual Studio](https://visualstudio.microsoft.com/downloads/)
- The latest version [.NET Core client library](https://dotnet.microsoft.com/download/dotnet-core) for your operating system.
- An active Communication Services resource and connection string. [Create a Communication Services resource](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource?tabs=windows&pivots=platform-net).

## Code Structure

- **./DirectRouting/Program.cs:** Core application code with manage phone numbers implementation.
- **./DirectRouting/DirectRouting.csproj:** Project configuration file.
- **./DirectRouting.sln:** Visual Studio solution.

## Before running sample code

1. Open an instance of PowerShell, Windows Terminal, Command Prompt or equivalent and navigate to the directory that you'd like to clone the sample to.
2. `git clone https://github.com/Azure-Samples/Communication-Services-dotnet-quickstarts.git`
3. With the `Connection String` procured in pre-requisites, add it to the **DirectRouting/program.cs** file. Assign your connection string in line 3:
  ```csharp
  var connectionString = "endpoint=https://<RESOURCE_NAME>.communication.azure.com/;accesskey=<ACCESS_KEY>";
  ```


## Run Locally

1. Open `DirectRouting.sln`
2. Run the `DirectRouting` project
