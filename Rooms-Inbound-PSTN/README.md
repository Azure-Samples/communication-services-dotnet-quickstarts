---
page_type: sample
languages:
- csharp
products:
- azure
- azure-communication-services
---

# Azure Communication Services - Inbound PSTN to Rooms (MoveParticipant)

This sample shows how to bring an inbound PSTN caller into an Azure Communication Services Room using the Call Automation **MoveParticipants** API. Like the other Call Automation quickstarts, it is an ASP.NET Core app that receives the `IncomingCall` Event Grid event and Call Automation callbacks on public webhook endpoints (exposed locally with an Azure Dev Tunnel), and you drive the steps from Swagger.

## How it works

**One-time setup:** before running the steps, create an Event Grid **Web Hook** subscription in the Azure portal for the **Incoming Call** event, pointing at `<CallbackUriHost>/api/incomingCall` (see [Subscribe to the IncomingCall event](#subscribe-to-the-incomingcall-event)). Update it if your tunnel address changes.

Run the steps in order from Swagger (`/swagger`):

1. **`POST /createRoom`**: creates a Room with a single `Presenter` participant and PSTN dial-out enabled, and prepares the browser join page. The response contains the page address with a one-time `session` code.
2. **Join the Room**: a Room call only exists on the server once a live participant joins it with a Calling SDK client. Open `https://localhost:8080/roomJoinClient?session=<code>` in your browser (the token and Room Id are pre-filled), click **Join Room** and wait for **Connected**. The page and token are only served on `localhost`, never through the Dev Tunnel.
3. **`POST /connectToRoom`**: connects Call Automation to the Room call with `ConnectCallAsync` + `RoomCallLocator`.
4. **`POST /dialInboundCall`**: Call Automation dials from one ACS-acquired number (`User3`) to another (`User2`). This raises the `Microsoft.Communication.IncomingCall` event.
5. **Answer the call** (automatic): the Event Grid webhook `POST /api/incomingCall` receives the event, ignores calls that aren't from `User3` to `User2`, and answers with `AnswerCallAsync`.
6. **`POST /moveParticipant`**: calls `MoveParticipantsAsync` to move `User3` from the answered call into the Room call. The app tracks `CallConnected` events received on `POST /api/callbacks` and only allows the move once **both** calls are connected (otherwise the move fails with error `8501`). `MoveParticipantSucceeded` / `MoveParticipantFailed` are logged.
7. **`POST /cleanup`**: hangs up every call the app created (deleting a Room doesn't end its call) and deletes the Room.

## Endpoints

### Workflow endpoints (you call these, in order, from Swagger)

| Endpoint | What it does |
|---|---|
| `POST /createRoom` | Creates a 10-day Room with `User1` as `Presenter` and PSTN dial-out enabled, stores the Room id, issues a VoIP access token for `User1` and creates a one-time `session` code for it. Returns the Room id and the join-page address. The token itself is never returned. |
| `POST /connectToRoom` | Returns 409 if `/createRoom` hasn't been called. Otherwise calls `ConnectCallAsync` with a `RoomCallLocator` (this needs a participant to have already joined the Room from the browser) and stores the Room call connection id. Returns that id; the call becomes usable once `CallConnected` arrives on `/api/callbacks`. |
| `POST /dialInboundCall` | Calls `CreateCallAsync` to `User2` with `User3` as the caller ID, which must be an ACS-owned number. The call raises `IncomingCall`, which `/api/incomingCall` answers. Returns the outbound call connection id. |
| `POST /moveParticipant` | Returns 409 if the Room call isn't connected or the inbound call hasn't been answered, and 409 until `CallConnected` has been received for **both** calls. Then calls `MoveParticipantsAsync` to move `User3` from the answered call into the Room call. Returns the Azure error (for example `8522`) if it fails. The final result arrives on `/api/callbacks` and is logged. |
| `POST /cleanup` | Hangs up every call the app created or answered (the Room call, the outbound call and the answered call), ignoring calls that already ended, then deletes the Room and resets the session. Deleting a Room doesn't end its call, so the hang-up is required. |

### Webhook endpoints (Azure calls these)

| Endpoint | What it does |
|---|---|
| `POST /api/incomingCall` | Event Grid webhook for `Microsoft.Communication.IncomingCall`. Replies to the subscription validation event. For an incoming call, it answers only calls from `User3` to `User2` with `AnswerCallAsync` and ignores the rest, since the subscription covers the whole resource. Stores the answered call's connection id as the source call. |
| `POST /api/callbacks` | Call Automation callback. Records `CallConnected` and `CallDisconnected`, and logs `MoveParticipantSucceeded`, `MoveParticipantFailed`, `ConnectFailed` and `CreateCallFailed`. |

### Browser helper endpoints (`localhost` only, hidden from Swagger)

| Endpoint | What it does |
|---|---|
| `GET /roomJoinClient` | Serves `RoomJoinClient/index.html`. Returns 404 unless the request host is `localhost`, so the Dev Tunnel can't reach it. |
| `GET /session?id=<code>` | Exchanges the one-time `session` code for the access token and Room id. Works once. A wrong or reused code, or a non-`localhost` request, returns 404. |
## Prerequisites

- An Azure account with an active subscription. [Create an account for free](https://azure.microsoft.com/free/?WT.mc_id=A261C142F).
- An active Communication Services resource. [Create a Communication Services resource](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource).
- Two ACS-acquired phone numbers: one with inbound calling enabled (`User2`, receives the call) and one with outbound calling enabled (`User3`, used as the caller ID).
- A Communication User identity to join the Room as the presenter. [Quick-create identities for testing](https://learn.microsoft.com/azure/communication-services/quickstarts/identity/quick-create-identity).
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).
- [Azure Dev Tunnel](https://learn.microsoft.com/azure/developer/dev-tunnels/get-started) to expose the local app to Azure. Visual Studio can create one for you (see Setup).
- A current version of Microsoft Edge or Google Chrome, with microphone access allowed for `localhost`.

## Setup

### Host a Dev Tunnel

Azure needs a public address to send events to. Use either option below.

**Option A: from Visual Studio (recommended)**

1. Open `RoomsInboundPSTN.sln`.
2. In the debug toolbar, open the dropdown next to the green start button and choose **Dev Tunnels (no active tunnel)** > **Create a Tunnel...**
3. Sign in, give the tunnel a name, set **Tunnel Type** to **Persistent** (the URL stays the same between sessions) and **Access** to **Public** (Azure must reach it without signing in), then click **OK**.
4. Select the new tunnel in the same dropdown so it is active, then press F5. Visual Studio starts the app and forwards its port through the tunnel.
5. Copy the tunnel URL from the Output window (**Dev Tunnels**). It looks like `https://<id>-8080.<region>.devtunnels.ms`.

If the tunnel forwards a different port from the one in `Properties/launchSettings.json`, correct it under **Debug > Debug Properties > Dev Tunnels**.

**Option B: from the command line**

```
devtunnel create --allow-anonymous
devtunnel port create -p 8080
devtunnel host
```

Use the tunnel URL as `CallbackUriHost` below, with no trailing path.

### Subscribe to the IncomingCall event

In the Azure portal, open your Communication Services resource, go to **Events** and add an Event Grid subscription for **Incoming Call** with the endpoint type **Web Hook** and the address `<CallbackUriHost>/api/incomingCall`. Azure sends a validation event that the app answers automatically, so the app must be running when you create the subscription.

## Configuration

Set these values in `appsettings.json`:

| Setting | Description |
|---|---|
| `AcsConnectionString` | Communication Services connection string. |
| `CallbackUriHost` | Public base URL of this app (your Dev Tunnel URL). Call Automation sends callbacks to `<CallbackUriHost>/api/callbacks`. |
| `User1` | Communication User (`8:acs:...`) that joins the Room as the presenter from the browser client. |
| `User2` | Your ACS-acquired phone number that receives the call and raises `IncomingCall`. |
| `User3` | A second ACS-acquired phone number the call is placed from (required as the outbound caller ID), and the participant that gets moved into the Room. |

Keep real values out of source control (e.g. `git update-index --skip-worktree appsettings.json`) or use a secret store.

## Code Structure

- **Program.cs**: the ASP.NET Core minimal API: Event Grid and callback webhooks plus the workflow endpoints (room creation, connect, dial, move, cleanup).
- **RoomJoinClient/index.html**: a browser client that joins the Room using the ACS Calling SDK, loaded as ES modules from `esm.sh`. It logs the call state, the call end reason (`code`/`subCode`) and the `callId`. Verbose SDK logs are written to the browser console (F12).
- **appsettings.json**: connection string, callback host and the user identities/phone numbers.
- **RoomsInboundPSTN.csproj**: project file (.NET 10). It copies `RoomJoinClient/index.html` to the build output.
- **RoomsInboundPSTN.sln**: Visual Studio solution.

## Run Locally

1. Set up the Dev Tunnel and fill in `appsettings.json`.
2. Press F5 in Visual Studio (with the Dev Tunnel selected), or run `dotnet run` from this folder with the tunnel hosted separately. Swagger opens at `https://localhost:8080/swagger`.
3. Run the steps above in order.

## Troubleshooting

| Symptom | Likely cause / fix |
|---|---|
| `Cannot destructure property 'CallClient' of 'window.AzureCommunicationCalling'` | An old version of the page that used `<script src>` tags. Use the current `index.html`, which imports the SDK as ES modules. |
| Join Room button does nothing | Script error or blocked CDN. Check the browser console (F12) and confirm `esm.sh` is reachable. |
| The join page fields are empty | The one-time `session` code was already used or the app restarted. Call `/createRoom` again. |
| Browser call ends with `code=500, subCode=5701` | A failure on the Azure side or in your network. Check the verbose logs in the F12 console, try another network or VPN setting, and give the logged `callId` to Azure support. |
| `/moveParticipant` returns 409 | The Room call or the inbound call isn't connected yet. Check the logs for `CallConnected` events and retry. |
| `8501` Action is invalid when call is not in Established state | A call isn't fully established yet. Wait for both `CallConnected` events and retry. |
| `8522` Call not found | The Room call or the source call has ended. |
| `CallbackUriHost` changes every run | A temporary tunnel was used. Create a persistent Dev Tunnel, or update `CallbackUriHost` and the Event Grid subscription each time. |
| The incoming call is never answered | Check the Event Grid subscription points at `<CallbackUriHost>/api/incomingCall`, the Dev Tunnel is running, and `User3`/`User2` match the numbers actually used. |
