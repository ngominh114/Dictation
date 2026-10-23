using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Dictation.Infrastructure.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    /// <summary>Registers MediatR with the lesson query handlers.</summary>
    public static IServiceCollection AddLessonQueries(this IServiceCollection services) =>
        services.AddMediatR(config => config.RegisterServicesFromAssemblyContaining<LessonQueryHandlers>());
}
