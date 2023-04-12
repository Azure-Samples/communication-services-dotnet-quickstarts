
using Azure.Communication.CallAutomation;
using CallAutomation_Playground;
using CallAutomation_Playground.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
CallAutomationClient callAutomationClient = new CallAutomationClient(builder.Configuration["ACS_ConnectionString"]);
CallAutomationEventProcessor eventProcessor = callAutomationClient.GetEventProcessor();

builder.Services.AddSingleton(callAutomationClient);
builder.Services.AddSingleton(eventProcessor);
builder.Services.AddSingleton<ITopLevelMenuService, TopLevelMenuService>();

// get Visual Studio dev tunnel uri
PlaygroundConfig playgroundConfig = new PlaygroundConfig
{
    CallbackUri = new Uri(builder.Configuration["VS_TUNNEL_URL"] + "api/event"),
    ACS_DirectOffer_Phonenumber = builder.Configuration["PlaygroundConfig:ACS_DirectOffer_Phonenumber"],
    InitialPromptUri = new Uri(builder.Configuration["PlaygroundConfig:InitialPromptUri"]),
    AddParticipantPromptUri = new Uri(builder.Configuration["PlaygroundConfig:AddParticipantPromptUri"]),
    HoldMusicPromptUri = new Uri(builder.Configuration["PlaygroundConfig:HoldMusicPromptUri"]),
    TransferParticipantApi = new Uri(builder.Configuration["PlaygroundConfig:TransferParticipantApi"])
};
//builder.Configuration.Bind(playgroundConfig);
builder.Services.AddSingleton(playgroundConfig);

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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
