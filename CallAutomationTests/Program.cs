using Azure.Messaging;
using Azure.Messaging.EventGrid;
using CallAutomation.Scenarios;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddTransient<IncomingCallHandler>();
builder.Services.AddTransient<RecordingHandler>();

var app = builder.Build();

ILogger<Program> logger = app.Services.GetRequiredService<ILogger<Program>>();
Logger.SetLoggerInstance(logger);

app.MapGet("/api/status", () =>
{
    Console.WriteLine($"API is running...");
    return Results.Ok("Call Automation API Server is running!!");
}).AddEndpointFilterFactory(EndpointFilter.RequestLogger);


app.MapPost("/api/incomingCall", (
    [FromBody] EventGridEvent[] events,
    IncomingCallHandler handler) =>
{
    return handler.HandleIncomingCall(events);
}).Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status500InternalServerError);


app.MapPost("/api/calls/{contextId}", (
    [FromBody] CloudEvent[] events,
    [FromRoute] string contextId,
    [FromQuery] string callerId,
    IncomingCallHandler handler
) =>
{
    return handler.HandleCallback(events, callerId);

}).AddEndpointFilterFactory(EndpointFilter.RequestLogger)
.Produces(StatusCodes.Status200OK);

app.MapPost("api/recording", ([FromBody] EventGridEvent[] eventGridEvents, RecordingHandler handler) =>
{
    return handler.HandleRecording(eventGridEvents);
}).AddEndpointFilterFactory(EndpointFilter.RequestLogger);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
