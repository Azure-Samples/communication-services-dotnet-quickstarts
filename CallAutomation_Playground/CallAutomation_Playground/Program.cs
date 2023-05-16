// This is the start of the program.
// Define any dependencies or configs here. 
using Azure.Communication.CallAutomation;
using CallAutomation_Playground;
using CallAutomation_Playground.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// CallAutomation client: Add with given Azure Communication Service's connection string
CallAutomationClient callAutomationClient = new CallAutomationClient(ReadingConfigs(builder, "COMMUNICATION_CONNECTION_STRING"));
builder.Services.AddSingleton(callAutomationClient);

// This is our main Top Level Menu service, which will include our business logic of IVR.
builder.Services.AddSingleton<ITopLevelMenuService, TopLevelMenuService>();
builder.Services.AddSingleton<IOngoingEventHandler, OngoingEventHandler>();

// setting up callback endpoint
// Note: we are using VS tunnel feature for hosting callback webhook
// update this if it were to use other 3rd party tunnel program, such as ngrok.
var callbackUriHost = builder.Configuration["VS_TUNNEL_URL"];

// Get all configs, such as callback url and prompts url
PlaygroundConfigs playgroundConfig = new PlaygroundConfigs
{
    CallbackUri = new Uri(callbackUriHost + "api/event"),
    DirectOfferedPhonenumber = ReadingConfigs(builder, "DIRECT_OFFERED_PHONE_NUMBER"),
    AllPrompts = new PlaygroundConfigs.Prompts
    {
        MainMenu = new Uri(ReadingConfigs(builder, "PROMPT_MAIN_MENU")),
        CollectPhoneNumber = new Uri(ReadingConfigs(builder, "PROMPT_COLLECT_PHONE_NUMBER")),
        Retry = new Uri(ReadingConfigs(builder, "PROMPT_RETRY")),
        AddParticipantSuccess = new Uri(ReadingConfigs(builder, "PROMPT_ADD_PARTICIPANT_SUCCESS")),
        AddParticipantFailure = new Uri(ReadingConfigs(builder, "PROMPT_ADD_PARTICIPANT_FAILURE")),
        TransferFailure = new Uri(ReadingConfigs(builder, "PROMPT_TRANSFER_FAILURE")),
        PlayRecordingStarted = new Uri(ReadingConfigs(builder, "PROMPT_PLAY_RECORDING_STARTED")),
        Goodbye = new Uri(ReadingConfigs(builder, "PROMPT_GOODBYE")),
        Music = new Uri(ReadingConfigs(builder, "PROMPT_MUSIC")),
    }
};
builder.Services.AddSingleton(playgroundConfig);

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

static string ReadingConfigs(WebApplicationBuilder builder, string configKey)
{
    string? returnedValue = builder.Configuration.GetSection("PlaygroundConfigs")[configKey];
    if (returnedValue == null)
    {
        throw new NullReferenceException($"{configKey} is not setup. README has details on how to set these variables.");
    }
    return returnedValue;
}