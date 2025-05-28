using Call_Automation_GCCH;
using Call_Automation_GCCH.Models;
using Call_Automation_GCCH.Services;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<CommunicationConfiguration>(builder.Configuration.GetSection(nameof(CommunicationConfiguration)));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICommunicationConfigurationService, CommunicationConfigurationService>();
builder.Services.AddScoped<CallAutomationService>();
builder.Services.AddSingleton<IServerSentEventsService, ServerSentEventsService>();

builder.Logging.ClearProviders();
builder.Logging.AddProvider(new ConsoleCollectorLoggerProvider());

var app = builder.Build();

app.UseStaticFiles(); // Required to serve wwwroot

app.UseSwagger(); // This generates /swagger/v1/swagger.json

app.UseSwaggerUI(c =>
{
    // So the UI is served at /swagger
    c.RoutePrefix = "swagger";


    // Use the custom gcch-swagger.html from wwwroot/swagger-ui
    c.IndexStream = () =>
    {
        var path = Path.Combine(builder.Environment.WebRootPath, "swagger-ui", "gcch-swagger.html");
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