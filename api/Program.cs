using Lib.AspNetCore.ServerSentEvents;

var builder = WebApplication.CreateBuilder(args);
var allowSpecificOrigins = "_allowSpecificOrigins";

// Add services to the container.
builder.Services.AddServerSentEvents();
builder.Services.AddHostedService<ServerSideEventsWorker>();
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: allowSpecificOrigins,
                      policy =>
                      {
                          policy.WithOrigins("http://127.0.0.1:5500");
                      });
});

var app = builder.Build();
app.UseCors(allowSpecificOrigins);
app.MapServerSentEvents("/sse-endpoint");
app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    public override string ToString() => $"{Date:d}: {TemperatureC}C, {Summary}";
}

class ServerSideEventsWorker : BackgroundService
{
    private readonly IServerSentEventsService _serverSentEventsService;
    private readonly string[] _summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    public ServerSideEventsWorker(IServerSentEventsService serverSentEventsService)
    {
        _serverSentEventsService = serverSentEventsService;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var dayCounter = 1;
        while (!cancellationToken.IsCancellationRequested)
        {
            var clients = _serverSentEventsService.GetClients();
            if (clients.Any())
            {
                var forecast = new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(dayCounter)),
                    Random.Shared.Next(-20, 55),
                    _summaries[Random.Shared.Next(_summaries.Length)]
                );
                await _serverSentEventsService.SendEventAsync(
                    new ServerSentEvent
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        Data = new List<string> { forecast.ToString() }
                    },
                    cancellationToken
                );
                dayCounter++;
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }
    }
}