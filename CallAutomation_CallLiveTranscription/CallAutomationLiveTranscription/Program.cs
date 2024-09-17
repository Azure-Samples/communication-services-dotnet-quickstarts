using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

/* Read config values from appsettings.json*/
var acsConnectionString = builder.Configuration.GetValue<string>("AcsConnectionString");
ArgumentNullException.ThrowIfNullOrEmpty(acsConnectionString);

//var cognitiveServicesEndpoint = builder.Configuration.GetValue<string>("CognitiveServiceEndpoint");
//ArgumentNullException.ThrowIfNullOrEmpty(cognitiveServicesEndpoint);

var acsPhoneNumber = builder.Configuration.GetValue<string>("AcsPhoneNumber");
ArgumentNullException.ThrowIfNullOrEmpty(acsPhoneNumber);

var callbackUriHost = builder.Configuration["VS_TUNNEL_URL"];
ArgumentNullException.ThrowIfNullOrEmpty(callbackUriHost);

var transportUrl = callbackUriHost.Replace("https", "wss")+"ws";
ArgumentNullException.ThrowIfNullOrEmpty(transportUrl);

//var agentPhoneNumber = builder.Configuration.GetValue<string>("AgentPhoneNumber");
/*ArgumentNullException.ThrowIfNullOrEmpty(agentPhoneNumber);*/

/* Call Automation Client */
Uri pmaEndpoint = new UriBuilder("https://uswc-01.sdf.pma.teams.microsoft.com:6448").Uri;
var client = new CallAutomationClient(pmaEndpoint, connectionString: acsConnectionString);

/* Register and make CallAutomationClient accessible via dependency injection */
builder.Services.AddSingleton(client);

builder.Services.AddSingleton<WebSocketHandlerService>();
builder.Services.AddSingleton<MediaService>();

var app = builder.Build();

string helpIVRPrompt = "Welcome to the Contoso Utilities. To access your account, we need to verify your identity. Please enter your date of birth in the format DDMMYYYY using the keypad on your phone. Once we’ve validated your identity we will connect you to the next available agent. Please note this call will be recorded!";
string addAgentPrompt = "Thank you for verifying your identity. We are now connecting you to the next available agent. Please hold the line and we will be with you shortly. Thank you for your patience.";
string incorrectDobPrompt = "Sorry, we were unable to verify your identity based on the date of birth you entered. Please try again. Remember to enter your date of birth in the format DDMMYYYY using the keypad on your phone. Once you've entered your date of birth, press the pound key. Thank you!";
string addParticipantFailurePrompt = "We're sorry, we were unable to connect you to an agent at this time, we will get the next available agent to call you back as soon as possible.";
string goodbyePrompt = "Thank you for calling Contoso Utilities. We hope we were able to assist you today. Goodbye";
string timeoutSilencePrompt = "I’m sorry, I didn’t receive any input. Please type your date of birth in the format of DDMMYYYY.";
string goodbyeContext = "Goodbye";
string addAgentContext = "AddAgent";
string incorrectDobContext = "IncorrectDob";
string addParticipantFailureContext = "FailedToAddParticipant";
string DobRegex = "^(0[1-9]|[12][0-9]|3[01])(0[1-9]|1[012])[12][0-9]{3}$";
var maxTimeout = 2;

string recordingId = string.Empty;
string recordingLocation = string.Empty;

