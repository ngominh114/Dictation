using Dictation.Domain.Lessons;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Dictation.Infrastructure.Persistence;

internal sealed class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    private static readonly ValueConverter<DateTime, DateTime> UtcConverter = new(
        v => v.Kind == DateTimeKind.Utc ? v : v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : DateTime.SpecifyKind(v, DateTimeKind.Utc),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    private static readonly ValueConverter<DateTime?, DateTime?> NullableUtcConverter = new(
        v => v.HasValue
            ? (v.Value.Kind == DateTimeKind.Utc ? v.Value : v.Value.Kind == DateTimeKind.Local ? v.Value.ToUniversalTime() : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc))
            : v,
        v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

    public void Configure(EntityTypeBuilder<Lesson> lesson)
    {
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
}
