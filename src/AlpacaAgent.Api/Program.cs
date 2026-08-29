using AlpacaAgent.Application.Ports;
using AlpacaAgent.Persistence;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("PostgreSql");
if (!string.IsNullOrWhiteSpace(connectionString)) builder.Services.AddAlpacaPostgresPersistence(connectionString);

var app = builder.Build();
app.MapGet("/health", async (IServiceProvider services, CancellationToken cancellationToken) =>
{
    var kernel = services.GetService<IDurableWorkflowKernel>();
    if (kernel is null) return Results.Json(new { status = "unhealthy", reason = "persistence-not-configured", brokerWritesEnabled = false }, statusCode: 503);
    try { return Results.Ok(await kernel.ObserveHealthAsync(cancellationToken)); }
    catch (Exception exception) { return Results.Json(new { status = "unhealthy", reason = exception.GetType().Name, brokerWritesEnabled = false }, statusCode: 503); }
});
app.Run();
