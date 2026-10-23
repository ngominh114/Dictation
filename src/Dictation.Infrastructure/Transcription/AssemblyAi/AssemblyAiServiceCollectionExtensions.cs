using Dictation.Application.Import;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;

namespace Dictation.Infrastructure.Transcription.AssemblyAi;

public static class AssemblyAiServiceCollectionExtensions
{
    /// <summary>
    /// Registers AssemblyAI as the <see cref="ITranscriptionProvider"/>: options bound from
    /// <c>Transcription:AssemblyAi</c> and a typed <see cref="HttpClient"/> with the standard resilience handler.
    /// Submit (POST) is never auto-retried.
    /// </summary>
    public static IServiceCollection AddAssemblyAiTranscription(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<AssemblyAiOptions>().BindConfiguration(AssemblyAiOptions.SectionName);

        services
            .AddHttpClient<AssemblyAiTranscriptionProvider>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<AssemblyAiOptions>>().Value;
                if (Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseAddress))
                {
                    client.BaseAddress = baseAddress;
                }
            })
            .AddStandardResilienceHandler(options => options.Retry.DisableForUnsafeHttpMethods());

        services.AddTransient<ITranscriptionProvider>(
            provider => provider.GetRequiredService<AssemblyAiTranscriptionProvider>());

        return services;
    }
}
