using Dictation.Application.Import;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Dictation.Infrastructure.Sources.Tatoeba;

public static class TatoebaServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Tatoeba source: options bound from <c>Sources:Tatoeba</c>, a typed
    /// <see cref="HttpClient"/> with the standard resilience handler, and the <see cref="IContentSource"/>.
    /// </summary>
    public static IServiceCollection AddTatoebaSource(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<TatoebaOptions>().BindConfiguration(TatoebaOptions.SectionName);

        services
            .AddHttpClient<TatoebaContentSource>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<TatoebaOptions>>().Value;
                if (Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseAddress))
                {
                    client.BaseAddress = baseAddress;
                }
            })
            .AddStandardResilienceHandler();

        services.AddTransient<IContentSource>(provider => provider.GetRequiredService<TatoebaContentSource>());

        return services;
    }
}
