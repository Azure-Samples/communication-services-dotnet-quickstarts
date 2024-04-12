---
page_type: sample
languages:
- csharp
products:
- azure
- azure-communication-services
- communication-chat
---

# Chat Teams Interop QuickStart

This quickstart contains samples for the following tutorials:
 - [Join your chat app to a Teams meeting](https://learn.microsoft.com/en-us/azure/communication-services/quickstarts/chat/meeting-interop?pivots=platform-windows)
 - [Enable inline image support in your Chat app](https://learn.microsoft.com/en-us/azure/communication-services/tutorials/chat-interop/meeting-interop-features-inline-image?pivots=programming-language-csharp)
 - [Enable file attachment support in your Chat app](https://learn.microsoft.com/en-us/azure/communication-services/tutorials/chat-interop/meeting-interop-features-file-attachment?pivots=programming-language-csharp)

## Prerequisites

* A [Teams deployment](/deployoffice/teams-install). 
* An Azure account with an active subscription. [Create an account for free](https://azure.microsoft.com/free/?WT.mc_id=A261C142F).  
* Install [Visual Studio 2019](https://visualstudio.microsoft.com/downloads/) with Universal Windows Platform development workload.  
* A deployed Communication Services resource. [Create a Communication Services resource](../../create-communication-resource.md). 
* A Teams Meeting Link.

## Code Structure

- **src\ChatTeamsInteropQuickStart:** Sample code to showcase how you can join a Teams chat using the Chat SDK.
- **src\ChatTeamsInteropInlineImageQuickStart:** Sample code to showcase how you can enable inline image support in a Teams Interoperability Chat.
- **src\ChatTeamsInteropFileSharingQuickStart:** Sample code to showcase how you can enable file sharing support in a Teams Interoperability Chat.

## Before running sample code

1. Open an instance of PowerShell, Windows Terminal, Command Prompt or equivalent and navigate to the directory that you'd like to clone the sample to.
2. `git clone https://github.com/Azure-Samples/Communication-Services-dotnet-quickstarts.git`
3. Add subscription id to environment variable using below command

setx AZURE_SUBSCRIPTION_ID <AZURE_SUBSCRIPTION_ID>

## Run Locally

1. Open `ChatTeamsInteropQuickStart.sln` in in Visual Studio
2. Choose a sample and set it as startup project
3. In `MainPage.xaml.cs`, provide required `connectionString_`
4. Run the application