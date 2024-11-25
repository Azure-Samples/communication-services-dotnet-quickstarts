using Azure;
using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using CliWrap;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Nodes;
using static System.Net.WebRequestMethods;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//var openIdConfigUrl = "https://acscallautomation.communication.azure.com/calling/.well-known/acsopenidconfiguration";

//var allowedAudience = "a46cfa6a-73ce-4b07-9cfd-c3c28496ca08";//"6aeaee8c-8285-46d0-b018-493a75a1faca";
 string _contentLocation = "";

// Add Azure Communication Services CallAutomation OpenID configuration
// 1. Load OpenID Connect metadata
var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
    builder.Configuration["OpenIdConfigUrl"],
    new OpenIdConnectConfigurationRetriever());

var openIdConfig = await configurationManager.GetConfigurationAsync();

// 2. Register JWT authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Configuration = openIdConfig;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidAudience = builder.Configuration["AllowedAudience"]
        };
    });

builder.Services.AddAuthorization();

//Get ACS Connection String from appsettings.json
var acsConnectionString = builder.Configuration.GetValue<string>("AcsConnectionString");
var acsPhoneNumber = builder.Configuration.GetValue<string>("AcsPhoneNumber");
var cognitiveServiceEndpoint = builder.Configuration.GetValue<string>("AcsCogService");
var voipId = builder.Configuration.GetValue<string>("AcsVoipId");

var targetPstnNumber = builder.Configuration.GetValue<string>("MyPSTNNumber"); 
ArgumentNullException.ThrowIfNullOrEmpty(acsConnectionString);

//Call Automation Client
///var pmaEndpoint = new Uri("https://usea-01.msit.pma.teams.microsoft.com:6448");
//var pmaEndpoint = new Uri("https://uswc-01.sdf.pma.teams.microsoft.com:6448");
//var pmaEndpoint = new Uri("https://uswc-01.sdf.pma.teams.microsoft.com:6448");
//var pmaEndpoint = new Uri("https://nextpma.plat.skype.com:6448");
//var pmaEndpoint = new Uri("https://euno-02.sdf.pma.teams.microsoft.com:6448");
//var pmaEndpoint = new Uri("https://msit.pma.teams.microsoft.com:6448");
var pmaEndpoint = new Uri("https://pma.plat.skype.com:6448");
var client = new CallAutomationClient(pmaEndpoint, connectionString: acsConnectionString);
//var client = new CallAutomationClient(connectionString: acsConnectionString);

string helpIVRPrompt = "Welcome to the Contoso Utilities. To access your account, we need to verify your identity. Please enter your date of birth in the format DDMMYYYY using the keypad on your phone. Once we’ve validated your identity we will connect you to the next available agent. Please note this call will be recorded!";
var audioBaseUrl = builder.Configuration["VS_TUNNEL_URL"];
string callConnectionId = "test";
string callerId = "4:+14389798484";
string callLocatorid = "test";
string websocketUri = "test";
// text to play
const string SpeechToTextVoice = "en-US-NancyNeural";
const string MainMenu = "Please say confirm or say cancel to proceed further.";
const string ConfirmedText = "Thank you for confirming your appointment tomorrow at 9am, we look forward to meeting with you.";
const string CancelText = """
Your appointment tomorrow at 9am has been cancelled. Please call the bank directly s
if you would like to rebook for another date and time.
""";
const string CustomerQueryTimeout = "I’m sorry I didn’t receive a response, please try again.";
const string NoResponse = "I didn't receive an input, we will go ahead and confirm your appointment. Goodbye";
const string InvalidAudio = "I’m sorry, I didn’t understand your response, please try again.";
const string ConfirmChoiceLabel = "Confirm";
const string RetryContext = "retry";
const string dtmfPrompt = "Thank you for the update. Please type  one two three four on your keypad to close call.";
string cancelLabel = "Cancel";

bool isPlayInterrupt = false;

var app = builder.Build();

var appBaseUrl = Environment.GetEnvironmentVariable("VS_TUNNEL_URL")?.TrimEnd('/');
Console.WriteLine($"callback url: " + appBaseUrl);

if (string.IsNullOrEmpty(appBaseUrl))
{
    var websiteHostName = Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME");
    Console.WriteLine($"websiteHostName :{websiteHostName}");
    appBaseUrl = $"https://{websiteHostName}";
    Console.WriteLine($"appBaseUrl :{appBaseUrl}");
}

app.MapPost("/createPSTNCall",
    [SwaggerOperation(Summary = "Creates a PSTN call with optional media streaming settings.")]
async (
        ILogger<Program> logger,
        [FromQuery, SwaggerParameter("Specifies the audio channel type. Default is 'Mixed'.", Required = false)] string? audioChannel = "Mixed",
        [FromQuery, SwaggerParameter("Enables bidirectional media streaming. Default is 'null'.", Required = false)] bool? enableBidirectional = null,
        [FromQuery, SwaggerParameter("Specifies the audio format. Default is 'null'.", Required = false)] string? audioFormat = null,
        [FromQuery, SwaggerParameter("Starts media streaming if true. Default is 'true'.", Required = false)] bool? startMediaStreaming = true) =>
    {

        PhoneNumberIdentifier target = new PhoneNumberIdentifier(targetPstnNumber);
        PhoneNumberIdentifier caller = new PhoneNumberIdentifier(acsPhoneNumber);
        var callbackUri = new Uri(new Uri(appBaseUrl), "/api/callbacks");
        CallInvite callInvite = new CallInvite(target, caller);

        websocketUri = appBaseUrl.Replace("https", "wss") + "/ws";
        var transcriptionWebsocket = "wss://0d1d-20-185-152-70.ngrok-free.app";

        /************** MediaStreamingOptions **********************/
        /* var mediaStreamingOptions = new MediaStreamingOptions(
                  new Uri("wss://1856-40-117-66-148.ngrok-free.app"),
                  MediaStreamingAudioChannel.Unmixed
                  )
         {
             EnableBidirectional = true,
             AudioFormat = AudioFormat.Pcm16KMono,
             StartMediaStreaming = true,
             MediaStreamingContent = MediaStreamingContent.Audio,
             EnableDtmfTones = true,
         };

         var defaultTrans = new TranscriptionOptions(
                     new Uri(websocketUri),
                    "en-US"
                     )
         {
             StartTranscription = true

         };*/

        var mediaStreamingOptions = new MediaStreamingOptions(
            new Uri(websocketUri),
            MediaStreamingContent.Audio,
            MediaStreamingAudioChannel.Unmixed,
            MediaStreamingTransport.Websocket,
            true)
        { EnableBidirectional = true};

        /************** CallIntelligenceOptions **********************/
        var callIntelligent = new CallIntelligenceOptions
        {
            CognitiveServicesEndpoint = new Uri(cognitiveServiceEndpoint)
        };

        var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
        {
          // MediaStreamingOptions = mediaStreamingOptions,
          // TranscriptionOptions = defaultTrans,
           CallIntelligenceOptions = callIntelligent,
           //EnableLoopbackAudio = true,

        };

        CreateCallResult createCallResult = await client.CreateCallAsync(createCallOptions);

        callConnectionId = createCallResult.CallConnectionProperties.CallConnectionId;

        logger.LogInformation($"============ Correlation id: {createCallResult.CallConnectionProperties.CorrelationId}");

        logger.LogInformation($"************ CallConnection id: {createCallResult.CallConnectionProperties.CallConnectionId}");
    })
.WithName("CreatePSTNCall")
.WithMetadata(new SwaggerOperationAttribute { Summary = "Creates a PSTN call with media streaming settings." });

