using Azure.Communication.CallAutomation;
using Call_Automation_GCCH;
using Call_Automation_GCCH.Application.UseCases.Calls;
using Call_Automation_GCCH.Core.Interfaces;
using Call_Automation_GCCH.Infrastructure.Services;
using Call_Automation_GCCH.Logging;
using Call_Automation_GCCH.Models;
using Call_Automation_GCCH.Services;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// CONFIGURATION
// ============================================================================
var commSection = builder.Configuration.GetSection("CommunicationSettings");
builder.Services.Configure<AcsCommunicationSettings>(commSection);

// ============================================================================
// CLEAN ARCHITECTURE - DEPENDENCY INJECTION
// ============================================================================

// Read configuration
string connectionString = commSection["AcsConnectionString"] ?? string.Empty;
string phoneNumber = commSection["AcsPhoneNumber"] ?? string.Empty;
string callbackUri = commSection["CallbackUriHost"] ?? string.Empty;
bool isArizona = bool.Parse(commSection["IsArizona"] ?? "true");
string pmaEndpoint = (isArizona ? commSection["PmaEndpointArizona"] : commSection["PmaEndpointTexas"]) ?? string.Empty;

// Infrastructure Layer - Azure SDK Client
builder.Services.AddSingleton<CallAutomationClient>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<CallAutomationClient>>();

    if (string.IsNullOrEmpty(connectionString))
    {
        logger.LogWarning("AcsConnectionString is not set. Use POST /api/configuration/setConnectionString to configure at runtime.");
        // Return a placeholder - will be replaced when connection string is set
        return new CallAutomationClient("endpoint=https://placeholder.communication.azure.us;accesskey=placeholder");
    }

    // Create client with PMA endpoint for GCCH
    if (!string.IsNullOrEmpty(pmaEndpoint))
    {
        logger.LogInformation("Creating CallAutomationClient with PMA endpoint: {PmaEndpoint}", pmaEndpoint);
        return new CallAutomationClient(pmaEndpoint: new Uri(pmaEndpoint), connectionString: connectionString);
    }
    else
    {
        logger.LogInformation("Creating CallAutomationClient without PMA endpoint");
        return new CallAutomationClient(connectionString);
    }
});

// Infrastructure Layer - Services
builder.Services.AddScoped<ICallService>(sp =>
{
    var client = sp.GetRequiredService<CallAutomationClient>();
    var logger = sp.GetRequiredService<ILogger<AzureCallService>>();
    return new AzureCallService(client, logger, phoneNumber);
});

builder.Services.AddScoped<IMediaService, AzureMediaService>();
builder.Services.AddScoped<IParticipantService, AzureParticipantService>();
builder.Services.AddScoped<IRecordingService, AzureRecordingService>();
builder.Services.AddScoped<IConfigurationService, AzureConfigurationService>();

// Application Layer - Call Use Cases
builder.Services.AddScoped<CreateCallUseCase>(sp =>
{
    var callService = sp.GetRequiredService<ICallService>();
    var logger = sp.GetRequiredService<ILogger<CreateCallUseCase>>();
    var callbackUriValue = new Uri(new Uri(callbackUri), "/api/callbacks").ToString();
    return new CreateCallUseCase(callService, logger, callbackUriValue);
});

builder.Services.AddScoped<CreateGroupCallUseCase>(sp =>
{
    var callService = sp.GetRequiredService<ICallService>();
    var logger = sp.GetRequiredService<ILogger<CreateGroupCallUseCase>>();
    var callbackUriValue = new Uri(new Uri(callbackUri), "/api/callbacks").ToString();
    return new CreateGroupCallUseCase(callService, logger, callbackUriValue);
});

builder.Services.AddScoped<TransferCallUseCase>(sp =>
{
    var callService = sp.GetRequiredService<ICallService>();
    var logger = sp.GetRequiredService<ILogger<TransferCallUseCase>>();
    return new TransferCallUseCase(callService, logger);
});

