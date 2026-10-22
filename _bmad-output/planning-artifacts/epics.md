---
status: final
stepsCompleted: [1, 2, 3, 4]
inputDocuments:
  - _bmad-output/spec-dictation-practice/spec-dictation-practice.md
  - _bmad-output/architecture-dictation-practice/architecture-dictation-practice.md
  - _bmad-output/architecture-dictation-practice/adapter-rules.md
  - _bmad-output/architecture-dictation-practice/web-contract.md
  - _bmad-output/research-audio-sources-and-transcription-integrat/research-audio-sources-and-transcription-integrat.md
  - _bmad-output/research-audio-sources-and-transcription-integrat/digests/sources-bbc.md
  - _bmad-output/research-audio-sources-and-transcription-integrat/digests/sources-tatoeba.md
  - _bmad-output/research-audio-sources-and-transcription-integrat/digests/transcription-apis.md
---

# Dictation Practice - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for Dictation Practice. It decomposes the spec (used in place of a PRD) and the architecture requirements into implementable stories. There is no UX document: UI behaviour is defined in `web-contract.md`.

## Requirements Inventory

### Functional Requirements

- **FR1:** On each app start, a background job imports new content from every registered source provider (BBC 6 Minute English, Tatoeba) and turns it into practice items (lessons) of ordered sentences. *(CAP-1)*
- **FR2:** Each new BBC episode becomes a lesson whose sentences each have text, start ms and end ms, produced by a transcription provider from the episode's static `_download.mp3`. *(CAP-1)*
- **FR3:** Each new Tatoeba lesson groups N sentences (default 10). Each sentence has its own audio clip and stored attribution (author, licence, link). Clips with an empty or disallowed licence are never stored. *(CAP-1)*
- **FR4:** A second import run adds no duplicate lessons and no duplicate Tatoeba sentences. *(CAP-1)*
- **FR5:** Each run imports at most 5 new entries per source (configurable). *(spec constraint)*
- **FR6:** The transcription provider is chosen by configuration: AssemblyAI (default) or Gladia, both behind `ITranscriptionProvider`. Both produce the same normalized sentence list `{index, text, startMs, endMs}`. Switching the config changes the provider for new submissions. *(CAP-2)*
- **FR7:** Each lesson tracks its own status: `Discovered`, `Transcribing`, `Ready` or `Failed`. One lesson failing does not stop the others. *(CAP-3)*
- **FR8:** Temporary errors are retried within the current run. A lesson that still cannot complete is marked `Failed` permanently and never retried automatically. The failure detail is logged, not stored. *(CAP-3, AD-5)*
- **FR9:** Stopping and restarting the app continues interrupted work. A transcription in progress is polled again and never resubmitted. *(CAP-3)*
- **FR10:** The learner can browse `Ready` lessons page by page. The backend returns the requested page and page size with the total count, and the UI moves between pages. *(CAP-4)*
- **FR11:** The learner can open a lesson and play the current sentence, replay it, and go to the next or previous sentence. Each action plays exactly that sentence's audio range. Next and previous stop at the first and last sentence. *(CAP-5)*
- **FR12:** Tatoeba lessons show the attribution of each clip in the player. *(CAP-5)*
- **FR13:** The learner can reveal the full transcript of the current lesson with a button. It is hidden by default and shows every sentence in order. The text matches the played audio. *(CAP-7)*
- **FR15:** When a lesson opens, the player preloads its audio from the source URL. If loading fails, it shows a clear error message with a Retry button. A BBC failure blocks the lesson; a Tatoeba clip failure blocks only that sentence. *(AD-9, web-contract)*

### NonFunctional Requirements

- **NFR1:** The backend is .NET 10 (LTS); the UI is static HTML and vanilla JS served by the API.
- **NFR2:** Personal, single-user, non-commercial use. No auth. Kestrel listens on `localhost` only.
- **NFR3:** Audio is never downloaded, stored, cut, transcoded or re-hosted, on the server or in the browser. Only URLs are persisted, and import never requests an audio URL.
- **NFR4:** The audio sent to transcription is the same URL the player plays (BBC static `_download.mp3`, never the RSS enclosure).
- **NFR5:** Provider API keys are set in appsettings, in a gitignored `appsettings.Local.json` loaded by the API. They never appear in committed files, logs or fixtures.
- **NFR6:** Stay within free tiers: AssemblyAI allows 5 concurrent jobs and 60 polls/min per transcript; Gladia allows 3 concurrent jobs; Tatoeba is throttled to 1 request/s.
- **NFR7:** Each transcribed sentence is one segment, with no splitting or merging. Audio positions are integer milliseconds.
- **NFR8:** Every HTTP error response is `ProblemDetails`, with exception details shown only in Development.
- **NFR9:** All I/O is async with cancellation. Logs are structured, and never contain request headers or keys.
- **NFR10:** Automated tests never call live providers.