app.MapPost("/createVOIPCall",
    [SwaggerOperation(Summary = "Creates a PSTN call with optional media streaming settings.")]
async (
        ILogger<Program> logger,
        [FromQuery, SwaggerParameter("Specifies the audio channel type. Default is 'Mixed'.", Required = false)] string? audioChannel = "Mixed",
        [FromQuery, SwaggerParameter("Enables bidirectional media streaming. Default is 'null'.", Required = false)] bool? enableBidirectional = null,
        [FromQuery, SwaggerParameter("Specifies the audio format. Default is 'null'.", Required = false)] string? audioFormat = null,
        [FromQuery, SwaggerParameter("Starts media streaming if true. Default is 'true'.", Required = false)] bool? startMediaStreaming = true) =>
    {

        
        var callbackUri = new Uri(new Uri(appBaseUrl), "/api/callbacks");
        CallInvite callInvite = new CallInvite(new CommunicationUserIdentifier("8:acs:9c7fd23d-fedd-4ba9-8093-3fef2b6cb41d_00000028-d921-d48f-78f0-b03a0d00330b"));

        websocketUri = appBaseUrl.Replace("https", "wss") + "/ws";
        var transcriptionWebsocket = "wss://0d1d-20-185-152-70.ngrok-free.app";

        /************** MediaStreamingOptions **********************/
        /* var mediaStreamingOptions = new MediaStreamingOptions(
                  new Uri("wss://1856-40-117-66-148.ngrok-free.app"),
                  MediaStreamingAudioChannel.Unmixed
                  )
         {
             EnableBidirectional = true,
             AudioFormat = AudioFormat.Pcm16KMono,
             StartMediaStreaming = true,
             MediaStreamingContent = MediaStreamingContent.Audio,
             EnableDtmfTones = true,
         };

         var defaultTrans = new TranscriptionOptions(
                     new Uri(websocketUri),
                    "en-US"
                     )
         {
             StartTranscription = true

         };*/

        var mediaStreamingOptions = new MediaStreamingOptions(
            new Uri(websocketUri),
            MediaStreamingContent.Audio,
            MediaStreamingAudioChannel.Unmixed,
            MediaStreamingTransport.Websocket,
            true)
        { EnableBidirectional = true };

        /************** CallIntelligenceOptions **********************/
        var callIntelligent = new CallIntelligenceOptions
        {
            CognitiveServicesEndpoint = new Uri(cognitiveServiceEndpoint)
        };

        var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
        {
            // MediaStreamingOptions = mediaStreamingOptions,
            // TranscriptionOptions = defaultTrans,
           // CallIntelligenceOptions = callIntelligent,
            //EnableLoopbackAudio = true,

        };

        CreateCallResult createCallResult = await client.CreateCallAsync(createCallOptions);

        callConnectionId = createCallResult.CallConnectionProperties.CallConnectionId;

        logger.LogInformation($"============ Correlation id: {createCallResult.CallConnectionProperties.CorrelationId}");

        logger.LogInformation($"************ CallConnection id: {createCallResult.CallConnectionProperties.CallConnectionId}");
    })
.WithName("CreateVOIPCall")
.WithMetadata(new SwaggerOperationAttribute { Summary = "Creates a PSTN call with media streaming settings." });

app.MapPost("/createGroupCall", async (ILogger<Program> logger) =>
{
    var callbackUri = new Uri(new Uri(appBaseUrl), "/api/callbacks");
    var pstnEndpoint = new PhoneNumberIdentifier(targetPstnNumber);
    var voipEndpoint = new CommunicationUserIdentifier(voipId);

    
    /*MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(
      new Uri("wss://0rv989s0-8081.inc1.devtunnels.ms/ws"),
      MediaStreamingContent.Audio,
      MediaStreamingAudioChannel.Mixed,
      StreamingTransport.Websocket,
      false);*/

    var groupCallOptions = new CreateGroupCallOptions(new List<CommunicationIdentifier> { pstnEndpoint, voipEndpoint }, callbackUri)
    {
       // MediaStreamingOptions = mediaStreamingOptions,
        CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServiceEndpoint) },
        SourceCallerIdNumber = new PhoneNumberIdentifier(acsPhoneNumber), // This is the Azure Communication Services provisioned phone number for the caller
    };
    CreateCallResult response = await client.CreateGroupCallAsync(groupCallOptions);
});

app.MapPost("/api/incomingCall", async (
    [FromBody] EventGridEvent[] eventGridEvents,
    ILogger<Program> logger) =>
{
    foreach (var eventGridEvent in eventGridEvents)
    {
        Console.WriteLine($"Incoming Call event received.");

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
        callerId = Helper.GetCallerId(jsonObject);
        var incomingCallContext = Helper.GetIncomingCallContext(jsonObject);
        var callbackUri = new Uri(new Uri(appBaseUrl), $"/api/callbacks/{Guid.NewGuid()}?callerId={callerId}");
        

        websocketUri = appBaseUrl.Replace("https", "wss") + "/ws";

        logger.LogInformation($"WebSocket Url: {websocketUri}");
        logger.LogInformation($"------ callerId: {callerId}");
        var transcriptionWebsocket = "wss://0d1d-20-185-152-70.ngrok-free.app";

        /* var mediaStreamingOptions = new MediaStreamingOptions(
                 new Uri(websocketUri),
                 MediaStreamingAudioChannel.Unmixed
                 )
         {
             EnableBidirectional = true,
             AudioFormat = AudioFormat.Pcm24KMono,
             StartMediaStreaming = true,
             MediaStreamingContent = MediaStreamingContent.Audio,
             EnableDtmfTones = true,
         };

         var transcriptionOptions = new TranscriptionOptions(
                     new Uri(transcriptionWebsocket),
                    "en-US"
                     )
         {
             StartTranscription = true

         };*/

        var mediaStreamingOptions = new MediaStreamingOptions(
            new Uri("wss://d9aa515a0cc1.ngrok-free.app"),
            MediaStreamingContent.Audio,
            MediaStreamingAudioChannel.Unmixed,
            MediaStreamingTransport.Websocket,
            true);

        /************** CallIntelligenceOptions **********************/
        var callIntelligent = new CallIntelligenceOptions
        {
            CognitiveServicesEndpoint = new Uri(cognitiveServiceEndpoint)
        };

        var options = new AnswerCallOptions(incomingCallContext, callbackUri)
         {
              //MediaStreamingOptions = mediaStreamingOptions,

             //TranscriptionOptions = transcriptionOptions,
             /*/CallIntelligenceOptions = new CallIntelligenceOptions
             {
                 CognitiveServicesEndpoint = new Uri(cognitiveServiceEndpoint)
             },*/
             OperationContext = "answerCallContext",
             //EnableLoopbackAudio = true,

        };

         AnswerCallResult answerCallResult = await client.AnswerCallAsync(options);
         callConnectionId = answerCallResult.CallConnectionProperties.CallConnectionId;
         callLocatorid = answerCallResult.CallConnectionProperties.ServerCallId;
         logger.LogInformation($"Answered call for connection id: {answerCallResult.CallConnection.CallConnectionId}");
         logger.LogInformation($"------Answered call for CorrelationId: {answerCallResult.CallConnectionProperties.CorrelationId}");
         logger.LogInformation($"------Answered call for serverCallId: {callLocatorid}");
        /* client.GetEventProcessor().AttachOngoingEventProcessor<CallConnected>(answerCallResult.CallConnection.CallConnectionId, async (mediaStreamingStopped) =>
         {
             logger.LogInformation($"------- Call mediaStreamingStopped accepted event received for connection id: {mediaStreamingStopped.CallConnectionId}.");
         });
         client.GetEventProcessor().AttachOngoingEventProcessor<MediaStreamingStarted>(answerCallResult.CallConnection.CallConnectionId, async (mediaStreamingStarted) =>
         {
             logger.LogInformation($"\"------- Call mediaStreamingStarted accepted event: {mediaStreamingStarted.CallConnectionId}.");
             var resultInformation = mediaStreamingStarted.ResultInformation;
             //logger.LogError("Encountered error during call transfer, message={msg}, code={code}, subCode={subCode}", resultInformation?.Message, resultInformation?.Code, resultInformation?.SubCode);
            // answerCallResult.CallConnection.GetCallMedia().StopMediaStreaming();

         });*/
    }
    return Results.Ok();
});

