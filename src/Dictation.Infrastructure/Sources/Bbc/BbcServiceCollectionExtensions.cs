using Dictation.Application.Import;
using Microsoft.Extensions.DependencyInjection;

namespace Dictation.Infrastructure.Sources.Bbc;

public static class BbcServiceCollectionExtensions
{
    /// <summary>
    /// Registers the BBC 6 Minute English source: options bound from <c>Sources:Bbc</c>, a typed
    /// <see cref="HttpClient"/> (default handler, follows redirects) with the standard resilience handler, and
    /// the <see cref="IContentSource"/>. The adapter sets its browser-like User-Agent on each request.
    /// </summary>
    public static IServiceCollection AddBbcSource(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<BbcOptions>().BindConfiguration(BbcOptions.SectionName);

        services
            .AddHttpClient<BbcContentSource>()
            .AddStandardResilienceHandler();

        services.AddTransient<IContentSource>(provider => provider.GetRequiredService<BbcContentSource>());

        return services;
    }
}