app.MapPost("/api/incomingCall", async (
    [FromBody] EventGridEvent[] eventGridEvents,
    ILogger<Program> logger) =>
{
    foreach (var eventGridEvent in eventGridEvents)
    {
        logger.LogInformation($"Call event received:{eventGridEvent.EventType}");

        /* Handle system events */
        if (eventGridEvent.TryGetSystemEventData(out object eventData))
        {
            /* Handle the subscription validation event. */
            if (eventData is SubscriptionValidationEventData subscriptionValidationEventData)
            {
                var responseData = new SubscriptionValidationResponse
                {
                    ValidationResponse = subscriptionValidationEventData.ValidationCode
                };
                return Results.Ok(responseData);
            }
        }

        if (eventData is AcsIncomingCallEventData incomingCallEventData)
        {
            var callerId = incomingCallEventData.FromCommunicationIdentifier.RawId;
            var callbackUri = new Uri(new Uri(callbackUriHost), $"/api/callbacks/{Guid.NewGuid()}?callerId={callerId}");
            logger.LogInformation($"Incoming call - correlationId: {incomingCallEventData.CorrelationId}, " +
                $"Callback url: {callbackUri}, transport Url: {transportUrl}");

            MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(new Uri(transportUrl),
                MediaStreamingContent.Audio, MediaStreamingAudioChannel.Mixed, startMediaStreaming: true);

            var options = new AnswerCallOptions(incomingCallEventData.IncomingCallContext, callbackUri)
            {
                MediaStreamingOptions = mediaStreamingOptions
            };

            AnswerCallResult answerCallResult = await client.AnswerCallAsync(options);
            var callConnectionMedia = answerCallResult.CallConnection.GetCallMedia();

            /* Use EventProcessor to process CallConnected event */
            var answer_result = await answerCallResult.WaitForEventProcessorAsync();
            if (answer_result.IsSuccess)
            {
                logger.LogInformation($"Received call event: {answer_result.GetType()}, callConnectionID: {answer_result.SuccessResult.CallConnectionId}, " +
                    $"serverCallId: {answer_result.SuccessResult.ServerCallId}");

                /* Play hello prompt to user */
               // await HandleDtmfRecognizeAsync(callConnectionMedia, callerId, helpIVRPrompt, "hellocontext");
            }
            client.GetEventProcessor().AttachOngoingEventProcessor<PlayCompleted>(
                 answerCallResult.CallConnection.CallConnectionId, async (playCompletedEvent) =>
             {
                 logger.LogInformation($"Received call event: {playCompletedEvent.GetType()}, context: {playCompletedEvent.OperationContext}");

                /* if (playCompletedEvent.OperationContext == addAgentContext)
                 {
                     // Add Agent
                     var callInvite = new CallInvite(new PhoneNumberIdentifier(agentPhoneNumber),
                         new PhoneNumberIdentifier(acsPhoneNumber));

                     var addParticipantOptions = new AddParticipantOptions(callInvite)
                     {
                         OperationContext = addAgentContext
                     };

                     var addParticipantResult = await answerCallResult.CallConnection.AddParticipantAsync(addParticipantOptions);
                     logger.LogInformation($"Adding agent to the call: {addParticipantResult.Value?.InvitationId}");
                 }
                 else if (playCompletedEvent.OperationContext == goodbyeContext || playCompletedEvent.OperationContext == addParticipantFailureContext)
                 {
                     await answerCallResult.CallConnection.HangUpAsync(true);
                 }*/
             });
            client.GetEventProcessor().AttachOngoingEventProcessor<RecognizeCompleted>(
                answerCallResult.CallConnection.CallConnectionId, async (recognizeCompletedEvent) =>
            {
                logger.LogInformation($"Received call event: {recognizeCompletedEvent.GetType()}, context: {recognizeCompletedEvent.OperationContext}");
                if (recognizeCompletedEvent.RecognizeResult is DtmfResult)
                {
                    var dtmfResult = recognizeCompletedEvent.RecognizeResult as DtmfResult;

                    //Take action for Recognition through DTMF 
                    var tones = dtmfResult?.ConvertToString();
                    Regex regex = new(DobRegex);
                    Match match = regex.Match(tones);
                    if (match.Success)
                    {
                        await HandlePlayAsync(callConnectionMedia, addAgentPrompt, addAgentContext);
                    }
                    else
                    {
                        await HandleDtmfRecognizeAsync(callConnectionMedia, callerId, incorrectDobPrompt, incorrectDobContext);
                    }
                }
            });
            client.GetEventProcessor().AttachOngoingEventProcessor<AddParticipantSucceeded>(
               answerCallResult.CallConnection.CallConnectionId, async (addParticipantSucceededEvent) =>
               {
                   logger.LogInformation($"Received call event: {addParticipantSucceededEvent.GetType()}, context: {addParticipantSucceededEvent.OperationContext}");
               });
            client.GetEventProcessor().AttachOngoingEventProcessor<ParticipantsUpdated>(
              answerCallResult.CallConnection.CallConnectionId, async (participantsUpdatedEvent) =>
              {
                  logger.LogInformation($"Received call event: {participantsUpdatedEvent.GetType()}, participants: {participantsUpdatedEvent.Participants.Count()}, sequenceId: {participantsUpdatedEvent.SequenceNumber}");
              });
            client.GetEventProcessor().AttachOngoingEventProcessor<CallDisconnected>(
              answerCallResult.CallConnection.CallConnectionId, async (callDisconnectedEvent) =>
              {
                  logger.LogInformation($"Received call event: {callDisconnectedEvent.GetType()}");
              });
            client.GetEventProcessor().AttachOngoingEventProcessor<AddParticipantFailed>(
              answerCallResult.CallConnection.CallConnectionId, async (addParticipantFailedEvent) =>
              {
                  logger.LogInformation($"Received call event: {addParticipantFailedEvent.GetType()}, CorrelationId: {addParticipantFailedEvent.CorrelationId}, " +
                      $"subCode: {addParticipantFailedEvent.ResultInformation?.SubCode}, message: {addParticipantFailedEvent.ResultInformation?.Message}, context: {addParticipantFailedEvent.OperationContext}");

                  await HandlePlayAsync(callConnectionMedia, addParticipantFailurePrompt, addParticipantFailureContext);
              });
            client.GetEventProcessor().AttachOngoingEventProcessor<PlayFailed>(
                answerCallResult.CallConnection.CallConnectionId, async (playFailedEvent) =>
            {
                logger.LogInformation($"Received call event: {playFailedEvent.GetType()}, CorrelationId: {playFailedEvent.CorrelationId}, " +
                    $"subCode: {playFailedEvent.ResultInformation?.SubCode}, message: {playFailedEvent.ResultInformation?.Message}, context: {playFailedEvent.OperationContext}");

                await answerCallResult.CallConnection.HangUpAsync(true);
            });

            client.GetEventProcessor().AttachOngoingEventProcessor<RecognizeFailed>(
                answerCallResult.CallConnection.CallConnectionId, async (recognizeFailedEvent) =>
            {
                logger.LogInformation($"Received call event: {recognizeFailedEvent.GetType()}, CorrelationId: {recognizeFailedEvent.CorrelationId}, " +
                    $"subCode: {recognizeFailedEvent.ResultInformation?.SubCode}, message: {recognizeFailedEvent.ResultInformation?.Message}, context: {recognizeFailedEvent.OperationContext}");

                if (MediaEventReasonCode.RecognizeInitialSilenceTimedOut.Equals(recognizeFailedEvent.ResultInformation?.SubCode.Value.ToString()) && maxTimeout > 0)
                {
                    logger.LogInformation("Retrying recognize...");
                    maxTimeout--;
                    await HandleDtmfRecognizeAsync(callConnectionMedia, callerId, timeoutSilencePrompt, "retryContext");
                }
                else
                {
                    logger.LogInformation("Playing goodbye message...");
                    await HandlePlayAsync(callConnectionMedia, goodbyePrompt, goodbyeContext);
                }
            });

            client.GetEventProcessor().AttachOngoingEventProcessor<MediaStreamingStarted>(
                answerCallResult.CallConnection.CallConnectionId, async (mediaStreamingStarted) =>
                {
                    logger.LogInformation($"Received media streaming event: {mediaStreamingStarted.GetType()}");
                });

            client.GetEventProcessor().AttachOngoingEventProcessor<MediaStreamingStopped>(
                answerCallResult.CallConnection.CallConnectionId, async (mediaStreamingStopped) =>
                {
                    logger.LogInformation("Received media streaming event: {type}", mediaStreamingStopped.GetType());
                });

            client.GetEventProcessor().AttachOngoingEventProcessor<MediaStreamingFailed>(
                answerCallResult.CallConnection.CallConnectionId, async (mediaStreamingFailed) =>
                {
                    logger.LogInformation($"Received media streaming event: {mediaStreamingFailed.GetType()}, " +
                        $"SubCode: {mediaStreamingFailed?.ResultInformation?.SubCode}, Message: {mediaStreamingFailed?.ResultInformation?.Message}");
                });
        }
    }
    return Results.Ok();
});

