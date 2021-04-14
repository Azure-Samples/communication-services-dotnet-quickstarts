---
page_type: sample
languages:
- csharp
products:
- azure
- azure-communication-services
---


# Add Chat to your Application

For full instructions on how to build this code sample from scratch, look at [Quickstart: Add Chat to your App](https://docs.microsoft.com/en-us/azure/communication-services/quickstarts/chat/get-started?pivots=programming-language-csharp)

## Prerequisites

- An Azure account with an active subscription. [Create an account for free](https://azure.microsoft.com/free/?WT.mc_id=A261C142F). 
- Install [Visual Studio](https://visualstudio.microsoft.com/downloads/)
- Create an Azure Communication Services resource. For details, see [Create a Communication Services resource](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource). You'll need to record your resource endpoint for this quickstart.
- Create three ACS Users and issue them a [User Access Token](https://docs.microsoft.com/en-us/azure/communication-services/quickstarts/access-tokens?pivots=programming-language-csharp). Be sure to set the scope to "chat", and note the token string as well as the userId string.

## Code Structure

- **./add-chat/Program.cs:** Core application code with chat operations implementation.
- **./add-chat/ChatQuickstart.csproj:** Project configuration file.

## Before running sample code

1. Open an instance of PowerShell, Windows Terminal, Command Prompt or equivalent and navigate to the directory that you'd like to clone the sample to.
2. `git clone https://github.com/Azure-Samples/Communication-Services-dotnet-quickstarts.git`
3. With the Communication Services procured in pre-requisites, add end point to the **add-chat/program.cs** file. Update Communication Services Resource name in line 13.
   ```Uri endpoint = new Uri("https://<RESOURCE_NAME>.communication.azure.com");```
4. With the `Access Tokens and User Identities` procured in pre-requisites, add it to the **add-chat/program.cs** file.
   ```CommunicationTokenCredential communicationTokenCredential = new CommunicationTokenCredential("<Access_Token>");```
   ```var chatParticipant = new ChatParticipant(identifier: new CommunicationUserIdentifier(id: "<Access_ID>"));```

## Run Locally

1. Open `ChatQuickstart.csproj`
2. Run the `ChatQuickstart` project
