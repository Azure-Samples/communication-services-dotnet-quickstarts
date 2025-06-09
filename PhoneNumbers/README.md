---
page_type: sample
languages:
- csharp
products:
- azure
- azure-communication-services
---


# Manage phone numbers Quickstart

For full instructions on how to build this code sample from scratch, look at [Quickstart: Manage phone numbers](https://docs.microsoft.com/azure/communication-services/quickstarts/telephony/get-phone-number?pivots=programming-language-csharp)

## Prerequisites

- An Azure account with an active subscription. [Create an account for free](https://azure.microsoft.com/free/?WT.mc_id=A261C142F).
- Install [Visual Studio](https://visualstudio.microsoft.com/downloads/)
- The latest version [.NET Core client library](https://dotnet.microsoft.com/download/dotnet-core) for your operating system.
- An active Communication Services resource and connection string. [Create a Communication Services resource](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource?tabs=windows&pivots=platform-net).

## Code Structure

- **./PhoneNumbers/Program.cs:** Core application code with manage phone numbers implementation.
- **./PhoneNumbers/PhoneNumbers.csproj:** Project configuration file.
- **./PhoneNumbers.sln:** Visual Studio solution.

## Before running sample code

1. Open an instance of PowerShell, Windows Terminal, Command Prompt or equivalent and navigate to the directory that you'd like to clone the sample to.
2. `git clone https://github.com/Azure-Samples/Communication-Services-dotnet-quickstarts.git`
3. With the `Connection String` procured in pre-requisites, add it to the **PhoneNumbers/program.cs** file. Assign your connection string in line 3:
  ```csharp
  var connectionString = "<connection_string>";
  ```


## Run Locally

1. Open `PhoneNumbers.sln`
2. Run the `PhoneNumbers` project
