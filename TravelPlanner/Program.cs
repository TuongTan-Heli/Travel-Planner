using DotNetEnv;
using TravelPlanner;
using TravelPlanner.Features.Chat.Services;
using TravelPlanner.Features.Map;
using TravelPlanner.Features.Weather.Services.WeatherService;

var builder = WebApplication.CreateBuilder(args);

// Load .env from the application root so Environment.GetEnvironmentVariable works
var envPath = Path.Combine(builder.Environment.ContentRootPath, ".env");
if (File.Exists(envPath)) Env.Load(envPath);

builder.WebHost.UseUrls("http://localhost:5223");

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddHttpClient<ChatService>();
builder.Services.AddHttpClient<WeatherService>();
builder.Services.AddHttpClient<MapService>();
builder.Services.AddSingleton<ScoringService>();
builder.Services.AddSingleton<ChatWebSocketService>();
builder.Services.AddSingleton<IntentExtractionService>();
builder.Services.AddSingleton<TravelPlanningService>();
builder.Services.AddSingleton<WebSocketNotifier>();
builder.Services.AddHttpClient<CurrencyExchangeService>();
builder.Services.AddSingleton<SetupItineraryService>();
builder.Services.AddSingleton<PresentationService>();
builder.Services.AddSingleton<Planner>();

builder.Services.AddHttpClient<Utils>();
builder.Services.AddMemoryCache();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseCors("AllowLocalhost");
app.UseWebSockets();
app.UseMiddleware<ErrorMiddleware>();

app.Map("/ws/chat", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    var webSocket = await context.WebSockets.AcceptWebSocketAsync();
    var chatService = context.RequestServices.GetRequiredService<ChatWebSocketService>();
    await chatService.HandleAsync(webSocket);
});

app.UseAuthorization();
app.MapControllers();

app.Run();