### Additional Requirements

**Starter / project setup (affects Epic 1 Story 1):**
- No third-party starter template. Create a solution with `Dictation.Core`, `Dictation.Infrastructure`, `Dictation.Api` (Minimal API host + `wwwroot`) and `Dictation.Tests` (xunit.v3).
- Dependency direction per AD-1, enforced by an architecture test.
- Pinned packages: EF Core SQLite 10.0.13, Http.Resilience 10.11.0, xunit.v3 4.0.2 (confirm it works with `Mvc.Testing` 10, otherwise use 3.2.2), Mvc.Testing 10.0.13.

**Architecture rules that shape stories:**
- **AD-1:** sources and transcription providers plug in by lowercase string key. `ImportService` uses all registered sources.
- **AD-2:** `IContentSource` returns two draft kinds: transcribable (BBC) and ready (Tatoeba). `ITranscriptionProvider` exposes Submit, GetStatus and GetSentences. Adapters throw only `ProviderException(FailureKind, message)`.
- **AD-3:** one segment shape for all sources, with a 0-based index assigned by `Lesson`. `EndMs` is exclusive. Attribution is an owned type.
- **AD-4:** `Status` changes only through `Lesson` methods called by `ImportService`. Segments are written in the same save as `Ready`.
- **AD-5:** failure kinds are Transient, Permanent and Configuration. Retries happen within one run only, then the lesson is `Failed`. Time bounds: 30 min transcription, 30 min pending, 60 min run. Submit is never retried (`DisableForUnsafeHttpMethods`).
- **AD-6:** known keys are passed to sources. Unique indexes on `(Source, ExternalId)` and `(Source, Segment.ExternalKey)`.
- **AD-7:** `MigrateAsync()` runs in `Program.cs` before `app.Run()`. SQLite uses WAL. `ImportBackgroundService` runs only when `Import:Enabled`, with a DI scope per lesson per tick.
- **AD-8:** each lesson stores its provider key. Submissions are capped by `MaxConcurrentJobs` (AssemblyAI 5, Gladia 3).
- **AD-10:** canonical language `en`, mapped to each provider's code inside the adapter.
- **AD-11:** the UI calls only the 3 REST endpoints in `web-contract.md`. Lesson detail carries no text.
- **AD-12:** adapter tests use recorded fixtures. Both transcription adapters pass one shared contract test. API tests run with `Import:Enabled=false` and a temporary SQLite file.

**Integrations:**
- The external APIs are BBC RSS and episode pages, the Tatoeba API, AssemblyAI and Gladia.
- Per-adapter rules are in `adapter-rules.md`, and request/response formats in the research report.

**Operations:**
- Runs locally only (`dotnet run`). The SQLite file and `appsettings.Local.json` (which holds the API keys) are gitignored.

### UX Design Requirements

No UX design document. UI behaviour (pages, player, preload, error messages) is covered by FR10–FR15 and `web-contract.md`.

### FR Coverage Map

- **FR1:** Epic 1 (import framework, Tatoeba source); Epic 2 (BBC source)
- **FR2:** Epic 2 — BBC episodes transcribed into timed sentences
- **FR3:** Epic 1 — Tatoeba groups, clips, attribution, licence filter
- **FR4:** Epic 1 (lesson + Tatoeba sentence dedup); Epic 2 (BBC guid dedup)
- **FR5:** Epic 1 — `Import:MaxPerSource` cap (applies to BBC unchanged)
- **FR6:** Epic 2 — AssemblyAI default (Story 2.2), Gladia alternative switchable by config (Story 2.4)
- **FR7:** Epic 1 (`Ready` path); Epic 2 (`Discovered` → `Transcribing` → `Ready`/`Failed`)
- **FR8:** Epic 2 — retry within run, permanent failure, time bounds
- **FR9:** Epic 2 — resume interrupted work without resubmitting
- **FR10:** Epic 1 — paginated lesson list
- **FR11:** Epic 1 (clips); Epic 2 (time ranges within one MP3, same `playSegment`)
- **FR12:** Epic 1 — attribution in the player
- **FR13:** Epic 1 — transcript endpoint + reveal button
- **FR15:** Epic 1 (preload + per-clip errors); Epic 2 (BBC whole-lesson error)