app.MapPost("/connectApiWithServerCallLocator", async ([FromQuery] string id, ILogger<Program> logger) =>
{
var callbackUri = new Uri(new Uri(appBaseUrl), "/api/callbacks");
//CallLocator callLocator = new RoomCallLocator("99484006759534582");
//CallLocator callLocator = new GroupCallLocator("29228d3e-040e-4656-a70e-890ab4e173e5");
CallLocator callLocator = new ServerCallLocator(id);
//websocketUri = "wss://6e41-20-185-152-66.ngrok-free.app" + "/ws";
websocketUri = appBaseUrl.Replace("https", "wss") + "/ws";
var transcriptionWebsocket = "wss://0d1d-20-185-152-70.ngrok-free.app";
    /*var mediaStreamingOptions = new MediaStreamingOptions(
                new Uri("wss://13fd-40-117-66-147.ngrok-free.app"),
                MediaStreamingAudioChannel.Unmixed
                )
    {
        EnableBidirectional = true,
        AudioFormat = AudioFormat.Pcm24KMono,
        StartMediaStreaming = true,
        MediaStreamingContent = MediaStreamingContent.Audio,
        EnableDtmfTones = true,
    };

    var transcriptionOptions = new TranscriptionOptions(
                new Uri(websocketUri),
               "en-US"
                )
    {
        StartTranscription = true
    
    };*/

    var mediaStreamingOptions = new MediaStreamingOptions(
           new Uri(websocketUri),
           MediaStreamingContent.Audio,
           MediaStreamingAudioChannel.Unmixed,
           MediaStreamingTransport.Websocket,
           true)
    {  };

    var callbackuri2 = new Uri("https://c2c9-20-185-152-75.ngrok-free.app");

    ConnectCallOptions connectCallOptions = new ConnectCallOptions(callLocator, callbackuri2)
    {
        MediaStreamingOptions = mediaStreamingOptions,
        /*TranscriptionOptions = transcriptionOptions,
        CallIntelligenceOptions = new CallIntelligenceOptions
        {
            CognitiveServicesEndpoint = new Uri(cognitiveServiceEndpoint)
        }*/
    };


    ConnectCallResult result = await client.ConnectCallAsync(connectCallOptions);
    logger.LogInformation($"********************* CALL CONNECTION ID : {result.CallConnectionProperties.CallConnectionId}");
    logger.LogInformation($"********************* CALL Correlation ID : {result.CallConnectionProperties.CorrelationId}");
});

app.MapGet("/getCallConnection", async (
    [FromQuery] string id,
    ILogger<Program> logger) =>
{
    logger.LogInformation("playToALL");

    PlaySource fileSource = new FileSource(new Uri("https://stmikhailccaaptest.blob.core.windows.net/assets/out.wav"));

    var test = new PlayToAllOptions(new List<PlaySource> { fileSource }) { OperationContext = "hellocontext" };
    var con = id ?? callConnectionId;
    //client.GetCallConnection(con).GetCallConnectionProperties();

    return Results.Ok(client.GetCallConnection(con).GetCallConnectionProperties());
});


app.MapPost("/playToALL", async (
    [FromQuery] string id,
    ILogger<Program> logger) =>
{
    logger.LogInformation("playToALL");

    PlaySource fileSource = new FileSource(new Uri("https://stmikhailccaaptest.blob.core.windows.net/assets/out.wav"));

    var test =  new PlayToAllOptions(new List<PlaySource> { fileSource}) { OperationContext = "hellocontext" };
    var con = id ?? callConnectionId;
    await client.GetCallConnection(con).GetCallMedia().PlayToAllAsync(test);

    return Results.Ok();
});

app.MapPost("/playTo", async (
    [FromQuery] string id,
    ILogger<Program> logger) =>
{
    logger.LogInformation("playTo");

    PlaySource salesAudio = new FileSource(new Uri("https://stmikhailccaaptest.blob.core.windows.net/assets/out.wav"));
    var playOptions = new PlayOptions(salesAudio, new List<CommunicationIdentifier> { CommunicationIdentifier.FromRawId(callerId) }) { OperationContext = "hellocontext" };
    var con = id ?? callConnectionId;
    await client.GetCallConnection(con).GetCallMedia().PlayAsync(playOptions);

    return Results.Ok();
});

app.MapPost("/playToText", async (
    [FromQuery] string id,
    ILogger<Program> logger) =>
{
    logger.LogInformation("playTo");

    PlaySource salesAudio = new TextSource("Update on this, the wrong instance might've been hit when testing. Isura is double checking if my change did cause any issue?") { 
        SourceLocale = "en-Us"
    };
    var playOptions = new PlayOptions(salesAudio, new List<CommunicationIdentifier> { CommunicationIdentifier.FromRawId(callerId) }) { OperationContext = "hellocontext" };
    var con = id ?? callConnectionId;
    await client.GetCallConnection(con).GetCallMedia().PlayAsync(playOptions);

    return Results.Ok();
});

app.MapPost("/sendDtmfTones", async (
    [FromQuery] string id,
    [FromQuery] string part,
    ILogger<Program> logger) =>
{
    logger.LogInformation("sendDtmfTones");

    var tones = new List<DtmfTone> { DtmfTone.Five};

    /*var sendDtmfTonesOptions = new SendDtmfTonesOptions(tones, CommunicationIdentifier.FromRawId("8:acs:6aeaee8c-8285-46d0-b018-493a75a1faca_00000026-2ffa-482f-80f5-8b3a0d009c07"))
    {
        OperationContext = "dtmftones",
    };*/

    var sendDtmfTonesOptions = new SendDtmfTonesOptions(tones, CommunicationIdentifier.FromRawId(part))
    {
        OperationContext = "dtmftones",
    };

    await client.GetCallConnection(callConnectionId).GetCallMedia().SendDtmfTonesAsync(sendDtmfTonesOptions);

    return Results.Ok();
});

app.MapPost("/recognizeDTMF", async (
    [FromQuery] string id,
    ILogger<Program> logger) =>
{
    logger.LogInformation("recognizee");

    var callRecognizeOption = GetMediaRecognizeDTMFOptions("testDtmf", targetPstnNumber);
    await client.GetCallConnection(callConnectionId).GetCallMedia().StartRecognizingAsync(callRecognizeOption);

    return Results.Ok();
});

app.MapPost("/recognizeChoice", async (
    [FromQuery] string id,
    ILogger<Program> logger) =>
{
    logger.LogInformation("recognizee");

    var recognizeOptions = GetMediaRecognizeChoiceOptions(MainMenu, targetPstnNumber);
    await client.GetCallConnection(callConnectionId).GetCallMedia().StartRecognizingAsync(recognizeOptions);

    return Results.Ok();
});

app.MapPost("/recognizeSpeech", async (
    [FromQuery] string id,
    ILogger<Program> logger) =>
{
    logger.LogInformation("recognizee");

    var recognizeOptions = GetMediaRecognizeSpeechOptions(dtmfPrompt, targetPstnNumber);
    await client.GetCallConnection(callConnectionId).GetCallMedia().StartRecognizingAsync(recognizeOptions);

    return Results.Ok();
});

