using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Azure.Messaging;
using Azure;
using Azure.AI.OpenAI;

var builder = WebApplication.CreateBuilder(args);

//Get ACS Connection String from appsettings.json
var acsConnectionString = builder.Configuration.GetValue<string>("AcsConnectionString");
//Call Automation Client
var client = new CallAutomationClient(connectionString: acsConnectionString);

//Grab the Cognitive Services endpoint from appsettings.json
var cognitiveServicesEndpoint = builder.Configuration.GetValue<string>("CognitiveServiceEndpoint");

//Register and make CallAutomationClient accessible via dependency injection
builder.Services.AddSingleton(client);
var app = builder.Build();

var devTunnelUri = builder.Configuration.GetValue<string>("DevTunnelUri");
var maxTimeout = 2;

app.MapGet("/", () => "Hello ACS CallAutomation!");

app.MapPost("/api/incomingCall", async (
    [FromBody] EventGridEvent[] eventGridEvents,
    ILogger<Program> logger) =>
{
    foreach (var eventGridEvent in eventGridEvents)
    {
        logger.LogInformation($"Incoming Call event received.");

        // Handle system events
        if (eventGridEvent.TryGetSystemEventData(out object eventData))
        {
            // Handle the subscription validation event.
            if (eventData is SubscriptionValidationEventData subscriptionValidationEventData)
            {
                var responseData = new SubscriptionValidationResponse
                {
                    ValidationResponse = subscriptionValidationEventData.ValidationCode
                };
                return Results.Ok(responseData);
            }
        }

        var jsonObject = Helper.GetJsonObject(eventGridEvent.Data);
        var callerId = Helper.GetCallerId(jsonObject);
        var incomingCallContext = Helper.GetIncomingCallContext(jsonObject);
        var callbackUri = new Uri(devTunnelUri + $"/api/callbacks/{Guid.NewGuid()}?callerId={callerId}");
        var options = new AnswerCallOptions(incomingCallContext, callbackUri)
        {
            CognitiveServicesEndpoint = new Uri(cognitiveServicesEndpoint)
        };

        AnswerCallResult answerCallResult = await client.AnswerCallAsync(options);
        Console.WriteLine($"Answered call for connection id: {answerCallResult.CallConnection.CallConnectionId}");

        //Use EventProcessor to process CallConnected event
        var answer_result = await answerCallResult.WaitForEventProcessorAsync();
        if (answer_result.IsSuccess)
        {
            Console.WriteLine($"Call connected event received for connection id: {answer_result.SuccessResult.CallConnectionId}");
            var callConnectionMedia = answerCallResult.CallConnection.GetCallMedia();
            await HandleRecognizeAsync(callConnectionMedia, callerId, "Hello. How can I help?");
        }

        client.GetEventProcessor().AttachOngoingEventProcessor<PlayCompleted>(answerCallResult.CallConnection.CallConnectionId, async (playCompletedEvent) =>
        {
            Console.WriteLine($"Play completed event received for connection id: {playCompletedEvent.CallConnectionId}. Hanging up call...");
            await HandleHangupAsync(answerCallResult.CallConnection);
        });

        client.GetEventProcessor().AttachOngoingEventProcessor<PlayFailed>(answerCallResult.CallConnection.CallConnectionId, async (playFailedEvent) =>
        {
            Console.WriteLine($"Play failed event received for connection id: {playFailedEvent.CallConnectionId}. Hanging up call...");
            await HandleHangupAsync(answerCallResult.CallConnection);
        });

        client.GetEventProcessor().AttachOngoingEventProcessor<RecognizeCompleted>(answerCallResult.CallConnection.CallConnectionId, async (recognizeCompletedEvent) =>
        {
            Console.WriteLine($"Recognize completed event received for connection id: {recognizeCompletedEvent.CallConnectionId}");
            var speech_result = recognizeCompletedEvent.RecognizeResult as SpeechResult;

            if (!string.IsNullOrWhiteSpace(speech_result?.Speech))
            {
                Console.WriteLine($"Recognized speech: {speech_result.Speech}");
                var chatGPTResponse = await GetChatGPTResponse(speech_result?.Speech);
                await HandleChatResponse(chatGPTResponse, answerCallResult.CallConnection.GetCallMedia(), callerId, logger);
            }
        });

        client.GetEventProcessor().AttachOngoingEventProcessor<RecognizeFailed>(answerCallResult.CallConnection.CallConnectionId, async (recognizeFailedEvent) =>
        {
            var callConnectionMedia = answerCallResult.CallConnection.GetCallMedia();

            if (MediaEventReasonCode.RecognizeInitialSilenceTimedOut.Equals(recognizeFailedEvent.ResultInformation.SubCode.Value.ToString()) && maxTimeout > 0)
            {
                Console.WriteLine($"Recognize failed event received for connection id: {recognizeFailedEvent.CallConnectionId}. Retrying recognize...");
                maxTimeout--;
                await HandleRecognizeAsync(callConnectionMedia, callerId, "I've noticed that you have been silent. Are you still there?");
            }
            else
            {
                Console.WriteLine($"Recognize failed event received for connection id: {recognizeFailedEvent.CallConnectionId}. Playing goodbye message...");
                await HandlePlayAsync(callConnectionMedia);
            }
        });
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

async Task HandleChatResponse(string chatResponse, CallMedia callConnectionMedia, string callerId, ILogger logger)
{
    var chatGPTResponseSource = new TextSource(chatResponse)
    {
        VoiceName = "en-US-NancyNeural"
    };

    var recognizeOptions =
        new CallMediaRecognizeSpeechOptions(
            targetParticipant: CommunicationIdentifier.FromRawId(callerId))
        {
            InterruptPrompt = false,
            InitialSilenceTimeout = TimeSpan.FromSeconds(15),
            Prompt = chatGPTResponseSource,
            OperationContext = "OpenAISample",
            EndSilenceTimeout = TimeSpan.FromMilliseconds(500)
        };

    var recognize_result = await callConnectionMedia.StartRecognizingAsync(recognizeOptions);
}

async Task<string> GetChatGPTResponse(string speech_input)
{
    var key = builder.Configuration.GetValue<string>("AzureOpenAIServiceKey");
    var endpoint = builder.Configuration.GetValue<string>("AzureOpenAIServiceEndpoint");

    var ai_client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(key));
    //var ai_client = new OpenAIClient(openAIApiKey: ""); //Use this initializer if you're using a non-Azure OpenAI API Key

    var chatCompletionsOptions = new ChatCompletionsOptions()
    {
        Messages = {
            new ChatMessage(ChatRole.System, "You are a helpful assistant."),
            new ChatMessage(ChatRole.User, $"In less than 200 characters: respond to this question: {speech_input}?"),
                    },
        MaxTokens = 1000
    };

    Response<ChatCompletions> response = await ai_client.GetChatCompletionsAsync(
        deploymentOrModelName: builder.Configuration.GetValue<string>("AzureOpenAIDeploymentModelName"),
        chatCompletionsOptions);

    var response_content = response.Value.Choices[0].Message.Content;

    return response_content;
}

async Task HandleRecognizeAsync(CallMedia callConnectionMedia, string callerId, string message)
{
    // Play greeting message
    var greetingPlaySource = new TextSource(message)
    {
        VoiceName = "en-US-NancyNeural"
    };

    var recognizeOptions =
        new CallMediaRecognizeSpeechOptions(
            targetParticipant: CommunicationIdentifier.FromRawId(callerId))
        {
            InterruptPrompt = false,
            InitialSilenceTimeout = TimeSpan.FromSeconds(15),
            Prompt = greetingPlaySource,
            OperationContext = "GetFreeFormText",
            EndSilenceTimeout = TimeSpan.FromMilliseconds(500)
        };

    var recognize_result = await callConnectionMedia.StartRecognizingAsync(recognizeOptions);
}

async Task HandlePlayAsync(CallMedia callConnectionMedia)
{
    // Play goodbye message
    var GoodbyePlaySource = new TextSource("Goodbye.")
    {
        VoiceName = "en-US-NancyNeural"
    };

    await callConnectionMedia.PlayToAllAsync(GoodbyePlaySource);
}

async Task HandleHangupAsync(CallConnection callConnection)
{
    var GoodbyePlaySource = new TextSource("Goodbye.")
    {
        VoiceName = "en-US-NancyNeural"
    };

    await callConnection.HangUpAsync(true);
}

app.Run();