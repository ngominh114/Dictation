using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dictation.Infrastructure.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    public const string DefaultConnectionString = "Data Source=dictation.db";

    /// <summary>Registers <see cref="DictationDbContext"/> on SQLite using <c>ConnectionStrings:Dictation</c>.</summary>
    public static IServiceCollection AddPersistence(this IServiceCollection services) =>
        // Read when the context is created, so configuration added later (e.g. by a test host) is honoured.
        services.AddDbContext<DictationDbContext>((provider, options) =>
            options.UseSqlite(
                provider.GetRequiredService<IConfiguration>().GetConnectionString("Dictation") ?? DefaultConnectionString));
}
