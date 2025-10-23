| page_type | languages                               | products                                                                    |
| --------- | --------------------------------------- | --------------------------------------------------------------------------- |
| sample    | <table><tr><td>DotNet</td><td>C#</td></tr></table> | <table><tr><td>azure</td><td>azure-communication-services</td></tr></table> |

# Call Automation - Lobby Call Support Sample

This sample demonstrates how to utilize the Call Automation SDK to implement a Lobby Call scenario. Users join a lobby call and remain on hold until an user in the target call confirms their participation. Once approved, Call Automation (bot) automatically connects the lobby users to the designated target call.
The sample uses a client application (java script sample) available in [Web Client Quickstart](https://github.com/Azure-Samples/communication-services-javascript-quickstarts/tree/users/v-kuppu/LobbyCallConfirmSample).

# Design


![Lobby Call Support](./Resources/Lobby_Call_Support_Scenario.jpg)

## Prerequisites
- An Azure account with an active subscription. [Create an account for free](https://azure.microsoft.com/free/?WT.mc_id=A261C142F).
- A deployed Communication Services resource. [Create a Communication Services resource](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource).
- A [phone number](https://learn.microsoft.com/en-us/azure/communication-services/quickstarts/telephony/get-phone-number) in your Azure Communication Services resource that can make outbound calls.
- Create Azure AI Multi Service resource. For details, see [Create an Azure AI Multi service](https://learn.microsoft.com/en-us/azure/cognitive-services/cognitive-services-apis-create-account).
- Create and host a Azure Dev Tunnel. Instructions [here](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/get-started)
- A Client application that can make calls to the Azure Communication Services resource. This can be a web client or a mobile client. You can use [Web Client Quickstart](https://github.com/Azure-Samples/communication-services-javascript-quickstarts/tree/users/v-kuppu/LobbyCallConfirmSample).

## Before running the sample for the first time

1. Open an instance of PowerShell, Windows Terminal, Command Prompt or equivalent and navigate to the directory that you would like to clone the sample to.
2. git clone https://github.com/Azure-Samples/Communication-Services-dotnet-quickstarts.git.
3. Navigate to `LobbyCallSupportSample` folder and open `LobbyCallSupportSample.sln` file.

### Setup and host your Azure DevTunnel

[Azure DevTunnels](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/overview) is an Azure service that enables you to share local web services hosted on the internet. Use the commands below to connect your local development environment to the public internet. This creates a tunnel with a persistent endpoint URL and which allows anonymous access. We will then use this endpoint to notify your application of calling events from the ACS Call Automation service.

```
   # Install the dev tunnel CLI tool
   dotnet tool install -g Microsoft.DevTunnels.Client
   # Authenticate with Azure
   devtunnel login
   # Create and start a new tunnel
   devtunnel create --allow-anonymous
   devtunnel port create -p 8080
   devtunnel host
```

### Configuring application

Open `appSettings.json` file to configure the following settings

1. `acsConnectionString`: Azure Communication Service resource's connection string.
2. `cognitiveServiceEndpoint`: Cognitive Service resource's endpoint.
   - This is used to play media to the participants in the call.
   - For more information, see [Create an Azure AI Multi service](https://learn.microsoft.com/en-us/azure/cognitive-services/cognitive-services-apis-create-account).
3. `callbackUriHost`: Base url of the app. (For local development use dev tunnel url)
4. `acsIdentityForLobbyCallReceiver`: ACS Inbound Phone Number
5. `acsIdentityForTargetCallReceiver`: ACS Phone Number to make the first call, external user number in real time
6. `acsIdentityForTargetCallSender`: ACS identity generated using web client

## Run app locally

1. Generate an Azure Communication Services identity for the lobby call receiver and target call receiver. You can do this from the `Azure Portal(ACS Resource > Identities > User Access Tokens > Generate Identity and USER ACCESS TOKEN)`.
2.  Setup EventSubscription(Incoming) with filter for `TO.DATA.RAWID = <ACS_GENERATED_ID_TARGET_CALL_RECEIVER>, <ACS_GENERATED_ID_LOBBY_CALL_RECEIVER>`.
3. Setup webhook for Incoming calls to point to `https://<your_dev_tunnel_url>/callbacks/incomingcall` in EventSubscription(Incoming).
4. Setup the following keys in the config/constants
	 ```
	 "acsConnectionString": "<acsConnectionString>",
	 "cognitiveServiceEndpoint": "<cognitiveServiceEndpoint>",
	 "callbackUriHost": "<callbackUriHost>",
	 "acsIdentityForLobbyCallReceiver": "<acsIdentityForLobbyCallReceiver>",(Generate Voice Calling Identity in Azure Portal)
	 "acsIdentityForTargetCallReceiver": "<acsIdentityForTargetCallReceiver>",(Generate Voice Calling Identity in Azure Portal)
	 "acsIdentityForTargetCallSender": "<acsIdentityForTargetCallSender>",(Generate Voice Calling Identity in Azure Portal)```
5. Define a websocket with url as `ws://your-websocket-server-url:port/ws` in your application(program.cs) to send and receive messages from and to the client application.
6. Define a Client application(JS Hero App in this case) that receives and responds to server notifications. Client application is available at [Web Client Quickstart](https://github.com/Azure-Samples/communication-services-javascript-quickstarts/tree/users/v-kuppu/LobbyCallConfirmSample).
  
7. Start the target call in Client application, 
    - Add token of target call sender(token would be generated in Azure user & tokens section).
	- Add user id of the target call receiver `<ACS_GENERATED_ID_FOR_LOBBY_CALL_RECEIVER>`.
	- Click on `Start Call` button to initiate the call.
8. Expect Call Connected event in /callbacks as the server app answers incoming call from target call sender to target call receiver.
9. Start a call from any Client Application (app used to make outbound calls) to `acsIdentityForLobbyCallReceiver`, call will be answered by the server app and automated voice will be played to lobby user with the text `You are currently in a lobby call, we will notify the admin that you are waiting.`
10. Once the play is completed, Target call will be notified with `A user is waiting in lobby, do you want to add the lobby user to your call?`.
11. Once the Target call confirms from client application, Move `acsIdentityForLobbyCallReceiver` in the backend sample.
12. If Target user says no, then no MOVE will be performed.
13. Ensure MoveParticipantSucceeded event is received in `/callbacks` endpoint.
14. Ensure the output in the logs shows the the additional lobby user in the target call. The number of participants in the target call are increased by adding the lobby user, then lobby call gets disconnected after the moving the lobby user(as lobby user is already moved into the target call).