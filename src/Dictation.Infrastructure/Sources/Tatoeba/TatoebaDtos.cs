using System.Text.Json.Serialization;

namespace Dictation.Infrastructure.Sources.Tatoeba;

internal sealed class TatoebaSentencesResponseDto
{
    [JsonPropertyName("data")]
    public List<TatoebaSentenceDto>? Data { get; set; }

    [JsonPropertyName("paging")]
    public TatoebaPagingDto? Paging { get; set; }
}

internal sealed class TatoebaSentenceDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("lang")]
    public string? Lang { get; set; }

    [JsonPropertyName("audios")]
    public List<TatoebaAudioDto>? Audios { get; set; }
}

internal sealed class TatoebaAudioDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
}

internal sealed class TatoebaPagingDto
{
    [JsonPropertyName("has_next")]
    public bool? HasNext { get; set; }

    [JsonPropertyName("next")]
    public string? Next { get; set; }
}
