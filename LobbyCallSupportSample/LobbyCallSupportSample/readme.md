| page_type | languages                               | products                                                                    |
| --------- | --------------------------------------- | --------------------------------------------------------------------------- |
| Sample    | <table><tr><td>DotNet</td><td>C#</td></tr></table> | <table><tr><td>azure</td><td>azure-communication-services</td></tr></table> |

# Call Automation – Lobby Call Support Sample

This sample demonstrates how to use the Call Automation SDK to implement a Lobby Call scenario with Azure Communication Services. Users join a lobby call and remain on hold until a participant in the target call confirms their participation. Once approved, the Call Automation bot automatically connects the lobby user to the designated target call.

---

## Table of Contents
- [Overview](#overview)
- [Design](#design)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [Running the App Locally](#running-the-app-locally)
- [Workflow](#workflow)
- [Troubleshooting](#troubleshooting)

---

## Overview

This project provides a sample implementation for lobby call handling using Azure Communication Services and the Call Automation SDK.

---

## Design

![Lobby Call Support](./Resources/Lobby_Call_Support_Scenario.jpg)

---

## Prerequisites

- **Azure Account:** An Azure account with an active subscription.  
  https://azure.microsoft.com/free/?WT.mc_id=A261C142F.
- **Communication Services Resource:** A deployed Communication Services resource.  
  https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource.
- **Phone Number:** A https://learn.microsoft.com/en-us/azure/communication-services/quickstarts/telephony/get-phone-number in your ACS resource that can make outbound calls.
- **Azure AI Multi-Service Resource:**  
  https://learn.microsoft.com/en-us/azure/cognitive-services/cognitive-services-apis-create-account.
- **Azure Dev Tunnel:**  
  https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/get-started.
- **Client Application:**  
  Navigate to `LobbyCallSupport-Client` folder in https://github.com/Azure-Samples/communication-services-javascript-quickstarts.

---

## Getting Started

### Clone the Source Code

1. Open PowerShell, Windows Terminal, Command Prompt, or equivalent.
2. Navigate to your desired directory.
3. Clone the repository:
   ```sh
   git clone https://github.com/Azure-Samples/Communication-Services-dotnet-quickstarts.git
     
4. Open `LobbyCallSupportSample.sln` in Visual Studio.

### Restore .NET Packages

In the LobbyCallSupportSample directory, run:
```sh
dotnet restore
```
---

## Setup and Host Azure Dev Tunnel

```
# Install Dev Tunnel CLI
dotnet tool install -g Microsoft.DevTunnels.Client

# Authenticate with Azure
devtunnel login

# Create and start a tunnel
devtunnel host -p 7006
```

---
## Configuration

Before running the application, configure the following settings in the `appSettings.json` file:

| Setting                    | Description                                                                                                    | Example Value                                      |
|----------------------------|----------------------------------------------------------------------------------------------------------------|----------------------------------------------------|
| `acsConnectionString`      | The connection string for your Azure Communication Services resource. Find this in the Azure Portal under your resource’s **Keys** section. | `"endpoint=https://<RESOURCE>.communication.azure.com/;accesskey=<KEY>"` |
| `cognitiveServiceEndpoint` | The endpoint for your Azure Cognitive Services resource. Used to play media to participants in the call. | `"https://<COGNITIVE_SERVICE_ENDPOINT>"` |
| `callbackUriHost`          | The base URL where your app will listen for incoming events from Azure Communication Services. For local development, use your Azure Dev Tunnel URL. | `"https://<your-dev-tunnel>.devtunnels.ms"` |
| `acsLobbyCallReceiver`     | ACS identity for the lobby call receiver. Generated using ACS SDK or Azure Portal. | `"8:acs:<GUID>"` |
| `acsTargetCallReceiver`    | ACS identity for the target call receiver. Generated using ACS SDK or Azure Portal. | `"8:acs:<GUID>"` |
| `acsTargetCallSender`      | ACS identity for the target call sender. Generated using ACS SDK or Azure Portal. | `"8:acs:<GUID>"` |

---
### How to Obtain These Values

- **acsConnectionString:**  
  1. Go to the Azure Portal.
  2. Navigate to your Communication Services resource.
  3. Select **Keys & Connection String**.
  4. Copy the **Connection String** value.

- **cognitiveServiceEndpoint:**  
  1. Create an Azure AI Multi-Service resource.
  2. Copy the endpoint from the resource overview page.

- **callbackUriHost:**  
  1. Set up an Azure Dev Tunnel as described in the prerequisites.
  2. Use the public URL provided by the Dev Tunnel as your callback URI host.

- **acsLobbyCallReceiver / acsTargetCallReceiver / acsTargetCallSender:**  
  1. Use the ACS web client or SDK to generate user identities.
  2. Store the generated identity strings here.
#### Example `appSettings.json`

```json
{
  "acsConnectionString": "endpoint=https://<RESOURCE>.communication.azure.com/;accesskey=<KEY>",
  "cognitiveServiceEndpoint": "https://<COGNITIVE_SERVICE_ENDPOINT>",
  "callbackUriHost": "https://<your-dev-tunnel>.devtunnels.ms",
  "acsLobbyCallReceiver": "8:acs:<GUID>",
  "acsTargetCallReceiver": "8:acs:<GUID>",
  "acsTargetCallSender": "8:acs:<GUID>"
}
```
---
## Running the App Locally


1. **Generate ACS identities** for lobby and target participants in **Azure Portal**.
2. **Setup EventSubscription** for incoming calls:
	- Set up a Web hook(`https://<your_dev_tunnel_url>/api/LobbyCallSupportEventHandler`) for callback.
   - Add Filter:
     - Key: `data.to.rawid`, operator: `string contains`, value: `acsLobbyCallReceiver, acsTargetCallReceiver`
3. Use the **JS Client App**, Navigate to `LobbyCallSupport-Client` folder in https://github.com/Azure-Samples/communication-services-javascript-quickstarts.
4. Use the **WebSocket**, `wss://<callbackUriHost-without-https>/ws` in client app for client-server communication.


---
## Workflow

- Start target call in client app `LobbyCallSupport-Client`:
  - Add token for `acsTargetCallSender`.
  - Add user ID for `acsTargetCallReceiver`.
  - Click **Start Call**.
- Incoming call from target sender → server answers → expect `Call Connected` event.
- **Lobby user** calls `acsLobbyCallReceiver` → automated voice plays: `You are currently in a lobby call, we will notify the admin that you are waiting.`
- Target call receives notification (a confirm dialog): `A user is waiting in lobby, do you want to add them to your call?`
- If confirmed, **Lobby user must accept the call when prompted to move in call test app** → expect **MoveParticipantSucceeded** event → lobby user joins target call.
- **If user does not accept the move call prompt → lobby user remains in lobby call.**
- If Target user declined → lobby user will not be moved to target call.

---

## API Testing with Swagger

You can explore and test the available API endpoints using the built-in Swagger UI:

- **Swagger URL:**  
  [https://localhost:7006/swagger/index.html](https://localhost:7006/swagger/index.html)

> If running in a dev tunnel or cloud environment, replace `localhost:7006` with your tunnel's public URL (e.g., `https://<your-dev-tunnel>.devtunnels.ms/swagger/index.html`).

---
## Troubleshooting

### Common Issues
- **Invalid ACS Connection String:**  
  Verify `acsConnectionString` in `appSettings.json`.
- **Callback URL Not Reachable:**  
  Ensure Dev Tunnel is running and URL matches `callbackUriHost`.
- **Phone Number Issues:**  
  Confirm numbers are provisioned and in E.164 format.
- **Identity Errors:**  
  Regenerate ACS identities if invalid.

**Still having trouble?**  
- Review the official https://learn.microsoft.com/azure/communication-services/.
- Search for similar issues or ask questions on https://learn.microsoft.com/answers/topics/azure-communication-services.html.
- Contact your Azure administrator or support team if you suspect a permissions or resource issue.