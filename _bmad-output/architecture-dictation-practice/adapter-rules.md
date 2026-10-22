# Adapter rules — Dictation Practice

Companion to `architecture-dictation-practice.md`. Per-adapter duties that implement AD-2, AD-6, AD-9 and AD-10. API formats live in the research report.

## All adapters

- Provider DTOs are `internal` to `Infrastructure` and mapped to `Core` types inside the adapter.
- Throw only `ProviderException(FailureKind, message)`. Convert every `HttpRequestException`, non-caller `TaskCanceledException`, `JsonException`, `TimeoutRejectedException`, `BrokenCircuitException` and unexpected status code (kinds per AD-5).
- Never send a request to an audio URL (no `GET`, `HEAD` or stream). Never call a provider upload endpoint.
- Declare a constant lowercase key (`bbc`, `tatoeba`, `assemblyai`, `gladia`).
- Map the canonical language (`en`) to the provider's code. Never read `Import:Languages`.

## BBC — `BbcContentSource` (key `bbc`)

- **Feed:** `Sources:Bbc:FeedUrl`. `ExternalId` = trimmed RSS `<guid>`, case preserved. Known keys = existing `ExternalId`s. Supported languages: `en`.
- **Episode page:** derive it from `pubDate` with the two URL patterns in the BBC build notes. Fall back to the index page's `ep-` links. If neither resolves, skip the item with a log.
- **Audio URL:** scrape it from the page during discovery. It must match `https://downloads.bbc.co.uk/…_download.mp3`. Never use the RSS enclosure.
  - If the page has no link, return the draft with `SourceAudioUrl = null`. `ImportService` saves it and calls `Fail()`, so the episode is not rediscovered.
- **Transcript HTML:** not parsed or stored.
- **HTTP client:** browser-like `User-Agent`; follow redirects.

## Tatoeba — `TatoebaContentSource` (key `tatoeba`)

- **Query:** `lang=eng`, `has_audio=yes`, `include=audios`, sentences in ascending ID, optional `Sources:Tatoeba:WordCount`. Throttle to 1 request/s.
- **Licence:**
  - Bind the live JSON field `license` (not `licence`).
  - Drop clips whose licence is empty or not in `Sources:Tatoeba:AllowedLicenses`, compared case-insensitively after trimming.
  - Default allowed list: `CC BY 2.0 FR`, `CC BY 4.0`, `CC BY-SA 4.0`, `CC BY-NC 4.0`, `CC BY-NC-ND 3.0`, `CC0 1.0`.
- **Clip:** if several are allowed, take the first in API order. URL = `https://api.tatoeba.org/v1/audios/{audioId}/file`; never `download_url`.
- **Keys:**
  - `DraftSentence.ExternalKey` = `{sentenceId}:{audioId}`. Exclude known keys **before** grouping.
  - Groups of `Sources:Tatoeba:GroupSize` (default 10). A trailing partial group is not created.
  - `ExternalId` = the group's sentence IDs in order, comma-joined, no spaces.
- **Attribution:** `{ Author, License, Url }` from `author`, `license` and `attribution_url`.

## AssemblyAI — `AssemblyAiTranscriptionProvider` (key `assemblyai`)

- **Submit:** `POST /v2/transcript` with `{ audio_url, language_code }`.
- **Poll:** `GET /v2/transcript/{id}`.
- **Sentences:** `GET /v2/transcript/{id}/sentences`. Times are already integer ms.
- **Concurrency:** `MaxConcurrentJobs` default 5.
- **Language:** `en` vs `en_us` is settled by the first real call, inside this adapter only.

## Gladia — `GladiaTranscriptionProvider` (key `gladia`)

- **Submit:** `POST /v2/pre-recorded` with `{ audio_url, language_config }`.
- **Poll:** `GET /v2/pre-recorded/{jobId}`, verified by fixture.
- **Sentences:** flatten `utterances[].words[]` and end a sentence after a word ending in `.`, `?` or `!`. `StartMs = floor(first.start × 1000)`, `EndMs = ceil(last.end × 1000)`.
- **Concurrency:** `MaxConcurrentJobs` default 3.

## Both transcription adapters

- A provider `error` status is thrown as `Permanent`; it is never returned as a status.
- Drop sentences whose trimmed text is empty. If none remain, throw `Permanent`.
- Clamp overlaps so `EndMs[i] ≤ StartMs[i+1]`.
- Both must pass the shared contract test (AD-12).
