using AlpacaAgent.Persistence;

var builder = Host.CreateApplicationBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("PostgreSql");
if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddAlpacaPostgresPersistence(connectionString);
    // Phase 3 deliberately registers no scheduler or dispatcher until concrete
    // node handlers are configured and validated.
}
await builder.Build().RunAsync();
