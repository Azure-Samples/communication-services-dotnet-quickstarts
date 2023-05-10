using CallAutomation.Scenarios;
using CallAutomation.Scenarios.Handlers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// add auth

builder.Services.AddAuthentication()
    .AddScheme<EventGridAuthOptions, EventGridAuthHandler>(EventGridAuthHandler.EventGridAuthenticationScheme, o =>
    {
        o.Secret = builder.Configuration["EventGridSecret"];
        o.HeaderName = "X-EventGrid-AuthKey";
        o.QueryParameter = "EventGridAuthKey";

    });

builder.Services.AddAuthorization(options => {
    options.AddPolicy(EventGridAuthHandler.EventGridAuthenticationScheme,
        authBuilder =>
        {
            authBuilder.AddAuthenticationSchemes(EventGridAuthHandler.EventGridAuthenticationScheme);
            authBuilder.RequireClaim("EventGridhandler");
        });
});

// add services 
builder.Services.AddAllEventGridEventHandlers();
builder.Services.AddRouterServices();

var app = builder.Build();

ILogger<Program> logger = app.Services.GetRequiredService<ILogger<Program>>();
Logger.SetLoggerInstance(logger);

app.MapGet("/api/status", () =>
{
    Console.WriteLine($"API is running...");
    return Results.Ok("Call Automation API Server is running!!");
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
