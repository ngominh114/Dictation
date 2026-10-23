# Epic 1 Context: Practise dictation with real sentences (Tatoeba)

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

The learner opens a local web app, browses a paginated list of lessons built from native-speaker Tatoeba sentences, and practises each lesson sentence by sentence (play, replay, next, previous), with a button that reveals the transcript. Audio streams straight from Tatoeba with clear load errors. No API key is needed. The epic also lays down the solution skeleton, the import framework, SQLite storage and the REST API. Epic 2 (BBC episodes plus AssemblyAI/Gladia transcription) builds on these without changing them, so the shared shapes must be generic from the start.

## Stories

- Story 1.1: Runnable app skeleton with lesson storage
- Story 1.2: Import Tatoeba lessons on startup
- Story 1.3: Browse and practise lessons

## Requirements & Constraints

- On each app start, a background import pulls new content from every registered source and saves it as lessons of ordered sentences.
- Tatoeba lessons hold `GroupSize` sentences (default 10). Each sentence has its own clip. Clips with an empty or disallowed licence are never stored.
- Each run imports at most `Import:MaxPerSource` (default 5) new lessons per source. A second run creates no duplicate lessons or sentences.
- Audio is never downloaded, stored, cut, transcoded or re-hosted, on the server or in the browser. Only URLs are persisted, and import never requests an audio URL.
- Personal, single-user, non-commercial use. No auth. Kestrel listens on localhost only. Runs with `dotnet run --project src/Dictation.Api`.
- API keys go in a gitignored `appsettings.Local.json` that the API loads. Keys never appear in committed files, logs or fixtures. The SQLite file is gitignored too.
- Every HTTP error is `ProblemDetails`, with exception detail only in Development. All I/O is async and takes a `CancellationToken`. Logs are structured and never contain headers or keys.
- Automated tests never call live providers.
- Language is data: lessons store the canonical `en`. Adding a language must not need a schema change.

## Technical Decisions

- **Projects:** `Dictation.Domain` (`Lesson`, `Segment`, `LessonStatus`); `Dictation.Application` (`ImportService`, `LessonQueryService`, ports, `ProviderException`, options); `Dictation.Infrastructure` (adapters, EF `DbContext`, repository, migrations, `internal` DTOs); `Dictation.Api` (`Program.cs`, Minimal API endpoints, `ImportBackgroundService`, `wwwroot`); `Dictation.Tests` (xunit.v3). Layout: `src/...` and `tests/Dictation.Tests/{Domain,Application,Persistence,Adapters/Fixtures,Api}`.
- **Dependency rule:** Domain references nothing. Application references only Domain and `Microsoft.Extensions.*.Abstractions`. Infrastructure references only Application. Adapters never reference each other or the repository. The project references enforce this.
- **Stack:** .NET 10.0.13, EF Core SQLite 10.0.13, Http.Resilience 10.11.0, xunit.v3 4.0.2 (fall back to 3.2.2 if it fails with Mvc.Testing), Mvc.Testing 10.0.13. No MediatR or CQRS. The UI is static HTML and vanilla JS.
- **Plug-in by key:** sources and providers have constant lowercase string keys (`tatoeba`, `bbc`, `assemblyai`, `gladia`), not enums. `ImportService` uses every registered `IContentSource`.
- **Source port:** `DiscoverAsync(language, knownKeys, max)` returns `LessonDraft`s. A *ready* draft (Tatoeba) carries a `DraftSentence { ExternalKey, Text, AudioUrl }` list and no `SourceAudioUrl`. A *transcribable* draft (BBC, Epic 2) has `SourceAudioUrl` and no sentences. Adapters throw only `ProviderException(FailureKind: Transient | Permanent | Configuration, message)`.
- **Data model:** `Lesson { Guid Id, Source, ExternalId, Title, Language, SourceUrl, SourceAudioUrl?, Status, TranscriptionProvider?, TranscriptionJobId?, TranscriptionStartedAt?, CreatedAt }`. `Segment { Index, ExternalKey?, Text, AudioUrl, StartMs, EndMs? }` is an owned collection. Unique indexes on `(Source, ExternalId)` and `(Source, Segment.ExternalKey)`. A violation skips the draft without failing the run.
- **Segments:** the 0-based `Index` is assigned only by `Lesson`. One sentence is one segment. `EndMs` is exclusive. Tatoeba segments use `StartMs = 0` and `EndMs = null` (play to the end of the clip).
- **Lifecycle:** status values are `Discovered`, `Transcribing`, `Ready` and `Failed`. Status changes only through `Lesson` methods (`CreateReady` in this epic, plus `CreateDiscovered`/`StartTranscription`/`Complete`/`Fail` for Epic 2), called only by `ImportService`, with one save per call. Segments are saved in the same save as `Ready`.
- **Startup:** `MigrateAsync()` runs before `app.Run()`; never use `EnsureCreated`. SQLite uses WAL mode. `ImportBackgroundService` runs only when `Import:Enabled` is true, once per start, with a DI scope per lesson. No exception escapes one lesson's import.
- **Tatoeba adapter rules:**
  - Query `lang=eng`, `has_audio=yes`, `include=audios`, sentences in ascending ID, with optional `Sources:Tatoeba:WordCount`. Throttle to 1 request/s.
  - Bind the JSON field `license`, not `licence`. Keep only clips whose trimmed licence is in `Sources:Tatoeba:AllowedLicenses`, compared case-insensitively. Default list: CC BY 2.0 FR, CC BY 4.0, CC BY-SA 4.0, CC BY-NC 4.0, CC BY-NC-ND 3.0, CC0 1.0.
  - Take the first allowed clip. Its URL is `https://api.tatoeba.org/v1/audios/{audioId}/file`; never use `download_url`.
  - `ExternalKey` = `{sentenceId}:{audioId}`. Exclude known keys before grouping. Drop a trailing partial group. Lesson `ExternalId` = the group's sentence IDs, comma-joined with no spaces. Map `en` to `eng` inside the adapter. The adapter never reads `Import:Languages`.
