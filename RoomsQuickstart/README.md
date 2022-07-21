---
page_type: sample
languages:
- csharp
products:
- azure
- azure-communication-services
---

# Azure Communication Services - Rooms Private Preview
**Private Repository - Contents shared under NDA and TAP Agreement**

This is a private respository for the Azure Communication Services technology adoption program (TAP), colloquially called private preview. The documentation and artifacts listed below help you try out new features in development and provide feedback.

# Create and manage rooms

This code sample contains source code for a c# application that can create and manage Azure Communication Services rooms.

For full instructions on how to build this code sample, please refer to the accompanying [document](https://docs.microsoft.com/en-us/azure/communication-services/quickstarts/rooms/get-started-rooms?branch=master).

The private preview version of Azure Communiation Services Rooms .NET SDK is also embedded.

## Prerequisites
- An Azure account with an active subscription. [Create an account for free](https://azure.microsoft.com/free/?WT.mc_id=A261C142F).
- An active Communication Services resource. [Create a Communication Services resource](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource).
- The latest version [.NET Core client library](https://dotnet.microsoft.com/download/dotnet-core) for your operating system.
- Two or more Communication User Identities. [Create and manage access tokens](https://docs.microsoft.com/en-us/azure/communication-services/quickstarts/access-tokens?pivots=programming-language-csharp) or [Quick-create identities for testing](https://review.docs.microsoft.com/en-us/azure/communication-services/quickstarts/identity/quick-create-identity).


## Code Structure

- **./RoomsQuickStart/Program.cs:** Core application code with room operations implementation.
- **./RoomsQuickStart/RoomsQuickStart.csproj:** Project configuration file.

## Before running sample code
1. In Program.cs, add your Azure Communication Resource connection string.
3. In Program.cs, add your Communication Users as room participants.