app.MapPost("/addparticipant", async (
    [FromQuery] string id,
    [FromQuery] string target,
    ILogger<Program> logger) =>
{
    logger.LogInformation("recognizee");

    //var recognizeOptions = GetMediaRecognizeSpeechOptions(dtmfPrompt, targetPstnNumber);
    client.GetCallConnection(callConnectionId).AddParticipant(new CallInvite(new CommunicationUserIdentifier(target)));

    return Results.Ok();
});

app.MapPost("/recognizeSpeechOrDtmf", async (
    [FromQuery] string id,
    ILogger<Program> logger) =>
{
    logger.LogInformation("recognizee");

    var recognizeOptions = GetMediaRecognizeSpeechOrDtmfOptions(dtmfPrompt, targetPstnNumber);
    await client.GetCallConnection(callConnectionId).GetCallMedia().StartRecognizingAsync(recognizeOptions);

    return Results.Ok();
});

app.MapPost("/hold", async (
    [FromQuery] string id,
    ILogger<Program> logger) =>
{
    logger.LogInformation("hold");

    // Play message
    var playSource = new TextSource(helpIVRPrompt)
    {
        VoiceName = "en-US-NancyNeural"
    };

    PlaySource holdPlaySource = new FileSource(new Uri("https://stmikhailccaaptest.blob.core.windows.net/assets/out.wav"));
    var holdOptions = new HoldOptions(CommunicationIdentifier.FromRawId(callerId))
    {
        OperationContext = "holdPstnParticipant",
        PlaySource = holdPlaySource,
    };


    var con = id ?? callConnectionId;
    await client.GetCallConnection(con).GetCallMedia().HoldAsync(holdOptions);

    return Results.Ok();
});

app.MapPost("/unhold", async (
    [FromQuery] string id,
    ILogger<Program> logger) =>
{
    logger.LogInformation("unhold");

    // Play message
    var playSource = new TextSource(helpIVRPrompt)
    {
        VoiceName = "en-US-NancyNeural"
    };

    PlaySource holdPlaySource = new FileSource(new Uri("https://stmikhailccaaptest.blob.core.windows.net/assets/out.wav"));
    var unholdOptions = new UnholdOptions(CommunicationIdentifier.FromRawId(callerId))
    {
        OperationContext = "unholdPstnParticipant",
        
    };

    var con = id ?? callConnectionId;
    await client.GetCallConnection(con).GetCallMedia().UnholdAsync(unholdOptions);

    return Results.Ok();
});

app.MapPost("/stopMediaStreaming", async (
    [FromQuery] string id,
    [FromQuery] string operationcontext,
    ILogger<Program> logger) =>
{
    logger.LogInformation("play");

    StopMediaStreamingOptions startMediaStreamingOptions = new StopMediaStreamingOptions()
    {
        OperationContext = operationcontext,
    };
    // Play message
    var playSource = new TextSource(helpIVRPrompt)
    {
        VoiceName = "en-US-NancyNeural"
    };

    var playOptions = new PlayToAllOptions(playSource) { OperationContext = "hellocontext" };
    var con = id ?? callConnectionId;
    await client.GetCallConnection(con).GetCallMedia().StopMediaStreamingAsync(startMediaStreamingOptions);

    return Results.Ok();
});

app.MapPost("/startMediaStreaming", async (
    [FromQuery] string id,
    [FromQuery] string operationcontext,
    ILogger<Program> logger) =>
{
    StartMediaStreamingOptions startMediaStreamingOptions = new StartMediaStreamingOptions( )
    {
        OperationContext = operationcontext,
    };

    var con = id ?? callConnectionId;
    await client.GetCallConnection(con).GetCallMedia().StartMediaStreamingAsync(startMediaStreamingOptions);

    return Results.Ok();
});

app.MapPost("/startTranscription", async (
    [FromQuery] string id,
    ILogger<Program> logger) =>
{

    var con = id ?? callConnectionId;
    await client.GetCallConnection(con).GetCallMedia().StartTranscriptionAsync();

    return Results.Ok();
});

app.MapPost("/stopTranscription", async (
    [FromQuery] string id,
    ILogger<Program> logger) =>
{

    var con = id ?? callConnectionId;
    await client.GetCallConnection(con).GetCallMedia().StopTranscriptionAsync();

    return Results.Ok();
});

app.MapPost("/updateTranscription", async (
    [FromQuery] string id,
    ILogger<Program> logger) =>
{
    var update = "";// new UpdateTranscriptionOptions("fr-CA");
    var con = id ?? callConnectionId;
    await client.GetCallConnection(con).GetCallMedia().UpdateTranscriptionAsync(update);

    return Results.Ok();
});

app.MapPost("/startRecording", async (
    [FromQuery] string id,
    ILogger<Program> logger) =>
{
        var con = id ?? callConnectionId;
        var _serverCallId =  client.GetCallConnection(con).GetCallConnectionProperties().Value.ServerCallId;
        StartRecordingOptions recordingOptions = new StartRecordingOptions(new ServerCallLocator(_serverCallId));
        var callRecording = client.GetCallRecording();
        var response = await callRecording.StartAsync(recordingOptions).ConfigureAwait(false);
        var _recordingId = response.Value.RecordingId;

    logger.LogInformation($"********************* CALL RecordingId : {_recordingId}");
    return Results.Ok($"RecordingId: {_recordingId}");    
});


app.MapDelete("/stopRecording", async (
     [FromQuery] string recordingId,
    ILogger<Program> logger) =>
{
        var response = await client.GetCallRecording().StopAsync(recordingId).ConfigureAwait(false);
    logger.LogInformation($"StopRecordingAsync response -- > {response}");

        return Results.Ok();
    
});

app.MapGet("/getRecordingState", async (
     [FromQuery] string recordingId,
    ILogger<Program> logger) =>
{

    var response = await client.GetCallRecording().GetStateAsync(recordingId).ConfigureAwait(false);
    logger.LogInformation($"GetRecordingStateAsync response -- > {response}");

    return Results.Ok($"{response.Value.RecordingState}");

});

app.MapGet("/getDownloadRecording", async (
     [FromQuery] string recordingId,
    ILogger<Program> logger) =>
{
    var callRecording = client.GetCallRecording();
    callRecording.DownloadTo(new Uri(_contentLocation), "Recording_File.wav");
    return Results.Ok();

}); 

app.MapPost("/recordingFileStatus", async ([FromBody] EventGridEvent[] eventGridEvents, ILogger<Program> logger) =>
{
    foreach (var eventGridEvent in eventGridEvents)
    {
        if (eventGridEvent.TryGetSystemEventData(out object eventData))
        {
            // Handle the webhook subscription validation event.
            if (eventData is Azure.Messaging.EventGrid.SystemEvents.SubscriptionValidationEventData subscriptionValidationEventData)
            {
                var responseData = new Azure.Messaging.EventGrid.SystemEvents.SubscriptionValidationResponse
                {
                    ValidationResponse = subscriptionValidationEventData.ValidationCode
                };
                return Results.Ok(responseData);
            }

            if (eventData is Azure.Messaging.EventGrid.SystemEvents.AcsRecordingFileStatusUpdatedEventData statusUpdated)
            {
                _contentLocation = statusUpdated.RecordingStorageInfo.RecordingChunks[0].ContentLocation;
               // _deleteLocation = statusUpdated.RecordingStorageInfo.RecordingChunks[0].DeleteLocation;
            }
        }
    }

    logger.LogInformation($"Recording Download Location : {_contentLocation}, Recording Delete Location: ");


    return Results.Ok($"Recording Download Location : {_contentLocation}, Recording Delete Location: ");
}).Produces(StatusCodes.Status200OK);

