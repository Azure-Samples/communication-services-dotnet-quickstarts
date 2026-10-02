---
page_type: sample
languages:
- csharp
products:
- azure
- azure-communication-services
---

# Azure Communication Services - Inbound PSTN to Rooms (MoveParticipant)

This sample shows how to bring an inbound PSTN caller into an Azure Communication Services Room using the Call Automation **MoveParticipants** API.

## How it works

1. **Create a Room**: a Room is created with a single `Presenter` participant and PSTN dial-out enabled.
2. **Join the Room**: a Room call only exists on the server once a live participant joins it with a Calling SDK client. The app issues a VoIP access token for the presenter, starts a small local HTTP server, and opens `RoomJoinClient/index.html` in your default browser with the token and Room Id pre-filled. Click **Join Room**, wait for **Connected**, then press ENTER in the console.
3. **Place an inbound PSTN call**: Call Automation dials from a PSTN number to your ACS-acquired number. This raises the `Microsoft.Communication.IncomingCall` event.
4. **Answer the call**: the app polls a Storage Queue (the Event Grid subscription destination) for the `IncomingCall` event and answers it with `AnswerCallAsync`. Expired incoming call contexts (error `8523`) are discarded.
5. **Move the caller into the Room**:
   - Connects Call Automation to the Room call with `ConnectCallAsync` + `RoomCallLocator`.
   - Waits until **both** the Room call and the answered PSTN (source) call are `Connected`.
   - Calls `MoveParticipantsAsync`. It retries up to 5 times if it gets error `8501` ("Action is invalid when call is not in Established state").
   - If error `8522` ("Call not found") occurs, it reports which of the two calls no longer exists.
6. **Clean up**: the Room is always deleted at the end.

## Prerequisites

- An Azure account with an active subscription. [Create an account for free](https://azure.microsoft.com/free/?WT.mc_id=A261C142F).
- An active Communication Services resource. [Create a Communication Services resource](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource).
- An ACS-acquired phone number with inbound calling enabled. [Get a phone number](https://learn.microsoft.com/azure/communication-services/quickstarts/telephony/get-phone-number).
- A Communication User identity to join the Room as the presenter. [Quick-create identities for testing](https://learn.microsoft.com/azure/communication-services/quickstarts/identity/quick-create-identity).
- An Azure Storage account with a queue (default name `incoming-call-events`).
- An Event Grid subscription on your Communication Services resource for the `Microsoft.Communication.IncomingCall` event. Its endpoint type must be **Storage Queue** and point at the queue above. You don't need a public webhook.
- [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0).
- A current version of Microsoft Edge or Google Chrome, with microphone access allowed for `localhost`.

## Code Structure

- **Program.cs**: the MoveParticipant flow (room creation, the Room join client launcher, PSTN dial and answer, and the move).
- **RoomJoinClient/index.html**: a browser client that joins the Room using the ACS Calling SDK, loaded as ES modules from `esm.sh`. It logs the call state, the call end reason (`code`/`subCode`) and the `callId`. Verbose SDK logs are written to the browser console (F12).
- **RoomsInboundPSTN.csproj**: project file (.NET 6). It copies `RoomJoinClient/index.html` to the build output.
- **RoomsInboundPSTN.sln**: Visual Studio solution.

## Configuration

In `Program.cs`, set:

| Setting | Description |
|---|---|
| `connectionString` | Communication Services connection string (or the `ACS_CONNECTION_STRING` environment variable). |
| `storageQueueConnectionString` | Storage account connection string (or the `STORAGE_QUEUE_CONNECTION_STRING` environment variable). |
| `incomingCallQueueName` | Queue that Event Grid delivers `IncomingCall` events to (or `INCOMING_CALL_QUEUE_NAME`; default `incoming-call-events`). |
| `user1` | Communication User that joins the Room as the presenter from the browser client. |
| `user2` | Your ACS-acquired phone number (the call target that raises `IncomingCall`). |
| `user3` | The PSTN number the call is placed from, and the participant that gets moved into the Room. |

> **Important:** don't commit real connection strings or access keys. Use environment variables or a secret store.

## Run Locally

1. Open `RoomsInboundPSTN.sln` in Visual Studio, or run `dotnet run` from this folder.
2. When the browser opens the Room join page, click **Join Room** and wait for **Connected**.
3. Press ENTER in the console to place the PSTN call and move the caller into the Room.

The join page is served from `http://localhost:<port>/index.html`, because browsers block ES module scripts on `file://` URLs. It only works while the console app is running.

## Troubleshooting

| Symptom | Likely cause / fix |
|---|---|
| `Cannot destructure property 'CallClient' of 'window.AzureCommunicationCalling'` | An old version of the page that used `<script src>` tags. Use the current `index.html`, which imports the SDK as ES modules. |
| Join Room button does nothing | Script error or blocked CDN. Check the browser console (F12) and confirm `esm.sh` is reachable. |
| Browser call ends with `code=500, subCode=5701` | A failure on the Azure side or in your network. Check the verbose logs in the F12 console, try another network or VPN setting, and give the logged `callId` to Azure support. |
| `8501` Action is invalid when call is not in Established state | A call isn't fully established yet. The app waits for both calls and retries. If it keeps failing, confirm the PSTN call is still active. |
| `8522` Call not found | The Room call or the source call has ended. Check the diagnostics line to see which one. |
| `8523` Incoming Call Context is invalid | A stale event was left in the queue. It's discarded automatically. |
