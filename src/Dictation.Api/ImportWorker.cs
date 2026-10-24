using Dictation.Application.Import;
using Dictation.Application.Import.Abstractions;
using Dictation.Application.Import.Configurations;
using Microsoft.Extensions.Options;

namespace Dictation.Api;

/// <summary>
/// When <c>Import:Enabled</c>, runs <see cref="ImportLessonsUseCase"/> once per registered source at app start,
/// in parallel, each in its own DI scope.
/// </summary>
public sealed class ImportWorker(
    IEnumerable<IContentSource> sources,
    IServiceScopeFactory scopeFactory,
    IOptions<ImportOptions> options,
    ILogger<ImportWorker> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            logger.LogInformation("Import is disabled");
            return Task.CompletedTask;
        }

        return Task.WhenAll(sources.Select(source => RunAsync(source, stoppingToken)));
    }

    private async Task RunAsync(IContentSource source, CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var useCase = scope.ServiceProvider.GetRequiredService<ImportLessonsUseCase>();
            await useCase.ExecuteAsync(source, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host is stopping.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Import of {Source} failed", source.Key);
        }
    }
}