app.MapPost("/api/callbacks", async (CloudEvent[] cloudEvents, ILogger<Program> logger) =>
{
    foreach (var cloudEvent in cloudEvents)
    {
       // PhoneNumberIdentifier target = new PhoneNumberIdentifier(targetPhonenumber);
        CallAutomationEventBase parsedEvent = CallAutomationEventParser.Parse(cloudEvent);
        logger.LogInformation(
                    "Received call event: {type}, callConnectionID: {connId}, serverCallId: {serverId}, operationContext: {operationContext}",
                    parsedEvent.GetType(),
                    parsedEvent.CallConnectionId,
                    parsedEvent.ServerCallId,
                    parsedEvent?.OperationContext);

        var callConnection = client.GetCallConnection(parsedEvent.CallConnectionId);
        var callMedia = callConnection.GetCallMedia();
        logger.LogInformation($"CALL CONNECTION ID ----> {parsedEvent.CallConnectionId}");
        logger.LogInformation($"CORRELATION ID ----> {parsedEvent.CorrelationId}");
       /// var connectionProperties = await callConnection.GetCallConnectionPropertiesAsync();
        //logger.LogInformation($"ANSWERED FOR ----> {connectionProperties.Value.AnsweredFor}");



        if (parsedEvent is CallConnected callConnected)
        {
            #region Transfer Call
            //PhoneNumberIdentifier caller = new PhoneNumberIdentifier("+18772119545");
            //var transferOption = new TransferToParticipantOptions(target);
            ////transferOption.Transferee = caller;
            //transferOption.OperationContext = "transferCallContext";
            //transferOption.SourceCallerIdNumber = caller;

            //// Sending event to a non-default endpoint.
            //transferOption.OperationCallbackUri = new Uri(callbackUriHost);
            //TransferCallToParticipantResult result = await callConnection.TransferCallToParticipantAsync(transferOption);
            //logger.LogInformation($"Call Transfered successfully");
            //logger.LogInformation($"Media Streaming Subscription state ----> {connectionProperties.Value.MediaStreamingSubscription.State}");
            #endregion

            

            #region Transcription
            //StartTranscriptionOptions tOptions = new StartTranscriptionOptions()
            //{
            //    OperationContext = "startMediaStreamingContext",
            //    //Locale = "en-US",
            //};
            //await callMedia.StartTranscriptionAsync(tOptions);
            //logger.LogInformation("Start Transcription.....");
            //connectionProperties = await callConnection.GetCallConnectionPropertiesAsync();
            //logger.LogInformation($"Transcription Subscription state ----> {connectionProperties.Value.TranscriptionSubscription.State}");
            //await Task.Delay(5000);

            ////Stop Transcription
            //StopTranscriptionOptions stopOptions = new StopTranscriptionOptions()
            //{
            //    OperationContext = "stopTranscription"
            //};

            //await callMedia.StopTranscriptionAsync(stopOptions);
            //logger.LogInformation("Stop Transcription.....");
            //connectionProperties = await callConnection.GetCallConnectionPropertiesAsync();
            //logger.LogInformation($"Transcription Subscription state ----> {connectionProperties.Value.TranscriptionSubscription.State}");

            ////Start Transcription
            //await callMedia.StartTranscriptionAsync(options);
            //logger.LogInformation("Start Transcription.....");
            //connectionProperties = await callConnection.GetCallConnectionPropertiesAsync();
            //logger.LogInformation($"Transcription Subscription state ----> {connectionProperties.Value.TranscriptionSubscription.State}");
            //await Task.Delay(5000);
            #endregion

            logger.LogInformation("Fetching recognize options...");

            // prepare recognize tones Choice
            //var recognizeOptions = GetMediaRecognizeChoiceOptions(MainMenu, targetPhonenumber);

            //// prepare recognize tones DTMF
            //var recognizeOptions = GetMediaRecognizeDTMFOptions(dtmfPrompt, targetPhonenumber);

            //// prepare recognize tones Speech
            //var recognizeOptions = GetMediaRecognizeSpeechOptions(dtmfPrompt, targetPhonenumber);

            //// prepare recognize tones Speech or dtmf
            //var recognizeOptions = GetMediaRecognizeSpeechOrDtmfOptions(dtmfPrompt, targetPhonenumber);

            logger.LogInformation("Recognizing options...");

            // Send request to recognize tones
            //await callMedia.StartRecognizingAsync(recognizeOptions);
        }
        else if (parsedEvent is RecognizeCompleted recognizeCompleted)
        {
            var choiceResult = recognizeCompleted.RecognizeResult as ChoiceResult;
            var labelDetected = choiceResult?.Label;
            var phraseDetected = choiceResult?.RecognizedPhrase;

            var speech = recognizeCompleted.RecognizeResult as SpeechResult;
            
            // If choice is detected by phrase, choiceResult.RecognizedPhrase will have the phrase detected, 
            // If choice is detected using dtmf tone, phrase will be null 
            logger.LogInformation("Recognize completed succesfully, labelDetected={labelDetected}, phraseDetected={phraseDetected}", labelDetected, phraseDetected);
            //var textToPlay = labelDetected.Equals(ConfirmChoiceLabel, StringComparison.OrdinalIgnoreCase) ? ConfirmedText : CancelText;
            var textToPlay = "Recognized tone";
            //await HandlePlayAsync(callMedia, textToPlay);
            #region Hold and Unhold
            ////Hold
            //var holdPlaySource = new TextSource("You are in hold... please wait") { VoiceName = SpeechToTextVoice };
            //var holdOptions = new HoldOptions(target)
            //{
            //    OperationCallbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks"),
            //    OperationContext = "holdPstnParticipant",
            //    PlaySourceInfo = holdPlaySource,
            //};
            ////hold participant with options and music
            //var holdParticipant = await callMedia.HoldAsync(holdOptions);

            //////hold participant without options and with music
            ////var holdParticipant = await callMedia.HoldAsync(target, holdPlaySource);

            //////hold participant without options and music
            ////var holdParticipant = await callMedia.HoldAsync(target);

            ////without async
            //////hold participant with options and music
            ////var holdParticipant = callMedia.Hold(holdOptions);

            //////hold participant without options and with music
            ////var holdParticipant = callMedia.Hold(target, holdPlaySource);

            //////hold participant without options and music
            ////var holdParticipant = callMedia.Hold(target);
            //var isParticipantHold = (await callConnection.GetParticipantAsync(target)).Value.IsOnHold;
            //logger.LogInformation($"Is participant on hold ----> {isParticipantHold}");

            //await Task.Delay(5000);
            ////Un-Hold
            //var unHoldOptions = new UnholdOptions(target)
            //{
            //    OperationContext = "UnHoldPstnParticipant"
            //};
            ////Un-Hold participant with options
            //var UnHoldParticipant = await callMedia.UnholdAsync(unHoldOptions);

            //////Un-Hold participant without options
            ////var UnHoldParticipant = await callMedia.UnholdAsync(target);

            ////without async
            //////Un-Hold participant with options
            ////var UnHoldParticipant = callMedia.Unhold(unHoldOptions);

            //////Un-Hold participant without options
            ////var UnHoldParticipant = callMedia.Unhold(target);
            //var isParticipantUnHold = (await callConnection.GetParticipantAsync(target)).Value.IsOnHold;
            //logger.LogInformation($"Is participant on hold ----> {isParticipantUnHold}");
            #endregion

            

            #region Transcription
            //StopTranscriptionOptions stopOptions = new StopTranscriptionOptions()
            //{
            //    OperationContext = "stopTranscription"
            //};

            //await callMedia.StopTranscriptionAsync(stopOptions);
            //logger.LogInformation("Stop Transcription.....");
            //connectionProperties = await callConnection.GetCallConnectionPropertiesAsync();
            //logger.LogInformation($"Transcription Subscription state ----> {connectionProperties.Value.TranscriptionSubscription.State}");

            ////Start Transcription
            //StartTranscriptionOptions options = new StartTranscriptionOptions()
            //{
            //    OperationContext = "startMediaStreamingContext",
            //    //Locale = "en-US",
            //};
            //await callMedia.StartTranscriptionAsync(options);
            //logger.LogInformation("Start Transcription.....");
            //connectionProperties = await callConnection.GetCallConnectionPropertiesAsync();
            //logger.LogInformation($"Transcription Subscription state ----> {connectionProperties.Value.TranscriptionSubscription.State}");
            //await Task.Delay(5000);

            ////Stop Transcription
            //await callMedia.StopTranscriptionAsync(stopOptions);
            //logger.LogInformation("Stop Transcription.....");
            //connectionProperties = await callConnection.GetCallConnectionPropertiesAsync();
            //logger.LogInformation($"Transcription Subscription state ----> {connectionProperties.Value.TranscriptionSubscription.State}");
            #endregion
            //await Task.Delay(5000);

            //await callConnection.HangUpAsync(true);
            //await HandlePlayAsync(callMedia, textToPlay);
        }
        else if (parsedEvent is RecognizeFailed { OperationContext: RetryContext } recognizeFailedEvent1)
        {
            logger.LogError("Encountered error during recognize, operationContext={context}", RetryContext);
            //logger.LogError($"Recognize failed with index : {recognizeFailedEvent1.FailedPlaySourceIndex}");
            
        }
        else if (parsedEvent is RecognizeFailed recognizeFailedEvent)
        {
            //logger.LogError($"Recognize failed with index : {recognizeFailedEvent.FailedPlaySourceIndex}");

            var resultInformation = recognizeFailedEvent.ResultInformation;
            logger.LogError("Encountered error during recognize, message={msg}, code={code}, subCode={subCode}",
                resultInformation?.Message,
                resultInformation?.Code,
                resultInformation?.SubCode);

            var reasonCode = recognizeFailedEvent.ReasonCode;
            string replyText = reasonCode switch
            {
                var _ when reasonCode.Equals(MediaEventReasonCode.RecognizePlayPromptFailed) ||
                reasonCode.Equals(MediaEventReasonCode.RecognizeInitialSilenceTimedOut) => CustomerQueryTimeout,
                var _ when reasonCode.Equals(MediaEventReasonCode.RecognizeIncorrectToneDetected) => InvalidAudio,
                _ => CustomerQueryTimeout,
            };

            
        }
        else if ((parsedEvent is AddParticipantSucceeded))
        {
            logger.LogInformation($"AddParticipantSucceeded event triggered.");
        }
        else if ((parsedEvent is AddParticipantFailed))
        {
            logger.LogInformation($"AddParticipantFailed event triggered.");
        }
        else if ((parsedEvent is MediaStreamingStarted))
        {
            logger.LogInformation($"MediaStreamingStarted event triggered.");
        }
        else if ((parsedEvent is MediaStreamingStopped))
        {
            logger.LogInformation($"MediaStreamingStopped event triggered.");
        }
        else if ((parsedEvent is MediaStreamingFailed))
        {
            logger.LogInformation($"MediaStreamingFailed event triggered.");
        }
        else if ((parsedEvent is TranscriptionStarted))
        {

            logger.LogInformation($"TranscriptionStarted event triggered.");
        }
        else if ((parsedEvent is TranscriptionStopped))
        {
            logger.LogInformation($"TranscriptionStopped event triggered.");
        }
        else if ((parsedEvent is TranscriptionFailed))
        {
            logger.LogInformation($"TranscriptionFailed event triggered.");
        }
        else if (parsedEvent is PlayCompleted)
        {
            logger.LogInformation($"Terminating call.");
            await callConnection.HangUpAsync(true);
        }
        else if (parsedEvent is PlayFailed playFailed)
        {
            //logger.LogInformation($"playFailed with the index : {playFailed.FailedPlaySourceIndex}");
            //await callConnection.HangUpAsync(true);
        }
        else if (parsedEvent is PlayStarted)
        {
            logger.LogInformation($"PlayStarted event triggered.");

            //if (isPlayInterrupt)
            //{
            //    var playTo = new List<CommunicationIdentifier> { target };
            //    var interrupt = new TextSource("Interrupt prompt message")
            //    {
            //        VoiceName = "en-US-NancyNeural"
            //    };
            //    PlayOptions interruptPlayOptions = new PlayOptions(interrupt, playTo)
            //    {
            //        OperationCallbackUri = new Uri(new Uri(callbackUriHost), "/api/callbacks"),
            //        Loop = false,
            //    };
            //    await callMedia.PlayAsync(interruptPlayOptions);
            //}
        }
        else if (parsedEvent is CallTransferAccepted)
        {
            logger.LogInformation($"CallTransferAccepted event triggered.");
        }
        else if (parsedEvent is CallTransferFailed)
        {
            logger.LogInformation($"CallTransferFailed event triggered.");
        }
    }
    return Results.Ok();
}).Produces(StatusCodes.Status200OK);