builder.Services.AddScoped<HangupCallUseCase>(sp =>
{
    var callService = sp.GetRequiredService<ICallService>();
    var logger = sp.GetRequiredService<ILogger<HangupCallUseCase>>();
    return new HangupCallUseCase(callService, logger);
});

// Application Layer - Media Use Cases
builder.Services.AddScoped<Call_Automation_GCCH.Application.UseCases.Media.PlayMediaUseCase>();
builder.Services.AddScoped<Call_Automation_GCCH.Application.UseCases.Media.PlayToAllUseCase>();
builder.Services.AddScoped<Call_Automation_GCCH.Application.UseCases.Media.HoldParticipantUseCase>();
builder.Services.AddScoped<Call_Automation_GCCH.Application.UseCases.Media.UnholdParticipantUseCase>();
builder.Services.AddScoped<Call_Automation_GCCH.Application.UseCases.Media.StartMediaStreamingUseCase>();
builder.Services.AddScoped<Call_Automation_GCCH.Application.UseCases.Media.StopMediaStreamingUseCase>();
builder.Services.AddScoped<Call_Automation_GCCH.Application.UseCases.Media.CancelAllMediaOperationsUseCase>();
builder.Services.AddScoped<Call_Automation_GCCH.Application.UseCases.Media.StartTranscriptionUseCase>();
builder.Services.AddScoped<Call_Automation_GCCH.Application.UseCases.Media.StopTranscriptionUseCase>();
builder.Services.AddScoped<Call_Automation_GCCH.Application.UseCases.Media.UpdateTranscriptionUseCase>();

// Application Layer - Participant Use Cases
builder.Services.AddScoped<Call_Automation_GCCH.Application.UseCases.Participants.AddParticipantUseCase>();
builder.Services.AddScoped<Call_Automation_GCCH.Application.UseCases.Participants.RemoveParticipantUseCase>();
builder.Services.AddScoped<Call_Automation_GCCH.Application.UseCases.Participants.GetParticipantUseCase>();
builder.Services.AddScoped<Call_Automation_GCCH.Application.UseCases.Participants.GetAllParticipantsUseCase>();
builder.Services.AddScoped<Call_Automation_GCCH.Application.UseCases.Participants.MuteParticipantUseCase>();
builder.Services.AddScoped<Call_Automation_GCCH.Application.UseCases.Participants.CancelAddParticipantUseCase>();

// Application Layer - Recording Use Cases
builder.Services.AddScoped<Call_Automation_GCCH.Application.UseCases.Recordings.StartRecordingUseCase>();
builder.Services.AddScoped<Call_Automation_GCCH.Application.UseCases.Recordings.PauseRecordingUseCase>();
builder.Services.AddScoped<Call_Automation_GCCH.Application.UseCases.Recordings.ResumeRecordingUseCase>();
builder.Services.AddScoped<Call_Automation_GCCH.Application.UseCases.Recordings.StopRecordingUseCase>();
builder.Services.AddScoped<Call_Automation_GCCH.Application.UseCases.Recordings.GetRecordingStateUseCase>();
builder.Services.AddScoped<Call_Automation_GCCH.Application.UseCases.Recordings.DeleteRecordingUseCase>();
builder.Services.AddScoped<Call_Automation_GCCH.Application.UseCases.Recordings.DownloadRecordingUseCase>();

// Application Layer - Configuration Use Cases
builder.Services.AddScoped<Call_Automation_GCCH.Application.UseCases.Configuration.GetConfigurationUseCase>();
builder.Services.AddScoped<Call_Automation_GCCH.Application.UseCases.Configuration.UpdateConfigurationUseCase>();


// ============================================================================
// LEGACY SERVICE (for backward compatibility)
// ============================================================================
builder.Services.AddSingleton<ICallAutomationService, CallAutomationService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<CallAutomationService>>();

    if (string.IsNullOrEmpty(connectionString))
    {
        logger.LogWarning("AcsConnectionString is not set. Use POST /api/configuration/setConnectionString to configure at runtime.");
    }

    return new CallAutomationService(connectionString, pmaEndpoint, logger);
});

