using Dictation.Infrastructure.ContentSources.Bbc;
using Dictation.Infrastructure.ContentSources.Tatoeba;
using Dictation.Infrastructure.Transcription.AssemblyAi;

namespace Dictation.Api;

public static class LessonResourcesServiceCollectionExtensions
{
    private const string ApiKeySetting = $"{AssemblyAiOptions.SectionName}:ApiKey";

    /// <summary>
    /// Registers the lesson resources: the content sources and the AssemblyAI transcription they rely on. BBC episodes need
    /// transcription, so the BBC source is registered only when the AssemblyAI key is set; otherwise a warning is
    /// logged at startup and no episode is failed for a configuration problem.
    /// </summary>
    public static IServiceCollection AddLessonResources(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAssemblyAiTranscription();
        services.AddTatoebaSource();

        if (!string.IsNullOrWhiteSpace(configuration[ApiKeySetting]))
        {
            services.AddBbcSource();
        }
        else
        {
            services.AddHostedService<TranscriptionNotConfiguredWarning>();
        }

        return services;
    }

    /// <summary>Logs once at startup that the BBC source is off because the AssemblyAI key is missing.</summary>
    private sealed class TranscriptionNotConfiguredWarning(ILogger<TranscriptionNotConfiguredWarning> logger) : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogWarning("BBC source disabled: {Setting} is not set (put it in appsettings.Local.json)", ApiKeySetting);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