// api to handle call back events
app.MapPost("/api/callbacks/{contextId}", async (
    [FromBody] CloudEvent[] cloudEvents,
    [FromRoute] string contextId,
    [Required] string callerId,
    ILogger<Program> logger) =>
{

    foreach (var cloudEvent in cloudEvents)
    {
        //CallAutomationEventBase @event = CallAutomationEventParser.Parse(cloudEvent);
        

        CallAutomationEventBase parsedEvent = CallAutomationEventParser.Parse(cloudEvent);
        logger.LogInformation($"{parsedEvent.GetType()} Event received: {JsonConvert.SerializeObject(parsedEvent, Formatting.Indented)}");

        /*if ((parsedEvent is CallConnected))
        {
            logger.LogInformation($"{parsedEvent.GetType()} Event received: {JsonConvert.SerializeObject(parsedEvent, Formatting.Indented)}");
            logger.LogInformation($"MediaStreamingStatus: {((MediaStreamingStarted)parsedEvent).MediaStreamingUpdate.MediaStreamingStatus}");
            logger.LogInformation($"MediaStreamingStatusDetails: {((MediaStreamingStarted)parsedEvent).MediaStreamingUpdate.MediaStreamingStatusDetails}");
        }*/
       /* if ((parsedEvent is MediaStreamingStarted))
        {
            logger.LogInformation($"{parsedEvent.GetType()} Event received: {JsonConvert.SerializeObject(parsedEvent, Formatting.Indented)}");
            logger.LogInformation($"MediaStreamingStatus: {((MediaStreamingStarted)parsedEvent).MediaStreamingUpdate.MediaStreamingStatus}");
            logger.LogInformation($"MediaStreamingStatusDetails: {((MediaStreamingStarted)parsedEvent).MediaStreamingUpdate.MediaStreamingStatusDetails}");
        }
        else if ((parsedEvent is MediaStreamingStopped))
        {
            logger.LogInformation($"{parsedEvent.GetType()} Event received: {JsonConvert.SerializeObject(parsedEvent, Formatting.Indented)}");
            logger.LogInformation($"MediaStreamingStatus: {((MediaStreamingStopped)parsedEvent).MediaStreamingUpdate.MediaStreamingStatus}");
            logger.LogInformation($"MediaStreamingStatusDetails: {((MediaStreamingStopped)parsedEvent).MediaStreamingUpdate.MediaStreamingStatusDetails}");
        }*/
    }

    return Results.Ok();
});