// ============================================================================
// ASP.NET CORE SERVICES
// ============================================================================
builder.Services.AddControllers()
    .AddApplicationPart(typeof(Call_Automation_GCCH.Presentation.Controllers.CallsController).Assembly);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.OrderActionsBy(apiDesc =>
    {
        var tag = apiDesc.ActionDescriptor.EndpointMetadata
            .OfType<Microsoft.AspNetCore.Http.TagsAttribute>()
            .SelectMany(t => t.Tags)
            .FirstOrDefault() ?? "zzz";
        return tag;
    });

    // Use fully qualified type names for schema IDs to handle duplicate type names across namespaces
    c.CustomSchemaIds(type => type.FullName?.Replace("+", "."));

    // Include XML comments for descriptions and examples in Swagger UI
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

// Add CallAutomationService as a singleton behind ICallAutomationService
// Client is initialized lazily — use the /api/configuration/setConnectionString endpoint
// to provide ACS credentials at runtime from Swagger.
builder.Services.AddSingleton<ICallAutomationService, CallAutomationService>(sp => {
    string connectionString = commSection["AcsConnectionString"] ?? string.Empty;
    bool isArizona = bool.Parse(commSection["IsArizona"] ?? "true");
    string pmaEndpoint = (isArizona ? commSection["PmaEndpointArizona"] : commSection["PmaEndpointTexas"]) ?? string.Empty;

    var logger = sp.GetRequiredService<ILogger<CallAutomationService>>();

    if (string.IsNullOrEmpty(connectionString))
    {
        logger.LogWarning("AcsConnectionString is not set. Use POST /api/configuration/setConnectionString to configure at runtime.");
    }

    return new CallAutomationService(connectionString, pmaEndpoint, logger);
});

builder.Logging.ClearProviders();
builder.Logging.AddProvider(new ConsoleCollectorLoggerProvider());

// Add HTTPS redirection and HSTS for production security
if (builder.Environment.IsProduction())
{
    builder.Services.AddHttpsRedirection(options =>
    {
        options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
        options.HttpsPort = 443;
    });

    builder.Services.AddHsts(options =>
    {
        options.MaxAge = TimeSpan.FromDays(365);
        options.IncludeSubDomains = true;
        options.Preload = false; // Set to true if you want to be included in HSTS preload list
    });
}

var app = builder.Build();

// Apply HTTPS redirection and HSTS in production
if (app.Environment.IsProduction())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

// Serve index.html as default page (must come before UseStaticFiles)
app.UseDefaultFiles(new DefaultFilesOptions
{
    DefaultFileNames = new List<string> { "index.html" }
});

// Serve static files from wwwroot
app.UseStaticFiles();

app.UseSwagger(); // This generates /swagger/v1/swagger.json

app.UseSwaggerUI(c =>
{
    // So the UI is served at /swagger
    c.RoutePrefix = "swagger";

    // Use the custom GCCHSwagger.html from wwwroot/swagger-ui if it exists
    var customSwaggerPath = Path.Combine(builder.Environment.WebRootPath ?? builder.Environment.ContentRootPath, "swagger-ui", "GCCHSwagger.html");
    if (File.Exists(customSwaggerPath))
    {
        c.IndexStream = () => File.OpenRead(customSwaggerPath);
    }
});

// Configure the audio files path
var audioPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "audio");
if (Directory.Exists(audioPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(audioPath),
        RequestPath = "/audio"
    });
}

// Enable WebSocket support
app.UseWebSockets();
app.Use(async (context, next) =>
{
  // Get the logger instance from the DI container
  var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

  if (context.Request.Path == "/ws")
  {
    logger.LogInformation($"Request received. Path: {context.Request.Path}");
    if (context.WebSockets.IsWebSocketRequest)
    {
      logger.LogInformation("WebSocket request received.");
      using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
      await WebSocketStreamingHandler.ProcessRequest(webSocket);
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

// Add custom WebSocket middleware
// app.UseMiddleware<Call_Automation_GCCH.Middleware.WebSocketMiddleware>();

app.MapControllers();

app.Run();