using Microsoft.Extensions.DependencyInjection;

namespace Dictation.Application.Lessons;

public static class LessonsServiceCollectionExtensions
{
    /// <summary>
    /// Registers MediatR with the lesson query handlers. The host registers the
    /// <see cref="Abstractions.ILessonRepository"/>.
    /// </summary>
    public static IServiceCollection AddLessonQueries(this IServiceCollection services) =>
        services.AddMediatR(config => config.RegisterServicesFromAssemblyContaining<GetLessonsQueryHandler>());
}