- **Conventions:** adapters are named `{Provider}{Role}`; ports are `I`-prefixed. Audio positions are integer ms. Times are UTC via `TimeProvider`. JSON is camelCase with nulls omitted. Enums are persisted by name. Options live under `Sources:*`, `Transcription:*` and `Import:*`. HTTP clients are typed with the standard resilience handler.
- **Tests:** adapters run against recorded fixtures in `Fixtures/{provider}/`, with no keys or usernames. API tests use `Import:Enabled=false` and a temporary SQLite file. `ImportService` is tested with stubbed ports.

## UX & Interaction Patterns

- **Endpoints** (the only calls the UI makes; only `Ready` lessons are exposed; an unknown or non-`Ready` id returns 404 `ProblemDetails`):
  - `GET /api/lessons?page&pageSize` returns `{ items:[{id,title,source,language,segmentCount,createdAt}], page, pageSize, totalCount }`. `page` is 1-based (default 1). `pageSize` defaults to 20, range 1–100; out of range returns 400. Order is `createdAt DESC, id DESC`.
  - `GET /api/lessons/{id}` returns `{ id, title, source, language, sourceUrl, segments:[{index,audioUrl,startMs,endMs?}] }` with no text, segments sorted by index.
  - `GET /api/lessons/{id}/transcript` returns `[{ index, text }]`.
- **Pages:** `index.html` shows the paged list with next and previous buttons; clicking a lesson opens `practice.html?id=…`. The transcript is hidden by default and is fetched only when "Show transcript" is pressed.
- **Preload:** create one `<audio preload="auto">` per distinct `audioUrl`, pointing straight at the source. Never use fetch/blob, Cache Storage, IndexedDB, localStorage or a service worker. Controls stay disabled until `canplaythrough` fires.
- **Playback:** one `playSegment(segment)` for play, replay, next and previous. Seek to `startMs` after `loadedmetadata`. Stop through a `requestAnimationFrame` check against `endMs`, or at the end of the file. Never branch on `source`. Show "sentence N of M" (`index + 1`). Next and previous stop at the ends.
- **Load errors:** handle the audio `error` event plus a 20 s `canplaythrough` timeout. Show an error panel with a Retry button that reloads that audio element. Each cause has a fixed message (network, source not supported, decode, timeout); the wording is in the web contract. A failed Tatoeba clip blocks only its own sentence; navigation and the transcript still work.

## Cross-Story Dependencies

- 1.1 (solution, model, migration, ProblemDetails, local config) comes before 1.2 and 1.3.
- 1.2 supplies the `Ready` lessons that 1.3 lists and plays. 1.3's API tests seed data directly, with `Import:Enabled=false`.
- Epic 2 reuses the import framework, the lifecycle methods, the `TranscriptionProvider`/`TranscriptionJobId`/`TranscriptionStartedAt` columns, the per-source cap, the endpoints and `playSegment`. Build them source-agnostic and keep the Epic 2 fields in the first migration.
