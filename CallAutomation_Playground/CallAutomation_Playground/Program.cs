
using Azure.Communication.CallAutomation;
using CallAutomation.Playground;
using CallAutomation.Playground.Interfaces;
using CallAutomation.Playground.Menus;
using CallAutomation.Playground.Services;

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

builder.Services.AddSingleton(new IvrMenuRegistry()
{
    IvrMenus = { { playgroundConfig.MainMenuName, new PlaygroundMainMenu(callAutomationClient, playgroundConfig)} }
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
