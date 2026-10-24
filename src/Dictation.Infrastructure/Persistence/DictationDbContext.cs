using Dictation.Application.Import.Abstractions;
using Dictation.Domain.Lessons;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Dictation.Infrastructure.Persistence;

/// <summary>The SQLite database: the lesson mapping, and the <see cref="ILessonImportRepository"/> the import uses.</summary>
public sealed class DictationDbContext(DbContextOptions<DictationDbContext> options)
    : DbContext(options), ILessonImportRepository
{
    // SQLITE_CONSTRAINT, with its PRIMARYKEY and UNIQUE extended codes.
    private const int SqliteConstraint = 19;
    private const int SqliteConstraintPrimaryKey = 1555;
    private const int SqliteConstraintUnique = 2067;

    private static readonly ValueConverter<DateTime, DateTime> UtcConverter = new(
        v => v.Kind == DateTimeKind.Utc ? v : v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : DateTime.SpecifyKind(v, DateTimeKind.Utc),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    private static readonly ValueConverter<DateTime?, DateTime?> NullableUtcConverter = new(
        v => v.HasValue
            ? (v.Value.Kind == DateTimeKind.Utc ? v.Value : v.Value.Kind == DateTimeKind.Local ? v.Value.ToUniversalTime() : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc))
            : v,
        v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

    public DbSet<Lesson> Lessons => Set<Lesson>();

    public async Task<IReadOnlySet<string>> GetKnownKeysAsync(string source, CancellationToken ct)
    {
        var externalIds = await Lessons
            .AsNoTracking()
            .Where(l => l.Source == source)
            .Select(l => l.ExternalId)
            .ToListAsync(ct);

        var segmentKeys = await Lessons
            .AsNoTracking()
            .Where(l => l.Source == source)
            .SelectMany(l => l.Segments)
            .Where(s => s.ExternalKey != null)
            .Select(s => s.ExternalKey!)
            .ToListAsync(ct);

        return externalIds.Concat(segmentKeys).ToHashSet(StringComparer.Ordinal);
    }

    public Task<bool> ExistsAsync(string source, string externalId, CancellationToken ct) =>
        Lessons.AnyAsync(l => l.Source == source && l.ExternalId == externalId, ct);

    public async Task<bool> TryAddAsync(Lesson lesson, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(lesson);

        Lessons.Add(lesson);
        try
        {
            await SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            ChangeTracker.Clear();
            return false;
        }
        catch
        {
            ChangeTracker.Clear();
            throw;
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var lesson = modelBuilder.Entity<Lesson>();
        lesson.ToTable("Lessons");
        lesson.HasKey(l => l.Id);
        lesson.Property(l => l.Id).ValueGeneratedNever();

        lesson.Property(l => l.Source).IsRequired();
        lesson.Property(l => l.ExternalId).IsRequired();
        lesson.Property(l => l.Title).IsRequired();
        lesson.Property(l => l.Language).IsRequired();
        lesson.Property(l => l.SourceUrl).IsRequired();
        lesson.Property(l => l.SourceAudioUrl);
        lesson.Property(l => l.Status).IsRequired().HasConversion<string>();
        lesson.Property(l => l.TranscriptionProvider);
        lesson.Property(l => l.TranscriptionJobId);
        lesson.Property(l => l.TranscriptionStartedAt).HasConversion(NullableUtcConverter);
        lesson.Property(l => l.CreatedAt).IsRequired().HasConversion(UtcConverter);

        lesson.HasIndex(l => new { l.Source, l.ExternalId }).IsUnique();

        lesson.OwnsMany(l => l.Segments, segment =>
        {
            segment.ToTable("Segments");
            segment.WithOwner().HasForeignKey("LessonId");
            segment.HasKey("LessonId", nameof(Segment.Index));
            segment.Property(s => s.Index).ValueGeneratedNever();

            segment.Property(s => s.Source).IsRequired();
            segment.Property(s => s.ExternalKey);
            segment.Property(s => s.Text).IsRequired();
            segment.Property(s => s.AudioUrl).IsRequired();
            segment.Property(s => s.StartMs).IsRequired();
            segment.Property(s => s.EndMs);

            segment.HasIndex(s => new { s.Source, s.ExternalKey })
                .IsUnique()
                .HasFilter("\"ExternalKey\" IS NOT NULL");
        });

        lesson.Navigation(l => l.Segments).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is SqliteException sqlite
        && sqlite.SqliteErrorCode == SqliteConstraint
        && sqlite.SqliteExtendedErrorCode is SqliteConstraintUnique or SqliteConstraintPrimaryKey;
}