CallMediaRecognizeDtmfOptions GetMediaRecognizeDTMFOptions(string content, string targetParticipant, string context = "")
{
    //var playSource = new TextSource(content) { VoiceName = SpeechToTextVoice };
    #region Recognize prompt list
    ////Multiple TextSource prompt
    //var playSources = new List<PlaySource>() { new TextSource("recognize prompt one") { VoiceName = SpeechToTextVoice }, new TextSource("recognize prompt two") { VoiceName = SpeechToTextVoice }, new TextSource(content) { VoiceName = SpeechToTextVoice } };

    ////Multiple FileSource Prompts
    //var playSources = new List<PlaySource>() { new FileSource(new Uri("https://www2.cs.uic.edu/~i101/SoundFiles/StarWars3.wav")), new FileSource(new Uri("https://www2.cs.uic.edu/~i101/SoundFiles/preamble10.wav")) };

    ////Multiple TextSource and file source prompt
    //var playSources = new List<PlaySource>() { new TextSource("recognize prompt one") { VoiceName = SpeechToTextVoice }, new FileSource(new Uri("https://www2.cs.uic.edu/~i101/SoundFiles/StarWars3.wav")), new TextSource(content) { VoiceName = SpeechToTextVoice } };

    ////Empty play sources
    //var playSources = new List<PlaySource>() { };

    //Invalid Prompt
    var playSources = new List<PlaySource>() { new FileSource(new Uri("https://www2.cs.uic.edu/~i101/SoundFiles/StarWars3.wav")), new FileSource(new Uri("https://dummy/dummy.wav")) };

    #endregion

    var recognizeOptions =
                new CallMediaRecognizeDtmfOptions(
                    targetParticipant: new PhoneNumberIdentifier(targetParticipant), maxTonesToCollect: 8)
                {
                    InterruptPrompt = false,
                    InterToneTimeout = TimeSpan.FromSeconds(5),
                    OperationContext = context,
                    InitialSilenceTimeout = TimeSpan.FromSeconds(15),
                    //PlayPrompts = playSources,
                };
    return recognizeOptions;
}

CallMediaRecognizeChoiceOptions GetMediaRecognizeChoiceOptions(string content, string targetParticipant, string context = "")
{
    //var playSource = new TextSource(content) { VoiceName = SpeechToTextVoice };
    //var playSource = new FileSource(new Uri("https://www2.cs.uic.edu/~i101/SoundFiles/StarWars3.wav"));
    var ssmlToPlay = "<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\"><voice name=\"en-US-JennyNeural\">Say confirm</voice></speak>";

    var playSource = new SsmlSource(ssmlToPlay);
    #region Recognize prompt list
    ////Multiple TextSource prompt
    //var playSources = new List<PlaySource>() { new TextSource("recognize prompt one") { VoiceName = SpeechToTextVoice }, new TextSource("recognize prompt two") { VoiceName = SpeechToTextVoice }, new TextSource(content) { VoiceName = SpeechToTextVoice } };

    ////Multiple FileSource Prompts
    //var playSources = new List<PlaySource>() { new FileSource(new Uri("https://www2.cs.uic.edu/~i101/SoundFiles/StarWars3.wav")), new FileSource(new Uri("https://www2.cs.uic.edu/~i101/SoundFiles/preamble10.wav")) };

    ////Multiple TextSource prompt
    //var playSources = new List<PlaySource>() { new TextSource("recognize prompt one") { VoiceName = SpeechToTextVoice }, new FileSource(new Uri("https://www2.cs.uic.edu/~i101/SoundFiles/StarWars3.wav")), new TextSource(content) { VoiceName = SpeechToTextVoice } };

    ////Empty play sources
    //var playSources = new List<PlaySource>() { };

    //Invalid Prompt
    var playSources = new List<PlaySource>() { new FileSource(new Uri("https://www2.cs.uic.edu/~i101/SoundFiles/StarWars3.wav")), new FileSource(new Uri("https://dummy/dummy.wav")) };

    //Multiple
    //var playSources = new List<PlaySource>() {
    //    new TextSource("Play Media prompt 1") { VoiceName = SpeechToTextVoice },
    //    new TextSource("Play Media prompt 2") { VoiceName = SpeechToTextVoice },
    //    new TextSource("Play Media prompt 3") { VoiceName = SpeechToTextVoice },
    //    new TextSource("Play Media prompt 4") { VoiceName = SpeechToTextVoice },
    //    new TextSource("Play Media prompt 5") { VoiceName = SpeechToTextVoice },
    //    new TextSource("Play Media prompt 6") { VoiceName = SpeechToTextVoice },
    //    new TextSource("Play Media prompt 7") { VoiceName = SpeechToTextVoice },
    //    new TextSource("Play Media prompt 8") { VoiceName = SpeechToTextVoice },
    //    new TextSource("Play Media prompt 9") { VoiceName = SpeechToTextVoice },
    //    new TextSource(content) { VoiceName = SpeechToTextVoice }};

    #endregion
    var recognizeOptions =
        new CallMediaRecognizeChoiceOptions(targetParticipant: new PhoneNumberIdentifier(targetParticipant), GetChoices())
        {
            InterruptCallMediaOperation = false,
            InterruptPrompt = false,
            InitialSilenceTimeout = TimeSpan.FromSeconds(10),
            Prompt = playSource,
            //PlayPrompts = playSources,
            OperationContext = context,
            OperationCallbackUri = new Uri("https://9276-20-185-152-66.ngrok-free.app")
        };

    return recognizeOptions;
}

CallMediaRecognizeSpeechOptions GetMediaRecognizeSpeechOptions(string content, string targetParticipant, string context = "")
{
    var playSource = new TextSource(content) { VoiceName = SpeechToTextVoice };
    #region Recognize prompt list
    ////Multiple TextSource prompt
    //var playSources = new List<PlaySource>() { new TextSource("recognize prompt one") { VoiceName = SpeechToTextVoice }, new TextSource("recognize prompt two") { VoiceName = SpeechToTextVoice }, new TextSource(content) { VoiceName = SpeechToTextVoice } };

    ////Multiple FileSource Prompts
    //var playSources = new List<PlaySource>() { new FileSource(new Uri("https://www2.cs.uic.edu/~i101/SoundFiles/StarWars3.wav")), new FileSource(new Uri("https://www2.cs.uic.edu/~i101/SoundFiles/preamble10.wav")) };

    ////Multiple TextSource and file source prompt
    //var playSources = new List<PlaySource>() { new TextSource("recognize prompt one") { VoiceName = SpeechToTextVoice }, new FileSource(new Uri("https://www2.cs.uic.edu/~i101/SoundFiles/StarWars3.wav")), new TextSource(content) { VoiceName = SpeechToTextVoice } };

    ////Empty play sources
    //var playSources = new List<PlaySource>() { };

    //Invalid Prompt
    var playSources = new List<PlaySource>() { new FileSource(new Uri("https://www2.cs.uic.edu/~i101/SoundFiles/StarWars3.wav")), new FileSource(new Uri("https://dummy/dummy.wav")) };

    #endregion

    var recognizeOptions =
                new CallMediaRecognizeSpeechOptions(
                    targetParticipant: new PhoneNumberIdentifier(targetParticipant))
                {
                    InterruptPrompt = false,
                    OperationContext = context,
                    InitialSilenceTimeout = TimeSpan.FromSeconds(15),
                    //PlayPrompts = playSources,
                    EndSilenceTimeout = TimeSpan.FromSeconds(15)
                };
    return recognizeOptions;
}

