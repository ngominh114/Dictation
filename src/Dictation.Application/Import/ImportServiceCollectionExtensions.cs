using Dictation.Application.Import.Configurations;
using Dictation.Application.Pipelines;
using Dictation.Domain.Lessons;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dictation.Application.Import;

public static class ImportServiceCollectionExtensions
{
    /// <summary>
    /// Registers the lesson pipeline and <see cref="ImportLessonsUseCase"/>.
    /// The host binds <see cref="ImportOptions"/> and registers the repository and the sources.
    /// </summary>
    public static IServiceCollection AddImport(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<ImportOptions>();

        services.TryAddScoped<Pipeline<Lesson>, LessonPipeline>();
        services.TryAddScoped<ImportLessonsUseCase>();

        return services;
    }
}
