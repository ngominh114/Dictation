---
id: SPEC-dictation-practice
companions:
  - ../architecture-dictation-practice/architecture-dictation-practice.md
  - ../architecture-dictation-practice/adapter-rules.md
  - ../architecture-dictation-practice/web-contract.md
  - ../research-audio-sources-and-transcription-integrat/research-audio-sources-and-transcription-integrat.md
  - ../research-audio-sources-and-transcription-integrat/digests/sources-bbc.md
  - ../research-audio-sources-and-transcription-integrat/digests/sources-tatoeba.md
  - ../research-audio-sources-and-transcription-integrat/digests/transcription-apis.md
sources: []
---

> **Canonical contract.** This SPEC and the files in `companions:` are the complete, preservation-validated contract for what to build, test, and validate. Source documents listed in frontmatter are for traceability — consult them only if you need narrative rationale or prose color this contract intentionally omits.

# Dictation Practice

## Why

 English learners improve listening by writing down what they hear, one sentence at a time. That needs real audio already cut into timed sentences. Public sources supply the audio. BBC 6 Minute English has long episodes with no timings; Tatoeba has short clips, one per sentence. A background pipeline turns both into one practice format. A small web app then lets the learner browse items and play them sentence by sentence. English only for now, built so other languages can be added later.

## Capabilities

- **CAP-1**
  - **intent:** A background job imports new content from every configured source provider (BBC 6 Minute English, Tatoeba) and turns it into practice items of ordered sentences, each with its own audio range.
  - **success:**
    - After a job run, each new BBC episode is an item whose sentences each have text, start ms and end ms.
    - Each new Tatoeba item holds N sentences with their own clip. Clips with an empty or disallowed licence are never stored.
    - A second run adds no duplicates.
- **CAP-2**
  - **intent:** Transcription goes through one `ITranscriptionProvider` interface. AssemblyAI is implemented and registered. A Gladia adapter exists as an unregistered placeholder with its config section, showing where an alternative plugs in.
  - **success:** AssemblyAI's recorded responses map to the normalized sentence list `{index, text, startMs, endMs}`. Swapping the provider needs only a DI registration change and config.
- **CAP-3**
  - **intent:** Ingestion tolerates failure: each item tracks its own status, transient errors are retried, and the job resumes after a restart.
  - **success:**
    - A failing item is marked failed while other items complete. The failure detail is logged, not stored.
    - A failed item is not run again on later app starts; a retry feature is deferred.
    - Stopping the app during a transcription loses only that job; the next start submits the episode again.
- **CAP-4**
  - **intent:** The learner can browse practice items page by page.
  - **success:** The backend returns a requested page and page size with the total count, and the UI moves between pages.
- **CAP-5**
  - **intent:** The learner can practise one item sentence by sentence: play the current sentence, replay it, and go to the next or previous sentence.
  - **success:** Each action plays exactly the current sentence's audio range and no neighbouring audio. Next and Previous stop at the first and last sentence.
- **CAP-6**
  - **intent:** Language is carried as data through sources, transcription and storage, with only English enabled.
  - **success:** Enabling another language requires configuration and, where needed, a new source adapter. No schema change and no edits to existing English code paths.
- **CAP-7**
  - **intent:** The learner can reveal the full transcript of the current item with a button.
  - **success:** The transcript is hidden by default. The button shows every sentence of the item in order, and the text matches the audio that is played: transcribed sentences for BBC, sentence text for Tatoeba.

## Constraints

- Backend in .NET.
- Content only from public APIs:
  - BBC 6 Minute English: the RSS feed, whose URL is in config, plus each episode's page.
  - Tatoeba API.
- Transcription only via AssemblyAI or Gladia. API formats are in the research companion.
- Personal, single-user, non-commercial use only. BBC terms allow personal use; most Tatoeba audio is CC BY-NC-ND. So:
  - no public re-hosting or redistribution of audio;
  - no modifying the audio;
- The BBC audio sent to transcription is the same file that is played: the static `…_download.mp3` from the episode page, never the RSS enclosure. The enclosure has ad insertion and expiring URLs.
- Sentence playback seeks within the original audio file, which both audio hosts allow via HTTP range requests. No server-side audio cutting.
- Provider API keys are read from appsettings, set in the gitignored `appsettings.Local.json`, and never committed.
- Stay within the free tiers:
  - AssemblyAI: 5 concurrent jobs and 60 status polls per minute per transcript.
  - Gladia: 3 concurrent jobs.
- Each transcribed sentence is one practice segment, whatever its length. No splitting or merging.
- Import is demo-sized: at most 5 entries per source per run (5 BBC episodes, 5 Tatoeba items), configurable.
- Scope is small. Every addition must serve the capabilities above.

## Non-goals

- Typing answers and automatic checking against the sentence text.
- User accounts, login, multi-user progress tracking.
- Non-English content in this version.
- Podcast discovery (iTunes) and video-platform sources (Supadata).
- Public hosting, commercial use, or redistribution of audio.
- Mobile app or offline mode.

## Success signal

- From a fresh clone with an AssemblyAI key configured, one ingestion run produces at least one BBC episode item and one Tatoeba item.
- In the UI, the learner pages through the list, opens each item, and uses play, replay, next and previous. Every action plays exactly one sentence.
- The transcript button shows the item's full text.

## Assumptions

- Single user; no login.
- The "simple UI" is a browser web app, because HTML5 audio can seek within a file. The framework is left to the architecture.
- A Tatoeba practice item groups about 10 sentences; the size is configurable.
- Audio is streamed from the provider URLs, not stored locally.
- The BBC episode page's own transcript is not stored. The full transcript comes from the transcribed sentences, so it matches the audio.
- Practice is listen-and-reveal only. The answer named a transcript button rather than typed-answer checking.
- The "5 entries per run" limit applies to each source separately.

## Open Questions

- AssemblyAI `language_code`: `en` or `en_us`? Confirm with the first call.