CallMediaRecognizeSpeechOrDtmfOptions GetMediaRecognizeSpeechOrDtmfOptions(string content, string targetParticipant, string context = "")
{
    var playSource = new TextSource(content) { VoiceName = SpeechToTextVoice };
    #region Recognize prompt list
    ////Multiple TextSource prompt
    //var playSources = new List<PlaySource>() { new TextSource("recognize prompt one") { VoiceName = SpeechToTextVoice }, new TextSource("recognize prompt two") { VoiceName = SpeechToTextVoice }, new TextSource(content) { VoiceName = SpeechToTextVoice } };

    ////Multiple FileSource Prompts
    //var playSources = new List<PlaySource>() { new FileSource(new Uri("https://www2.cs.uic.edu/~i101/SoundFiles/StarWars3.wav")), new FileSource(new Uri("https://www2.cs.uic.edu/~i101/SoundFiles/preamble10.wav")) };

    ////Multiple TextSource and file source prompt
    //var playSources = new List<PlaySource>() { new TextSource("recognize prompt one") { VoiceName = SpeechToTextVoice }, new FileSource(new Uri("https://www2.cs.uic.edu/~i101/SoundFiles/StarWars3.wav")), new TextSource(content) { VoiceName = SpeechToTextVoice } };

    ////Empty play sources
    //var playSources = new List<PlaySource>() { };

    //Invalid Prompt
    var playSources = new List<PlaySource>() { new FileSource(new Uri("https://www2.cs.uic.edu/~i101/SoundFiles/StarWars3.wav")), new FileSource(new Uri("https://dummy/dummy.wav")) };

    #endregion

    var recognizeOptions =
                new CallMediaRecognizeSpeechOrDtmfOptions(
                    targetParticipant: new PhoneNumberIdentifier(targetParticipant), maxTonesToCollect: 8)
                {
                    InterruptPrompt = false,
                    OperationContext = context,
                    InitialSilenceTimeout = TimeSpan.FromSeconds(15),
                   // PlayPrompts = playSources,
                    EndSilenceTimeout = TimeSpan.FromSeconds(5)
                };
    return recognizeOptions;
}

List<RecognitionChoice> GetChoices()
{
    return new List<RecognitionChoice> {
            new RecognitionChoice("Confirm", new List<string> {
                "Confirm",
                "First",
                "One"
            }) {
                Tone = DtmfTone.One
            },
            new RecognitionChoice("Cancel", new List<string> {
                "Cancel",
                "Second",
                "Two"
            }) {
                Tone = DtmfTone.Two
            }
        };
}

PlayToAllOptions PlayToAllOptions()
{
    //var playSource = new FileSource(new Uri("https://www2.cs.uic.edu/~i101/SoundFiles/StarWars3.wav"));
    var ssmlToPlay = "<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\"><voice name=\"en-US-JennyNeural\">SSML prompt from play media.</voice></speak>";

    var ssmlSource = new SsmlSource(ssmlToPlay);

    // Play message
    var textSource = new TextSource(helpIVRPrompt)
    {
        VoiceName = "en-US-NancyNeural"
    };

    PlaySource fileSource = new FileSource(new Uri("https://stmikhailccaaptest.blob.core.windows.net/assets/out.wav"));

    return new PlayToAllOptions(new List<PlaySource> { ssmlSource, fileSource, textSource }) { OperationContext = "hellocontext" };
}

// First, ensure app.UseAuthentication() comes BEFORE app.UseWebSockets() and the WebSocket middleware
// Move these lines before the WebSocket middleware:
//app.UseAuthentication();
//app.UseAuthorization();




// 3. Use authentication & authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.UseWebSockets();

// 4. WebSocket token validation manually in middleware
/*app.Use(async (context, next) =>
{
    if (context.Request.Path != "/ws")
    {
        await next(context);
        return;
    }

    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("WebSocket connection expected.");
        return;
    }

    var result = await context.AuthenticateAsync();
    if (!result.Succeeded)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsync("Unauthorized WebSocket connection.");
        return;
    }

    context.User = result.Principal;

    // Optional: Log headers
    var correlationId = context.Request.Headers["x-ms-call-correlation-id"].FirstOrDefault();
    var callConnectionId = context.Request.Headers["x-ms-call-connection-id"].FirstOrDefault();

    Console.WriteLine($"Authenticated WebSocket - Correlation ID: {correlationId ?? "not provided"}");
    Console.WriteLine($"Authenticated WebSocket - CallConnection ID: {callConnectionId ?? "not provided"}");

    // Now you can safely accept the WebSocket and process the connection
    // var webSocket = await context.WebSockets.AcceptWebSocketAsync();
    // var mediaService = new AcsMediaStreamingHandler(webSocket, builder.Configuration);
    // await mediaService.ProcessWebSocketAsync();
});*/

// Improved WebSocket middleware with cleaner authorization
/*app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            try
            {
                // Look for token in multiple places: Authorization header or query string
                string token = null;

                // 1. Check Authorization header
                string authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    token = authHeader.Substring("Bearer ".Length).Trim();
                }

                if (string.IsNullOrEmpty(token))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Authorization token is required. Provide it via 'Authorization: Bearer TOKEN' header or '?access_token=TOKEN' query parameter");
                    return;
                }

                try
                {
                    // Validate the token
                    var handler = new JwtSecurityTokenHandler();
                    var validationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                       // ValidIssuer = configuration.Issuer,
                        //ValidAudience = AllowedAudience,
                        //IssuerSigningKeys = configuration.SigningKeys
                    };

                    // Set the authenticated principal on the context
                    context.User = handler.ValidateToken(token, validationParameters, out var validatedToken);

                    // Extract important headers for ACS Call Automation
                    var correlationId = context.Request.Headers["x-ms-call-correlation-id"].FirstOrDefault();
                    var callConnectionId = context.Request.Headers["x-ms-call-connection-id"].FirstOrDefault();

                    // Log the extracted values
                    Console.WriteLine($"WebSocket connection authenticated - Correlation ID: {correlationId ?? "not provided"}");
                    Console.WriteLine($"WebSocket connection authenticated - CallConnection ID: {callConnectionId ?? "not provided"}");

                    // Accept the WebSocket connection
                    //var webSocket = await context.WebSockets.AcceptWebSocketAsync();

                    // Create the media service handler
                    //var mediaService = new AcsMediaStreamingHandler(webSocket, builder.Configuration);

                    // Process the WebSocket asynchronously - this keeps the connection open
                    //await mediaService.ProcessWebSocketAsync();
                }
                catch (SecurityTokenException ex)
                {
                    Console.WriteLine($"Token validation failed: {ex.Message}");
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync($"Invalid token: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebSocket error: {ex.Message}");
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsync("An error occurred while processing the WebSocket request");
            }
        }
        else
        {
            // Not a WebSocket request
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("WebSocket connection expected");
        }
    }
    else
    {
        // Not a WebSocket endpoint, continue down the pipeline
        await next(context);
    }
});


app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            try
            {
                // Extract correlation ID and call connection ID
                var correlationId = context.Request.Headers["x-ms-call-correlation-id"].FirstOrDefault();
                var callConnectionId = context.Request.Headers["x-ms-call-connection-id"].FirstOrDefault();

                // Log the extracted values
                Console.WriteLine($"****************************** Correlation ID: {correlationId}");
                Console.WriteLine($"****************************** Call Connection ID: {callConnectionId}");

                var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                var mediaService = new AcsMediaStreamingHandler(webSocket, builder.Configuration);

                // Set the single WebSocket connection
                await mediaService.ProcessWebSocketAsync();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception received {ex}");
            }
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
    
});*/
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            try
            {
                // Print current time and URL
                var currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var requestUrl = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}";
                
                Console.WriteLine($"[{currentTime}] Request URL: {requestUrl}");
                
                //Task.Delay(5000).Wait(); // Simulate some delay for debugging   
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                
                var mediaService = new AcsMediaStreamingHandler(webSocket, builder.Configuration);

                // Set the single WebSocket connection
                await mediaService.ProcessWebSocketAsync();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception received {ex}");
            }
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

app.MapPost("/api/calls/log", (
    [FromBody] CloudEvent[] cloudEvents,
    ILogger<Program> logger) =>
{
    foreach (var cloudEvent in cloudEvents)
    {
        logger.LogInformation($"Event {cloudEvent.Type}: {cloudEvent.Data}");
    }
    return Results.Ok();
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Logger.LogInformation("The app started");

app.Run();