## Epic List

### Epic 1: Practise dictation with real sentences (Tatoeba)

The learner opens the app, browses a paginated list of practice lessons built from native-speaker Tatoeba sentences, and practises each one sentence by sentence. Play, replay, next and previous are available, along with the clip attribution and a button that reveals the transcript. Audio is preloaded, with clear errors. This epic also lays down the solution skeleton, the import framework, storage and the REST API that Epic 2 builds on. It needs no API key.

**FRs covered:** FR1, FR3, FR4, FR5, FR7, FR10, FR11, FR12, FR13, FR15

### Epic 2: Practise with BBC 6 Minute English episodes

The learner also gets real BBC conversations as lessons. Each episode is transcribed into timed sentences and played from the original MP3 with the same player. Import is reliable: each lesson tracks its status, errors are retried within the run, failed lessons stay failed, interrupted work resumes on restart, and free-tier limits are respected. The transcription provider can be switched between AssemblyAI and Gladia by configuration.

**FRs covered:** FR1, FR2, FR4, FR6, FR7, FR8, FR9, FR11, FR15

## Epic 1: Practise dictation with real sentences (Tatoeba)

The learner browses Tatoeba-based lessons and practises them sentence by sentence. No API key is needed.

### Story 1.1: Runnable app skeleton with lesson storage

As a developer,
I want the solution, data model and database set up,
So that every later story builds on the agreed structure.

**Acceptance Criteria:**

**Given** a fresh clone
**When** I run `dotnet run --project src/Dictation.Api`
**Then** the API starts on localhost, applies EF migrations to a SQLite file before serving, and serves `wwwroot/index.html`
**And** the solution has `Dictation.Core`, `Dictation.Infrastructure`, `Dictation.Api` and `Dictation.Tests`, with the package versions from the architecture spine

**Given** the `Lesson` and `Segment` model (AD-3, AD-4)
**When** the migration runs
**Then** lessons and segments tables exist with unique indexes on `(Source, ExternalId)` and `(Source, Segment.ExternalKey)`

**Given** the test project
**When** `dotnet test` runs
**Then** an architecture test confirms the dependency direction (AD-1), and xunit.v3 works with `Mvc.Testing` (fall back to 3.2.2 if not)

**And** API keys are read from a gitignored `appsettings.Local.json`, and unhandled errors return `ProblemDetails`

### Story 1.2: Import Tatoeba lessons on startup

As a learner,
I want practice lessons built from native-speaker sentences,
So that I have something to practise without any setup.

**Acceptance Criteria:**

**Given** the app starts with `Import:Enabled=true`
**When** the import runs
**Then** up to `Import:MaxPerSource` (5) new Tatoeba lessons of `GroupSize` (10) sentences are saved as `Ready`, each sentence with its clip URL and attribution (rules in `adapter-rules.md`)

**Given** clips with an empty or disallowed licence
**When** the import runs
**Then** those clips are never stored

**Given** the import already ran
**When** the app restarts
**Then** no duplicate lesson or sentence is created

**And** no audio URL is requested during import, and the adapter is tested against recorded Tatoeba responses (no live calls)

### Story 1.3: Browse and practise lessons

As a learner,
I want to pick a lesson from a list, practise it sentence by sentence, and reveal the transcript,
So that I can practise dictation one sentence at a time and check what I heard.

**Acceptance Criteria:**

**Browse**

**Given** `Ready` lessons exist
**When** I call `GET /api/lessons?page=1&pageSize=20`
**Then** I get `{ items, page, pageSize, totalCount }`, newest first (see `web-contract.md`)

**Given** an invalid `page` or `pageSize`
**When** I call the endpoint
**Then** I get 400 `ProblemDetails`

**Given** I open `index.html`
**When** the page loads
**Then** I see the lesson list with next and previous page buttons, and clicking a lesson opens `practice.html?id=…`

**Practise**

**Given** I open a lesson
**When** `practice.html` loads `GET /api/lessons/{id}` (segments without text)
**Then** the audio is preloaded from its source URL and the controls stay disabled until it can play

**Given** the audio is ready
**When** I press play, replay, next or previous
**Then** exactly the current sentence plays: from `startMs` to `endMs`, or to the end of the clip when there is no `endMs`
**And** next and previous stop at the first and last sentence, and the UI shows "sentence N of M"

