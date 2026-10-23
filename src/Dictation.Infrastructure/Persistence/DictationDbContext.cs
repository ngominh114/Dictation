using Dictation.Domain.Lessons;
using Microsoft.EntityFrameworkCore;

namespace Dictation.Infrastructure.Persistence;

public sealed class DictationDbContext(DbContextOptions<DictationDbContext> options) : DbContext(options)
{
    public DbSet<Lesson> Lessons => Set<Lesson>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new LessonConfiguration());
    }
}
