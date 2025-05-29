using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Swashbuckle.AspNetCore.Annotations;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Get ACS Connection String from appsettings.json
var acsConnectionString = builder.Configuration.GetValue<string>("AcsConnectionString");
ArgumentNullException.ThrowIfNullOrEmpty(acsConnectionString);
string websocketUri = "test";
string callConnectionId = "test";
var targetPstnNumber = builder.Configuration.GetValue<string>("MyPSTNNumber");
var acsPhoneNumber = builder.Configuration.GetValue<string>("AcsPhoneNumber");
var cognitiveServiceEndpoint = builder.Configuration.GetValue<string>("AcsCogService");

//Call Automation Client
var pmaEndpoint = new Uri("https://uswc-01.sdf.pma.teams.microsoft.com:6448");

//Call Automation Client
var client = new CallAutomationClient(pmaEndpoint, acsConnectionString);
var app = builder.Build();
var appBaseUrl = Environment.GetEnvironmentVariable("VS_TUNNEL_URL")?.TrimEnd('/');

if (string.IsNullOrEmpty(appBaseUrl))
{
    appBaseUrl = builder.Configuration.GetValue<string>("DevTunnelUri")?.TrimEnd('/');;
    Console.WriteLine($"appBaseUrl :{appBaseUrl}");
}

app.MapPost("/createPSTNCall",
    [SwaggerOperation(Summary = "Creates a PSTN call with optional media streaming settings.")]
async (ILogger<Program> logger) =>
    {

        PhoneNumberIdentifier target = new PhoneNumberIdentifier(targetPstnNumber);
        PhoneNumberIdentifier caller = new PhoneNumberIdentifier(acsPhoneNumber);
        var callbackUri = new Uri(new Uri(appBaseUrl), "/api/callbacks");
        CallInvite callInvite = new CallInvite(target, caller);

        websocketUri = appBaseUrl.Replace("https", "wss") + "/ws";

        /************** MediaStreamingOptions **********************/
        var mediaStreamingOptions = new MediaStreamingOptions(
                 new Uri(websocketUri),
                 MediaStreamingAudioChannel.Unmixed)
        {
            EnableBidirectional = true,
            AudioFormat = AudioFormat.Pcm24KMono,
            StartMediaStreaming = true,
            MediaStreamingContent = MediaStreamingContent.Audio,
            EnableDtmfTones = true,
        };

        var defaultTrans = new TranscriptionOptions(
                    new Uri(websocketUri),
                   "en-US")
        {
            StartTranscription = true

        };

        /************** CallIntelligenceOptions **********************/
        var callIntelligent = new CallIntelligenceOptions
        {
            CognitiveServicesEndpoint = new Uri(cognitiveServiceEndpoint)
        };

        var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
        {
            MediaStreamingOptions = mediaStreamingOptions,
            // TranscriptionOptions = defaultTrans,
            //CallIntelligenceOptions = callIntelligent
        };

        CreateCallResult createCallResult = await client.CreateCallAsync(createCallOptions);

        callConnectionId = createCallResult.CallConnectionProperties.CallConnectionId;

        logger.LogInformation($"============ Correlation id: {createCallResult.CallConnectionProperties.CorrelationId}");

        logger.LogInformation($"************ CallConnection id: {createCallResult.CallConnectionProperties.CallConnectionId}");
    })
.WithName("CreatePSTNCall")
.WithMetadata(new SwaggerOperationAttribute { Summary = "Creates a PSTN call with media streaming settings." });

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
    }
    return Results.Ok();
}).Produces(StatusCodes.Status200OK);

app.UseWebSockets();

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            try
            {
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        options.RoutePrefix = string.Empty; // Set Swagger UI as the root page
    });
}

app.MapControllers();

app.Run();