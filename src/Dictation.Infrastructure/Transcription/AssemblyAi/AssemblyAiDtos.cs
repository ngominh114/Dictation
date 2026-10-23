using System.Text.Json.Serialization;

namespace Dictation.Infrastructure.Transcription.AssemblyAi;

internal sealed class AssemblyAiSubmitRequestDto
{
    [JsonPropertyName("audio_url")]
    public required string AudioUrl { get; init; }

    [JsonPropertyName("language_code")]
    public required string LanguageCode { get; init; }
}

internal sealed class AssemblyAiTranscriptDto
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

internal sealed class AssemblyAiSentencesResponseDto
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("sentences")]
    public List<AssemblyAiSentenceDto>? Sentences { get; set; }
}

internal sealed class AssemblyAiSentenceDto
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("start")]
    public long Start { get; set; }

    [JsonPropertyName("end")]
    public long End { get; set; }
}
