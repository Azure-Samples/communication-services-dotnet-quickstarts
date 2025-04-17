using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Call_Automation_GCCH;
using Call_Automation_GCCH.Models;
using Call_Automation_GCCH.Services;
using Microsoft.Extensions.FileProviders;

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
    string pmaEndpoint = commSection["PmaEndpoint"];
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

// Add custom WebSocket middleware
app.UseMiddleware<Call_Automation_GCCH.Middleware.WebSocketMiddleware>();

app.MapControllers();

app.Run();