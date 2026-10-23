using Dictation.Domain.Lessons;
using Dictation.Tests.Support;

namespace Dictation.Tests.Domain;

public sealed class LessonTests
{
    [Fact]
    public void AddSegment_assigns_sequential_zero_based_indexes()
    {
        var lesson = TestLessons.Create();

        lesson.AddSentence("1:10");
        lesson.AddSentence("2:20");
        lesson.AddSentence(null);

        Assert.Equal([0, 1, 2], lesson.Segments.Select(s => s.Index));
    }
}
