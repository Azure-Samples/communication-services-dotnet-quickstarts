using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Call_Automation_GCCH;
using Call_Automation_GCCH.Controllers;
using Call_Automation_GCCH.Models;
using Call_Automation_GCCH.Services;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

var commSection = builder.Configuration.GetSection("CommunicationSettings");
// This reads "CommunicationSettings" from appsettings.json
builder.Services.Configure<ConfigurationRequest>(commSection);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CallAutomationService as a singleton
builder.Services.AddSingleton<CallAutomationService>(sp => {
    string connectionString = commSection["AcsConnectionString"];
    bool isArizona = bool.Parse(commSection["IsArizona"] ?? "true");
    string pmaEndpoint = isArizona ? commSection["PmaEndpointArizona"] : commSection["PmaEndpointTexas"];
    
    if (string.IsNullOrEmpty(pmaEndpoint)) {
        sp.GetRequiredService<ILogger<Program>>().LogWarning($"The {(isArizona ? "PmaEndpointArizona" : "PmaEndpointTexas")} setting is empty");
    }
    
    return new CallAutomationService(connectionString, pmaEndpoint, sp.GetRequiredService<ILogger<CallAutomationService>>());
});

builder.Logging.ClearProviders();
builder.Logging.AddProvider(new ConsoleCollectorLoggerProvider());

var app = builder.Build();

app.UseStaticFiles(); // Required to serve wwwroot

app.UseSwagger(); // This generates /swagger/v1/swagger.json

app.UseSwaggerUI(c =>
{
    // So the UI is served at /swagger
    c.RoutePrefix = "swagger";

    // Use the custom GCCHSwagger.html from wwwroot/swagger-ui
    c.IndexStream = () =>
    {
        var path = Path.Combine(builder.Environment.WebRootPath, "swagger-ui", "GCCHSwagger.html");
        return File.OpenRead(path);
    };
});

// Configure the audio files path
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "audio")),
    RequestPath = "/audio"
});

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
      await Helper.ProcessRequest(webSocket);
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