
# Project Title

A brief description of what this project does and who it's for

|page_type|languages|products
|---|---|---|
|sample|<table><tr><td>csharp</tr></td></table>|<table><tr><td>azure</td><td>azure-communication-services</td></tr></table>|

# Connect to a room call using Call Automation SDK

In this quickstart sample, we cover how you can use Call Automation SDK to connect to an active Azure Communication Services (ACS) Rooms call with a connect endpoint.
This involves creating a room call with room id and users and enabling PSTN dial out to add PSTN participant(s).

## Prerequisites

- Create an Azure account with an active subscription. For details, see [Create an account for free](https://azure.microsoft.com/free/)
- Create an Azure Communication Services resource. For details, see [Create an Azure Communication Resource](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource). You will need to record your resource **connection string** for this sample.
- Get a phone number for your new Azure Communication Services resource. For details, see [Get a phone number](https://learn.microsoft.com/en-us/azure/communication-services/quickstarts/telephony/get-phone-number?tabs=windows&pivots=programming-language-csharp)

- To know about rooms see https://learn.microsoft.com/en-us/azure/communication-services/concepts/rooms/room-concept
- To join room call see https://learn.microsoft.com/en-us/azure/communication-services/quickstarts/rooms/join-rooms-call?pivots=platform-web

## Before running the sample for the first time

1. Open an instance of PowerShell, Windows Terminal, Command Prompt or equivalent and navigate to the directory that you would like to clone the sample to.
2. git clone `https://github.com/Azure-Samples/Communication-Services-dotnet-quickstarts.git`.
3. Navigate to `ConnectCall-CallAutomation` folder and open `ConnectCall-CallAutomation.sln` file.

## Before running calling rooms quickstart
1. To initiate rooms call with room id https://github.com/Azure-Samples/communication-services-javascript-quickstarts/tree/main/calling-rooms-quickstart
2. cd into the `calling-rooms-quickstart` folder.
3. From the root of the above folder, and with node installed, run `npm install`
4. to run sample `npx webpack serve --config webpack.config.js`

### Setup and host your Azure DevTunnel

[Azure DevTunnels](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/overview) is an Azure service that enables you to share local web services hosted on the internet. Use the commands below to connect your local development environment to the public internet. This creates a tunnel with a persistent endpoint URL and which allows anonymous access. We will then use this endpoint to notify your application of calling events from the ACS Call Automation service.

```bash
devtunnel create --allow-anonymous
devtunnel port create -p 8080
devtunnel host
```

### Configuring application

Open the appsettings.json file to configure the following settings

1. `CallbackUriHost`:  Base url of the app. (For local development replace the above dev tunnel url from the above for the port 8080).
2. `AcsConnectionString`: Azure Communication Service resource's connection string.
3. `AcsPhoneNumber`: Phone number associated with the Azure Communication Service resource. For e.g. "+1425XXXAAAA"
4. `ParticipantPhoneNumber`: Participant phone number. For e.g. "+1425XXXAAAA"

### Run app locally

1. Run the `ConnectCall-CallAutomation` project with `dotnet run`.
2. Browser should pop up with the below page. If not navigate it to `http://localhost:8080/`
3. To connect rooms call, click on the `Connect a call!` button or make a Http get request to https://<CALLBACK_URI>/connectCall

### Creating and connecting to room call.

1. Navigate to `http://localhost:8080/` or devtunnel url to create users and room id ![create room with user](./data/createRoom.png)
2. Open two tabs for Presenter and attendee  ![calling room quickstart](./data/callingRoomQuickstart.png) 
3. Copy tokens for presenter and attendee from ![tokens](./data/tokens.png)
4. Initialize call agent with tokens for both presenter and attendee.
5. Take room id ![room id](./data/roomId.png) and initiate rooms call for both users. ![join room call](./data/joinRoomCall.png)
6. Connect room call with call automation connect call endpoint. ![connect room call](./data/connectCall.png)

