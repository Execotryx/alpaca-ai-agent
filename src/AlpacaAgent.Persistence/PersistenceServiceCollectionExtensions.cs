using AlpacaAgent.Application.Ports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace AlpacaAgent.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddAlpacaPostgresPersistence(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));
        services.AddDbContext<WorkflowDbContext>(options => options.UseNpgsql(connectionString));
        services.AddSingleton<IDurableWorkflowKernel, PostgresWorkflowKernel>();
        services.AddSingleton<IDurableOutboxDispatcher, PostgresOutboxDispatcher>();
        return services;
    }
}