**Given** a Tatoeba sentence
**When** it is shown
**Then** its attribution (author, licence, link) is displayed

**Given** audio fails to load or takes longer than 20 s
**When** the player detects it
**Then** a clear error message with a Retry button is shown (messages in `web-contract.md`); a failed clip blocks only its own sentence

**And** an unknown or non-`Ready` lesson id returns 404 `ProblemDetails`

**Reveal transcript**

**Given** I am on a lesson
**When** the page loads
**Then** the transcript is hidden and no sentence text has been fetched

**When** I press "Show transcript"
**Then** the page calls `GET /api/lessons/{id}/transcript` and shows every sentence in order

## Epic 2: Practise with BBC 6 Minute English episodes

The learner also gets real BBC conversations, transcribed by AssemblyAI into timed sentences and played with the same player.

### Story 2.1: Discover BBC episodes

As a learner,
I want new BBC 6 Minute English episodes picked up automatically,
So that I get real conversation practice.

**Acceptance Criteria:**

**Given** the BBC feed URL in config
**When** the import runs
**Then** up to 5 new episodes are saved as `Discovered` lessons with the static `_download.mp3` URL scraped from each episode page (never the RSS enclosure; rules in `adapter-rules.md`)

**Given** an episode page with no download link
**When** it is discovered
**Then** the lesson is saved as `Failed` and is not discovered again

**Given** an episode already imported
**When** the import runs again
**Then** it is skipped

**And** the adapter is tested against recorded RSS and HTML fixtures, and never requests the MP3

### Story 2.2: Transcribe BBC episodes with AssemblyAI

As a learner,
I want each BBC episode split into timed sentences,
So that I can practise it sentence by sentence.

**Acceptance Criteria:**

**Given** a `Discovered` lesson and an AssemblyAI key in `appsettings.Local.json`
**When** the import runs
**Then** the MP3 URL is submitted to AssemblyAI and the lesson becomes `Transcribing`, with the job id stored
**And** at most 5 lessons are `Transcribing` at once

**Given** a `Transcribing` lesson
**When** a poll (every 10 s) returns completed
**Then** the sentences are saved as segments (one per sentence, ms times, 0-based index) in the same save that marks the lesson `Ready`

**Given** a `Ready` BBC lesson
**When** I practise it in the existing player
**Then** each sentence plays only its time range within the single MP3, and an audio load failure blocks the whole lesson with the error message

**And** the adapter is tested against recorded AssemblyAI responses with the shared transcription contract test

### Story 2.3: Reliable import: failures and restart

As a learner,
I want problems with one episode not to break the app or waste transcription credit,
So that the lessons that can be imported always are.

**Acceptance Criteria:**

**Given** a temporary error (timeout, 429, 5xx)
**When** it happens during a run
**Then** it is retried within that run only; the submit call itself is never auto-retried

**Given** a permanent error (provider error, empty transcript, 400), or a lesson still unfinished after the time limits (30 min transcription, 30 min pending, 60 min run)
**When** the run handles it
**Then** the lesson is marked `Failed`, the detail is logged, other lessons continue, and the lesson is never retried automatically

**Given** a missing or invalid AssemblyAI key
**When** the run starts submitting
**Then** submission stops with one clear error log, and Tatoeba import still works

**Given** the app was stopped while a lesson was `Transcribing`
**When** it restarts
**Then** the stored job is polled again and never resubmitted

### Story 2.4: Switch transcription to Gladia by configuration

As a learner,
I want the app to work the same whichever transcription provider is configured,
So that the provider can be changed without touching the rest of the app.

**Acceptance Criteria:**

**Given** `Transcription:Provider = gladia` and a Gladia key in `appsettings.Local.json`
**When** the import runs
**Then** new BBC lessons are submitted to Gladia and become `Ready` with the same segment shape as AssemblyAI (sentences built from word timings, seconds converted to ms; rules in `adapter-rules.md`)
**And** at most 3 lessons are `Transcribing` with Gladia at once

**Given** lessons already `Transcribing` with AssemblyAI
**When** the config is switched to Gladia and the app restarts
**Then** those lessons are still polled with AssemblyAI (each lesson keeps its stored provider)

**Given** an unknown value in `Transcription:Provider`
**When** the app starts
**Then** it is a configuration error with one clear log, and Tatoeba import still works

**And** the Gladia adapter passes the same shared transcription contract test as AssemblyAI, using recorded Gladia responses, and adding it required no change to `Core`, storage, API or UI
