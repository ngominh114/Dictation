# Adapter rules — Dictation Practice

Companion to `architecture-dictation-practice.md`. Per-adapter duties that implement AD-2 and AD-7. API formats live in the research report.

## All adapters

- A source adapter parses what the source sends, whatever the format, into its own `internal` DTOs (`{Source}Dtos.cs`), then maps them to a `Discovered` lesson in a private `ToLesson` via `Lesson.CreateDiscovered` and `AddSegment`. No model sits between the DTOs and `Lesson`. Transcription adapters map their own `internal` DTOs to `TranscribedSentence`.
- Throw `ImportException(message)` for problems the provider signals or an unexpected status code. Other exceptions may propagate; any failure fails the lesson or skips the source (AD-5).
- Never send a request to an audio URL (no `GET`, `HEAD` or stream). Never call a provider upload endpoint.
- Declare a constant lowercase key (`bbc`, `tatoeba`, `assemblyai`, `gladia`).
- Map the canonical language (`en`) to the provider's code. Never read `Import:Languages`.

## BBC — `BbcContentSource` (key `bbc`)

- **Feed:** `Sources:Bbc:FeedUrl`. `ExternalId` = trimmed RSS `<guid>`, case preserved. Known keys = existing `ExternalId`s. Supported languages: `en`.
- **Episode page:** derive it from `pubDate` with the two URL patterns in the BBC build notes. Fall back to the index page's `ep-` links. If neither resolves, skip the item with a log.
- **Audio URL:** scrape it from the page during discovery. It must match `https://downloads.bbc.co.uk/…_download.mp3`. Never use the RSS enclosure.
  - If the page has no link, the lesson is returned with `SourceAudioUrl = null` and no segments. `FinishStep` fails it and it is stored, so the episode is skipped from then on.
- **Transcript HTML:** not parsed or stored.
- **HTTP client:** browser-like `User-Agent`; follow redirects.

## Tatoeba — `TatoebaContentSource` (key `tatoeba`)

- **Query:** `lang=eng`, `sort=created`, `has_audio=yes`, `include=audios`, `showtrans=none`, `limit=100`, following `paging.next`, optional `Sources:Tatoeba:WordCount`.
- **Clip:** take the first clip in API order (personal use only, so clip licences are not checked). URL = `https://api.tatoeba.org/v1/audios/{audioId}/file`; never `download_url`.
- **Keys:**
  - Segment `ExternalKey` = `{sentenceId}:{audioId}`. Exclude known keys **before** grouping.
  - Each lesson holds `Sources:Tatoeba:GroupSize` sentences (default 10), added as segments with `StartMs = 0`, `EndMs = null`. A trailing partial group is not created.
  - Lesson `ExternalId` = its sentence IDs in order, comma-joined, no spaces.

## AssemblyAI — `AssemblyAiTranscriptionProvider` (key `assemblyai`)

- **Submit:** `POST /v2/transcript` with `{ audio_url, language_code }`.
- **Poll:** `GET /v2/transcript/{id}` every `Transcription:AssemblyAi:PollIntervalSeconds` (default 5) until `completed`, inside `GetSentencesAsync`.
- **Sentences:** `GET /v2/transcript/{id}/sentences`. Times are already integer ms.
- **Auth:** header `authorization: {Transcription:AssemblyAi:ApiKey}`, no `Bearer`.
- **Language:** sends `language_code: "en"`; if the first real call needs `en_us`, change it inside this adapter only.

## Gladia — `GladiaTranscriptionProvider` (key `gladia`)

Placeholder, not registered in DI (AD-6). Every method throws `ImportException("Gladia is a placeholder and not implemented")`. The rules below are reference for a future implementation.

- **Submit:** `POST /v2/pre-recorded` with `{ audio_url, language_config }`.
- **Poll:** `GET /v2/pre-recorded/{jobId}`.
- **Sentences:** flatten `utterances[].words[]` and end a sentence after a word ending in `.`, `?` or `!`. `StartMs = floor(first.start × 1000)`, `EndMs = ceil(last.end × 1000)`.
- **Concurrency:** `MaxConcurrentJobs` default 3.

## Transcription adapters

- A provider `error` status is thrown as `ImportException`; it is never returned as a status.
- Drop sentences whose trimmed text is empty. If none remain, throw `ImportException`.
- Clamp overlaps so `EndMs[i] ≤ StartMs[i+1]`.
