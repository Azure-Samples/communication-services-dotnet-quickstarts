using Azure.Communication.Rooms;
using Azure.Communication;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Communication.Identity;
using Azure.Communication.CallAutomation;
using Azure.Messaging.EventGrid;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;

namespace RoomsQuickstart
{
    class Program
    {
        private static readonly JsonElement config = LoadConfig();

        private static JsonElement LoadConfig()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Configuration file not found: '{path}'.");
            }

            using var doc = JsonDocument.Parse(File.ReadAllText(path), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
            return doc.RootElement.Clone();
        }

        private static string? GetSetting(string name) =>
            config.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString())
                ? value.GetString()
                : null;

        private static readonly string connectionString = GetSetting("AcsConnectionString")
            ?? throw new InvalidOperationException("'AcsConnectionString' is not set in appsettings.json. Please set it to your Azure Communication Services connection string.");

        // Storage account connection string + queue name that your Event Grid subscription for the
        // 'Microsoft.Communication.IncomingCall' event is configured to deliver to (Storage Queue destination).
        // This lets us retrieve the IncomingCall event by polling the queue instead of hosting a public webhook.
        private static readonly string storageQueueConnectionString = GetSetting("StorageQueueConnectionString")
            ?? throw new InvalidOperationException("'StorageQueueConnectionString' is not set in appsettings.json. Please set it to your Azure Storage account connection string.");
        private static readonly string incomingCallQueueName = GetSetting("IncomingCallQueueName") ?? "incoming-call-events";

        static QueueClient? incomingCallQueueClient = null;
        public static QueueClient IncomingCallQueueClient
        {
            // Event Grid writes messages to Storage Queues using Base64 encoding, so the
            // QueueClient must be configured to decode accordingly, otherwise MessageText
            // will contain raw Base64 and fail to parse as JSON.
            get => incomingCallQueueClient ??= new QueueClient(
                storageQueueConnectionString,
                incomingCallQueueName,
                new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 });
            set => incomingCallQueueClient = value;
        }
        static CallAutomationClient? callAutomationClient = null;
        public static CallAutomationClient CallAutomation
        {
            get => callAutomationClient ??= new CallAutomationClient(connectionString);
            set => callAutomationClient = value;
        }

        static RoomsClient? roomsCollection = null;
        public static RoomsClient RoomCollection
        {
            get => roomsCollection ??= new RoomsClient(connectionString);
            set => roomsCollection = value;
        }

        static CommunicationIdentityClient? identityClient = null;
        public static CommunicationIdentityClient IdentityClient
        {
            get => identityClient ??= new CommunicationIdentityClient(connectionString);
            set => identityClient = value;
        }

        private static string GetRequiredSetting(string name) =>
            GetSetting(name) ?? throw new InvalidOperationException($"'{name}' is not set in appsettings.json.");

        static readonly CommunicationUserIdentifier user1 = new CommunicationUserIdentifier(GetRequiredSetting("User1"));
        static readonly PhoneNumberIdentifier user2 = new PhoneNumberIdentifier(GetRequiredSetting("User2"));
        static readonly PhoneNumberIdentifier user3 = new PhoneNumberIdentifier(GetRequiredSetting("User3"));

        static async Task Main(string[] args)
        {
            Console.WriteLine("Azure Communication Services - Rooms Inbound PSTN (MoveParticipant)");

            string? roomId = null;
            try
            {
                roomId = await CreateRoom();
                if (roomId is null)
                {
                    return;
                }

                // A Room's call only exists server-side once a real participant has joined it via a
                // Calling SDK client. Launch the bundled browser client (RoomJoinClient/index.html)
                // with a token + Room Id pre-filled and wait for it to join before continuing.
                string presenterToken = await GetRoomAccessTokenAsync(user1);
                Console.WriteLine($"Presenter ({user1.RawId}) access token:\n{presenterToken}\n");
                Console.WriteLine($"Room Id to join: {roomId}");

                LaunchRoomJoinClient(presenterToken, roomId);

                Console.WriteLine("Opened the Room join page in your default browser with the token and " +
                    "Room Id pre-filled. Click 'Join Room' there, wait until it shows 'Connected', " +
                    "then press ENTER here to continue...");
                Console.ReadLine();

                // Dial from user3 to user2. Both must be ACS-acquired numbers: CreateCallAsync requires
                // the source caller ID to be owned by this resource, and user2 receives the call and
                // raises the 'Microsoft.Communication.IncomingCall' event.
                string? outboundCallConnectionId = await DialPstnToPstnAsync(user3, user2);
                if (outboundCallConnectionId is null)
                {
                    return;
                }
                activeCallConnectionIds.Add(outboundCallConnectionId);

                // Wait for the resulting inbound PSTN call event to arrive (via the IncomingCall
                // event delivered to a Storage Queue), answer it, and get the resulting CallConnectionId.
                string? sourceCallConnectionId = await WaitForAndAnswerIncomingCallAsync(TimeSpan.FromSeconds(30));
                if (sourceCallConnectionId is not null)
                {
                    activeCallConnectionIds.Add(sourceCallConnectionId);
                    bool moved = await InboundPstnDialInToRoom(roomId, user3, sourceCallConnectionId);
                    if (moved)
                    {
                        // Keep the Room and calls alive until the user is done; cleanup runs afterwards.
                        Console.WriteLine("\nThe caller is now in the Room. Press ENTER to end the session and clean up " +
                            "(hang up calls, delete the Room)...");
                        Console.ReadLine();
                    }
                }
                else
                {
                    Console.WriteLine("No incoming call was answered within the timeout window; skipping move.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to perform MoveParticipant flow -> {ex}");
            }
            finally
            {
                await HangUpActiveCallsAsync();
                await DeleteVisitedQueueMessagesAsync();
                roomJoinClientListener?.Close();

                if (roomId is not null)
                {
                    await DeleteRoom(roomId);
                }
            }
        }

        // Call connections created by this app; hung up on exit because deleting a Room doesn't end its call.
        static readonly List<string> activeCallConnectionIds = new List<string>();

        static async Task HangUpActiveCallsAsync()
        {
            foreach (string callConnectionId in activeCallConnectionIds)
            {
                try
                {
                    await CallAutomation.GetCallConnection(callConnectionId).HangUpAsync(true);
                    Console.WriteLine($"Hung up call connection '{callConnectionId}'.");
                }
                catch (RequestFailedException ex) when (ex.Status == 404 || ex.ErrorCode == "8522")
                {
                    // Already ended.
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to hang up call connection '{callConnectionId}': {ex.Message}");
                }
            }

            activeCallConnectionIds.Clear();
        }

        static bool IsPhoneParticipant(JsonElement data, string propertyName, PhoneNumberIdentifier expected)
        {
            if (!data.TryGetProperty(propertyName, out var participant))
                return false;

            string? actual = null;
            if (participant.TryGetProperty("phoneNumber", out var phone) &&
                phone.TryGetProperty("value", out var value))
            {
                actual = value.GetString();
            }
            else if (participant.TryGetProperty("rawId", out var rawId))
            {
                actual = rawId.GetString()?.Replace("4:", string.Empty);
            }

            return string.Equals(actual?.TrimStart('+'), expected.PhoneNumber?.TrimStart('+'), StringComparison.Ordinal);
        }

        // Queue messages already handled. They are hidden for a while instead of being deleted right away,
        // and deleted during cleanup so late-arriving or related events aren't lost mid-flow.
        static readonly Dictionary<string, string> visitedQueueMessages = new Dictionary<string, string>();
        static readonly TimeSpan visitedMessageHideTime = TimeSpan.FromMinutes(10);

        static async Task MarkQueueMessageVisitedAsync(QueueMessage message)
        {
            try
            {
                // Passing no new text keeps the content; the new pop receipt is needed for the later delete.
                var receipt = await IncomingCallQueueClient.UpdateMessageAsync(
                    message.MessageId, message.PopReceipt, visibilityTimeout: visitedMessageHideTime);
                visitedQueueMessages[message.MessageId] = receipt.Value.PopReceipt;
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"Could not mark queue message '{message.MessageId}' as visited: {ex.Message}");
            }
        }

        static async Task DeleteVisitedQueueMessagesAsync()
        {
            foreach (var (messageId, popReceipt) in visitedQueueMessages)
            {
                try
                {
                    await IncomingCallQueueClient.DeleteMessageAsync(messageId, popReceipt);
                }
                catch (RequestFailedException ex)
                {
                    Console.WriteLine($"Could not delete queue message '{messageId}': {ex.Message}");
                }
            }

            visitedQueueMessages.Clear();
        }

        /// <summary>
        /// Polls the Storage Queue that the 'Microsoft.Communication.IncomingCall' Event Grid
        /// subscription delivers to, waiting for an inbound PSTN call event. When one arrives,
        /// it answers the call via Call Automation and returns the resulting CallConnectionId.
        /// </summary>
        static async Task<string?> WaitForAndAnswerIncomingCallAsync(TimeSpan timeout)
        {
            Console.WriteLine("\n---------Waiting for incoming PSTN call (polling Storage Queue)---------\n");

            // Callback Uri required by AnswerCallOptions; not actually used since this sample
            // doesn't process callback events.
            var callbackUri = new Uri("https://localhost/api/callbacks");

            var deadline = DateTime.UtcNow.Add(timeout);
            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    QueueMessage[] messages = await IncomingCallQueueClient.ReceiveMessagesAsync(maxMessages: 32);
                    foreach (var message in messages)
                    {
                        EventGridEvent[] events = EventGridEvent.ParseMany(BinaryData.FromString(message.MessageText));
                        bool leaveUntouched = false;

                        foreach (var egEvent in events)
                        {
                            if (egEvent.EventType == "Microsoft.Communication.IncomingCall")
                            {
                                using var callDataDoc = JsonDocument.Parse(egEvent.Data.ToString());

                                // The queue is resource-wide; only answer the call placed from user3 to user2.
                                if (!IsPhoneParticipant(callDataDoc.RootElement, "from", user3) ||
                                    !IsPhoneParticipant(callDataDoc.RootElement, "to", user2))
                                {
                                    // Leave it untouched (no delete) so other consumers can still process it.
                                    Console.WriteLine("Ignoring incoming call event that doesn't match the expected caller/callee.");
                                    leaveUntouched = true;
                                    continue;
                                }

                                string incomingCallContext = callDataDoc.RootElement.GetProperty("incomingCallContext").GetString()!;

                                try
                                {
                                    AnswerCallOptions answerOptions = new AnswerCallOptions(incomingCallContext, callbackUri);
                                    AnswerCallResult answerResult = await CallAutomation.AnswerCallAsync(answerOptions);
                                    string callConnectionId = answerResult.CallConnectionProperties.CallConnectionId;

                                    Console.WriteLine($"Answered incoming PSTN call. CallConnectionId: {callConnectionId}");

                                    await MarkQueueMessageVisitedAsync(message);
                                    return callConnectionId;
                                }
                                catch (RequestFailedException ex) when (ex.ErrorCode == "8523")
                                {
                                    // The incomingCallContext had already expired; drop it and keep waiting.
                                    Console.WriteLine("Incoming call context was invalid/expired; discarding this event and continuing to wait.");
                                    continue;
                                }
                            }
                        }

                        // Handled (or not relevant): hide it and delete it during cleanup so it doesn't keep reappearing.
                        if (!leaveUntouched)
                        {
                            await MarkQueueMessageVisitedAsync(message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error while polling for incoming call event: {ex.Message}");
                }

                await Task.Delay(TimeSpan.FromSeconds(2));
            }

            return null;
        }

        /// <summary>
        /// Places an outbound PSTN call from <paramref name="fromPstnNumber"/> to
        /// <paramref name="toPstnNumber"/> using Call Automation, which triggers the
        /// 'Microsoft.Communication.IncomingCall' event for <paramref name="toPstnNumber"/>.
        /// </summary>
        static async Task<string?> DialPstnToPstnAsync(
            PhoneNumberIdentifier fromPstnNumber,
            PhoneNumberIdentifier toPstnNumber)
        {
            try
            {
                Console.WriteLine($"\n---------Dialing from '{fromPstnNumber.PhoneNumber}' to '{toPstnNumber.PhoneNumber}'---------\n");

                var callbackUri = new Uri("https://localhost/api/callbacks");
                var callInvite = new CallInvite(toPstnNumber, fromPstnNumber);
                var createCallOptions = new CreateCallOptions(callInvite, callbackUri);

                CreateCallResult createCallResult = await CallAutomation.CreateCallAsync(createCallOptions);
                string callConnectionId = createCallResult.CallConnectionProperties.CallConnectionId;

                Console.WriteLine($"Placed outbound call. CallConnectionId: {callConnectionId}");
                return callConnectionId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to dial from '{fromPstnNumber.PhoneNumber}' to '{toPstnNumber.PhoneNumber}', ex --> {ex}");
                return null;
            }
        }

        /// <summary>
        /// Brings an inbound PSTN caller into an ACS Room:
        ///   1. Connects Call Automation to the Room using a RoomCallLocator.
        ///   2. Uses the MoveParticipant API to move the PSTN caller from the source call into the Room call.
        /// </summary>
        static async Task<bool> InboundPstnDialInToRoom(
            string roomId,
            PhoneNumberIdentifier pstnCaller,
            string sourceCallConnectionId)
        {
            try
            {
                Console.WriteLine("\n---------Inbound PSTN dial-in to Room (MoveParticipant)---------\n");

                var callbackUri = new Uri("https://localhost/api/callbacks");

                // 1. Connect Call Automation to the Room so we have a call we can move participants into.
                var connectOptions = new ConnectCallOptions(new RoomCallLocator(roomId), callbackUri);
                ConnectCallResult connectResult = await CallAutomation.ConnectCallAsync(connectOptions);
                string roomCallConnectionId = connectResult.CallConnectionProperties.CallConnectionId;
                activeCallConnectionIds.Add(roomCallConnectionId);
                Console.WriteLine($"Connected to room '{roomId}'. Room call connection id: {roomCallConnectionId}");

                var roomCallConnection = CallAutomation.GetCallConnection(roomCallConnectionId);

                // The room call must reach the Connected state before participants can be moved into it,
                // otherwise MoveParticipantsAsync fails with error code 8501.
                if (!await WaitForCallConnectedAsync(roomCallConnection, TimeSpan.FromSeconds(10)))
                {
                    Console.WriteLine($"Room call connection '{roomCallConnectionId}' did not reach the Connected state in time; aborting move.");
                    return false;
                }

                // The answered PSTN (source) call must also be Established, otherwise the move fails with 8501.
                var sourceCallConnection = CallAutomation.GetCallConnection(sourceCallConnectionId);
                if (!await WaitForCallConnectedAsync(sourceCallConnection, TimeSpan.FromSeconds(15), "Source"))
                {
                    Console.WriteLine($"Source call connection '{sourceCallConnectionId}' did not reach the Connected state in time; aborting move.");
                    return false;
                }

                // 2. Move the inbound PSTN caller from their current call into the Room call.
                var moveOptions = new MoveParticipantsOptions(
                    targetParticipants: new CommunicationIdentifier[] { pstnCaller },
                    fromCall: sourceCallConnectionId);

                try
                {
                    Response<MoveParticipantsResult> moveResult;
                    for (int attempt = 1; ; attempt++)
                    {
                        try
                        {
                            moveResult = await roomCallConnection.MoveParticipantsAsync(moveOptions);
                            break;
                        }
                        catch (RequestFailedException ex) when (ex.ErrorCode == "8501" && attempt < 5)
                        {
                            Console.WriteLine($"MoveParticipants attempt {attempt} failed with 8501 (call not yet Established); retrying...");
                            await Task.Delay(TimeSpan.FromSeconds(2));
                        }
                    }
                    int status = moveResult.GetRawResponse().Status;
                    if (status is >= 200 and <= 299)
                    {
                        Console.WriteLine($"MoveParticipant initiated: {pstnCaller.PhoneNumber} -> room '{roomId}'.");
                        return true;
                    }

                    Console.WriteLine($"MoveParticipant failed with status code: {status}");
                    return false;
                }
                catch (RequestFailedException ex) when (ex.ErrorCode == "8522")
                {
                    // "Call not found" doesn't indicate which call disappeared; check both.
                    bool roomCallStillExists = await CallConnectionExistsAsync(roomCallConnection);
                    bool sourceCallStillExists = await CallConnectionExistsAsync(CallAutomation.GetCallConnection(sourceCallConnectionId));
                    Console.WriteLine(
                        $"MoveParticipantsAsync returned 'Call not found' (8522). Diagnostics: " +
                        $"roomCallConnection '{roomCallConnectionId}' exists={roomCallStillExists}, " +
                        $"sourceCallConnection '{sourceCallConnectionId}' exists={sourceCallStillExists}.");
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed inbound PSTN dial-in to room '{roomId}', ex --> {ex}");
                return false;
            }
        }

        /// <summary>
        /// Polls the given call connection until it reaches the Connected state (or the timeout elapses).
        /// </summary>
        static async Task<bool> WaitForCallConnectedAsync(CallConnection callConnection, TimeSpan timeout, string label = "Room")
        {
            var deadline = DateTime.UtcNow.Add(timeout);
            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    var properties = await callConnection.GetCallConnectionPropertiesAsync();
                    var state = properties.Value.CallConnectionState;
                    Console.WriteLine($"{label} call connection state: {state}");
                    if (state == CallConnectionState.Connected)
                        return true;
                }
                catch (RequestFailedException ex)
                {
                    Console.WriteLine($"Error while polling call connection state: Status={ex.Status}, ErrorCode={ex.ErrorCode}, Message={ex.Message}");
                    if (ex.Status == 404)
                    {
                        Console.WriteLine("Call connection no longer exists (404); aborting wait.");
                        return false;
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            return false;
        }

        /// <summary>
        /// Checks whether the given call connection still exists (hasn't been disconnected/torn down).
        /// </summary>
        static async Task<bool> CallConnectionExistsAsync(CallConnection callConnection)
        {
            try
            {
                await callConnection.GetCallConnectionPropertiesAsync();
                return true;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return false;
            }
        }

        static HttpListener? roomJoinClientListener;

        /// <summary>
        /// Starts a minimal background HTTP server on a free localhost port that serves the Room join
        /// client page, and returns its base URL (e.g. "http://localhost:51234/").
        /// </summary>
        static string StartRoomJoinClientServer(string htmlPath, string sessionId, string token, string roomId)
        {
            // The token is handed out at most once, and only to a request that presents the nonce.
            int sessionConsumed = 0;
            var portFinder = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            portFinder.Start();
            int port = ((IPEndPoint)portFinder.LocalEndpoint).Port;
            portFinder.Stop();

            string baseUrl = $"http://localhost:{port}/";
            var listener = new HttpListener();
            listener.Prefixes.Add(baseUrl);
            listener.Start();
            roomJoinClientListener = listener;

            _ = Task.Run(async () =>
            {
                while (listener.IsListening)
                {
                    HttpListenerContext context;
                    try
                    {
                        context = await listener.GetContextAsync();
                    }
                    catch (Exception)
                    {
                        break;
                    }

                    try
                    {
                        string path = context.Request.Url!.AbsolutePath;
                        if (path == "/" || path.Equals("/index.html", StringComparison.OrdinalIgnoreCase))
                        {
                            byte[] content = await File.ReadAllBytesAsync(htmlPath);
                            context.Response.ContentType = "text/html; charset=utf-8";
                            context.Response.ContentLength64 = content.Length;
                            await context.Response.OutputStream.WriteAsync(content, 0, content.Length);
                        }
                        else if (path.Equals("/session", StringComparison.OrdinalIgnoreCase))
                        {
                            string? id = context.Request.QueryString["id"];
                            if (id == sessionId && Interlocked.Exchange(ref sessionConsumed, 1) == 0)
                            {
                                byte[] content = JsonSerializer.SerializeToUtf8Bytes(new { token, roomId });
                                context.Response.ContentType = "application/json";
                                context.Response.Headers["Cache-Control"] = "no-store";
                                context.Response.ContentLength64 = content.Length;
                                await context.Response.OutputStream.WriteAsync(content, 0, content.Length);
                            }
                            else
                            {
                                context.Response.StatusCode = 404;
                            }
                        }
                        else
                        {
                            context.Response.StatusCode = 404;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Room join client server error: {ex.Message}");
                    }
                    finally
                    {
                        context.Response.Close();
                    }
                }
            });

            return baseUrl;
        }

        /// <summary>
        /// Opens the bundled browser-based Room join client (RoomJoinClient/index.html) in the default
        /// browser, with the access token and Room Id pre-filled via a one-time localhost session endpoint.
        /// </summary>
        static void LaunchRoomJoinClient(string token, string roomId)
        {
            try
            {
                string htmlPath = Path.Combine(AppContext.BaseDirectory, "RoomJoinClient", "index.html");
                if (!File.Exists(htmlPath))
                {
                    htmlPath = Path.Combine(Directory.GetCurrentDirectory(), "RoomJoinClient", "index.html");
                }

                if (!File.Exists(htmlPath))
                {
                    Console.WriteLine($"Could not find RoomJoinClient/index.html to auto-launch (looked under '{htmlPath}'). " +
                        "Open it manually and paste in the token and Room Id printed above.");
                    return;
                }

                // Serve the page over http://localhost because browsers block ES module scripts on file:// URLs.
                // The token is not placed in the URL; the page redeems a one-time nonce for it instead.
                string sessionId = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16));
                string baseUrl = StartRoomJoinClientServer(htmlPath, sessionId, token, roomId);
                string url = $"{baseUrl}index.html?session={sessionId}";
                Console.WriteLine($"Room join client served at {baseUrl}index.html");

                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to auto-launch the Room join client: {ex.Message}. " +
                    "Open 'RoomJoinClient/index.html' manually and paste in the token and Room Id printed above.");
            }
        }

        /// <summary>
        /// Issues a Communication Services access token (VoIP scope) for the given user identity,
        /// used by the Calling SDK client to join the Room as a live participant.
        /// </summary>
        static async Task<string> GetRoomAccessTokenAsync(CommunicationUserIdentifier user)
        {
            Azure.Core.AccessToken tokenResponse = await IdentityClient.GetTokenAsync(
                user,
                new[] { CommunicationTokenScope.VoIP });
            return tokenResponse.Token;
        }

        static async Task<string?> CreateRoom()
        {
            try
            {
                Console.WriteLine("\n---------Create Room---------\n");
                var createRoomOptions = new CreateRoomOptions()
                {
                    ValidFrom = DateTimeOffset.UtcNow,
                    ValidUntil = DateTimeOffset.UtcNow.AddDays(10),
                    PstnDialOutEnabled = true,
                    Participants = new List<RoomParticipant>
                    {
                        new RoomParticipant(user1) { Role = ParticipantRole.Presenter }
                    }
                };

                CommunicationRoom createdRoom = await RoomCollection.CreateRoomAsync(createRoomOptions, CancellationToken.None);
                Console.WriteLine($"room_id: {createdRoom.Id}");
                return createdRoom.Id;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create room, ex --> {ex}");
                return null;
            }
        }

        static async Task DeleteRoom(string roomId)
        {
            try
            {
                Console.WriteLine("\n---------Delete Room---------\n");
                await RoomCollection.DeleteRoomAsync(roomId, CancellationToken.None);
                Console.WriteLine($"Successfully deleted room with id: {roomId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to delete room with id: {roomId} ex --> {ex}");
            }
        }
    }
}