// api to handle call back events
app.MapPost("/api/callbacks/{contextId}", async (
    [FromBody] CloudEvent[] cloudEvents,
    [FromRoute] string contextId,
    [Required] string callerId,
    CallAutomationClient callAutomationClient,
    ILogger<Program> logger) =>
{
    var eventProcessor = client.GetEventProcessor();
    eventProcessor.ProcessEvents(cloudEvents);
    return Results.Ok();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

async Task HandleDtmfRecognizeAsync(CallMedia callConnectionMedia, string callerId, string message, string context)
{
    // Play greeting message
    var greetingPlaySource = new TextSource(message)
    {
        VoiceName = "en-US-NancyNeural"
    };

    var recognizeOptions =
        new CallMediaRecognizeDtmfOptions(
            targetParticipant: CommunicationIdentifier.FromRawId(callerId), maxTonesToCollect: 8)
        {
            InterruptPrompt = false,
            InterToneTimeout = TimeSpan.FromSeconds(5),
            Prompt = greetingPlaySource,
            OperationContext = context,
            InitialSilenceTimeout = TimeSpan.FromSeconds(15)
        };

    var recognize_result = await callConnectionMedia.StartRecognizingAsync(recognizeOptions);
}

async Task HandlePlayAsync(CallMedia callConnectionMedia, string textToPlay, string context)
{
    // Play message
    var playSource = new TextSource(textToPlay)
    {
        VoiceName = "en-US-NancyNeural"
    };

    var playOptions = new PlayToAllOptions(playSource) { OperationContext = context };
    await callConnectionMedia.PlayToAllAsync(playOptions);
}

app.MapPost("/sendAudioFile", async (HttpContext context, MediaService mediaService) =>
{
    string filePath = string.Format("..//test.pcm");  // PCM file to stream

    try
    {
        await mediaService.SendAudioFileAsync(filePath);
        context.Response.StatusCode = StatusCodes.Status200OK;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Exception -> {ex}");
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
    }
});

app.UseWebSockets();
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            var mediaService = context.RequestServices.GetRequiredService<MediaService>();

            var webSocket = await context.WebSockets.AcceptWebSocketAsync();

            // Set the single WebSocket connection
            await mediaService.HandleWebSocketConnectionAsync(webSocket);
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }
    else
    {
        await next(context);
    }
});

app.Run();