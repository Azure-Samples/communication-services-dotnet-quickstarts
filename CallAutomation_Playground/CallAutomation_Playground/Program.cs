using Azure.Communication.CallAutomation;
using CallAutomation.Playground;
using CallAutomation.Playground.Choices;
using CallAutomation.Playground.Interfaces;
using CallAutomation.Playground.Services;
using CallAutomation.Playground.Tones;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
CallAutomationClient callAutomationClient = new CallAutomationClient(builder.Configuration["ACS:ConnectionString"]);
CallAutomationEventProcessor eventProcessor = callAutomationClient.GetEventProcessor();

// Application configuration
var playgroundConfig = new PlaygroundConfig();
playgroundConfig.CallbackUri ??= new Uri(builder.Configuration["VS_TUNNEL_URL"]);
builder.Configuration.Bind(PlaygroundConfig.Name, playgroundConfig);
builder.Services.AddSingleton(playgroundConfig);

builder.Services.AddSingleton(callAutomationClient);
builder.Services.AddSingleton(eventProcessor);

builder.Services.AddIvrMenu("MainMenu", x =>
{
    x.WithConfiguration(y => builder.Configuration.Bind(IvrConfiguration.Name, y))
        .AddChoice<One, PressOneChoice>()
        .AddChoice<Two, PressTwoChoice>()
        .AddChoice<Three, PressThreeChoice>()
        .AddChoice<Four, PressFourChoice>()
        .Build();
});

builder.Services.AddSingleton<ITopLevelMenuService, TopLevelMenuService>();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
