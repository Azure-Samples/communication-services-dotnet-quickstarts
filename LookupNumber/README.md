---
page_type: sample
languages:
- csharp
products:
- azure
- azure-communication-services
---


# Manage phone numbers Quickstart

For full instructions on how to build this code sample from scratch, look at [Quickstart: Look Up Phone Numbers](https://learn.microsoft.com/en-us/azure/communication-services/quickstarts/telephony/number-lookup?pivots=programming-language-java)

## Prerequisites

- An Azure account with an active subscription. [Create an account for free](https://azure.microsoft.com/free/?WT.mc_id=A261C142F).
- Install [Visual Studio](https://visualstudio.microsoft.com/downloads/)
- The latest version [.NET Core client library](https://dotnet.microsoft.com/download/dotnet-core) for your operating system.
- An active Communication Services resource and connection string. [Create a Communication Services resource](https://learn.microsoft.com/en-us/azure/communication-services/quickstarts/create-communication-resource).

## Code Structure

- **./LookupNumber/Program.cs:** Core application code with phone number look up implementation.
- **./LookupNumber/NumberLookupQuickstart.csproj:** Project configuration file.

## Before running sample code

1. Open an instance of PowerShell, Windows Terminal, Command Prompt or equivalent and navigate to the directory that you'd like to clone the sample to.
2. `git clone https://github.com/Azure-Samples/Communication-Services-dotnet-quickstarts.git`
3. With the `Connection String` procured in pre-requisites, add connection string as an environment variable named `COMMUNICATION_SERVICES_CONNECTION_STRING`
4.  Update lines 14 and 20 with the phone number you want to look up.
5.  Decide which lookup you would like to perform, and keep in mind that looking up all the operator details incurs a cost, while looking up only number formatting is free.

> [!WARNING]
> If you want to avoid incurring a charge, comment out lines 20-22


## Run Locally

1. Open `NumberLookupQuickstart.csproj`
2. Run the `NumberLookupQuickstart` project
