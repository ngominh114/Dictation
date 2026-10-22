---
title: 'technical research: API formats of the selected providers'
type: 'technical'
topic: 'Request/response formats of the APIs used by the dictation app'
decision: 'Exact request and response formats for BBC 6 Minute English (RSS + episode page), Tatoeba, AssemblyAI and Gladia'
source: 'native run'
status: complete
preset: 'standard'
validation: 'normal'
created: '2026-10-22'
updated: '2026-10-22'
claims_verified: 13
claims_unverified: 6
---

# API formats of the selected providers

Request and response formats for the APIs the app uses, as observed live or read from the official docs on 2026-10-22. Samples are trimmed to the fields that matter.

| Provider | Role | Protocol | Auth |
|---|---|---|---|
| BBC 6 Minute English | Episode list (RSS) and episode page (audio + transcript) | RSS 2.0 XML + HTML | none |
| Tatoeba | Sentences that already have audio | REST / JSON | none |
| AssemblyAI | Speech-to-text, sentence timestamps (default) | REST / JSON, async | `authorization` header |
| Gladia | Speech-to-text (alternative) | REST / JSON, async | `x-gladia-key` header |

---

## 1. BBC 6 Minute English

No JSON API exists [1][2]. Two formats are used: the RSS feed and the episode HTML page.

### 1.1 RSS feed

`GET https://podcasts.files.bbci.co.uk/p02pc9tn.rss` — RSS 2.0 with `itunes:` and BBC `ppg:` namespaces; 346 items, newest first, back to 2018-04-26 [1]. The feed URL is fixed and kept in configuration.
```xml
<item>
  <title>What should you eat for a better night's sleep?</title>
  <description><![CDATA[<p>...Georgie and Phil discuss this...</p>]]></description>
  <pubDate>Thu, 22 Oct 2026 09:56:00 +0000</pubDate>
  <itunes:duration>423</itunes:duration>
  <enclosure url="http://open.live.bbc.co.uk/mediaselector/6/redir/version/2.0/mediaset/audio-nondrm-download-rss-low/proto/http/vpid/p0phyzz9.mp3"
             length="3384000" type="audio/mpeg"/>
  <ppg:enclosureSecure url="https://open.live.bbc.co.uk/.../proto/https/vpid/p0phyzz9.mp3"
             length="3384000" type="audio/mpeg"/>
  <guid isPermaLink="false">urn:bbc:podcast:p0phz9z7</guid>
  <link>http://www.bbc.co.uk/programmes/p0phz9z7</link>
</item>
```
| Element | Format |
|---|---|
| `guid` | `urn:bbc:podcast:<pid>` — stable episode key |
| `pubDate` | RFC 822 date |
| `itunes:duration` | integer, seconds |
| `enclosure@url` | redirecting URL (3 × 302 to an ad-insertion host, signed and expiring) [4] |

The feed has **no transcript and no link to the Learning English episode page** [1].

### 1.2 Episode page (HTML)

URL pattern, keyed by air date (`pubDate` → `YYMMDD`) [2]:
- 2022 onward: `https://www.bbc.co.uk/learningenglish/english/features/6-minute-english_{YYYY}/ep-{YYMMDD}`
- before 2022: `https://www.bbc.co.uk/learningenglish/english/features/6-minute-english/ep-{YYMMDD}`

Links found in the page (file names contain a topic slug, so they must be scraped, not built) [3]:
```
https://downloads.bbc.co.uk/learningenglish/features/6min/261015_6_minute_english_why_do_people_buy_gold_download.mp3
https://downloads.bbc.co.uk/learningenglish/features/6min/261015_6_minute_english_why_do_people_buy_gold_transcript.pdf
```
The `_download.mp3` is a static file (`audio/mpeg`, no redirect) and answers byte-range requests with `206 Partial Content` [3][29].

Transcript markup — speaker turns inside one `<p>`, under an `<h3>` heading [3]:
```html
<p><strong>Georgie</strong><br />Hi Phil.<br />&nbsp;<br />
   <strong>Phil</strong><br />Hi Georgie. I've got a question for you. Do you own anything made of gold?<br />&nbsp;<br />...</p>
```
`<strong>` is also used for non-speaker labels and vocabulary words. The page states "This is not a word-for-word transcript" [3].

---

## 2. Tatoeba

**Base URL:** `https://api.tatoeba.org` · no auth · `/v1` = stable, `/unstable` = may change · no documented rate limit [9]

### 2.1 Search sentences with audio

`GET /v1/sentences?lang=eng&sort=created&has_audio=yes&include=audios&showtrans=none&limit=100`

| Param | Value | Notes |
|---|---|---|
| `lang` | ISO 639-3, comma-separated (`eng`, `fra`) | required |
| `sort` | `[-]relevance` · `[-]words` · `[-]created` · `[-]modified` · `random` | required (400 without it) |
| `has_audio` | `yes` / `no` | |
| `include` | `audios`, `transcriptions` | add `audios` to get the clip list |
| `showtrans` | `matching` · `all` · `none` | translations in the response |
| `word_count` | range, e.g. `5-10` | useful for difficulty |
| `limit` | page size | max 500 observed (undocumented) |
| `after` | cursor | take it from `paging.next` |

Response [11]:
```json
{
  "data": [
    {
      "id": 14007817,
      "text": "Dad is proud of his car.",
      "lang": "eng",
      "script": null,
      "license": "CC BY 2.0 FR",
      "owner": "<user>",
      "is_unapproved": false,
      "audios": [
        {
          "id": 1285708,
          "created": "2026-09-26T05:43:42+00:00",
          "author": "<user>",
          "license": "CC BY 4.0",
          "attribution_url": "https://tatoeba.org/user/profile/<user>",
          "download_url": "https://api.tatoeba.org/v1/audio/1285708/file",
          "modified": "2026-09-26T05:43:42+00:00"
        }
      ]
    }
  ],
  "paging": {
    "total": 850073,
    "has_next": true,
    "next": "https://api.tatoeba.org/v1/sentences?lang=eng&has_audio=yes&sort=created&limit=100&include=audios&showtrans=none&after=1786112902%2C14007790"
  }
}
```
| Field | Format |
|---|---|
| `audios[].license` | free-text licence name; **empty = no reuse** [10] |
| `audios[].created` / `modified` | ISO 8601 or `null` |
| `paging.next` | full URL with the `after` cursor; absent/`has_next:false` on the last page |

Format quirks:
- `download_url` (`/v1/audio/{id}/file`) returns **404**. The working URL is **`/v1/audios/{id}/file`** (`200 audio/mpeg`, supports range requests) — build it from `audios[].id` [11][29].
- The OpenAPI schema names the audio field `licence`; the live JSON sends `license` [9][11].
- There is no query filter on audio licence [9].

### 2.2 Audio file

`GET https://api.tatoeba.org/v1/audios/{audioId}/file` → `200 audio/mpeg`, `Content-Disposition: attachment; filename="{sentenceId}-{audioId}.mp3"`, `Access-Control-Allow-Origin: *` [11][29].

---

## 3. AssemblyAI

**Base URL:** `https://api.assemblyai.com` · version `/v2` · header `authorization: <API_KEY>` (no `Bearer`) [13]. No maintained .NET SDK — the C# SDK was archived on 2025-04-01 [17].

### 3.1 Submit

`POST /v2/transcript` · `content-type: application/json` [13]
```json
{
  "audio_url": "https://downloads.bbc.co.uk/learningenglish/features/6min/261015_6_minute_english_why_do_people_buy_gold_download.mp3",
  "language_code": "en",
  "punctuate": true,
  "format_text": true,
  "webhook_url": "https://example.com/hooks/assemblyai"
}
```
| Field | Notes |
|---|---|
| `audio_url` | required; public URL, fetched by AssemblyAI (no upload needed) |
| `language_code` | e.g. `en`; cannot be combined with `language_detection: true` |
| `speech_models` | optional ordered list; default `["universal-3-5-pro","universal-2"]` (singular `speech_model` is deprecated) |
| `speaker_labels` | optional boolean |
| `webhook_url` | optional; called when the job completes or fails |

Response:
```json
{ "id": "0072a82b-aa22-4962-add2-6121c36c17c6", "status": "queued", "audio_url": "https://...", "speech_models": ["universal-3-5-pro","universal-2"] }
```

### 3.2 Poll

`GET /v2/transcript/{id}` → `status`: `queued` · `processing` · `completed` · `error` (with an `error` message) [13]. Polling is capped at 60 requests/min per transcript (`429` + `Retry-After`) [15].

### 3.3 Get sentences

`GET /v2/transcript/{id}/sentences` [14]
```json
{
  "id": "0072a82b-aa22-4962-add2-6121c36c17c6",
  "confidence": 0.9,
  "audio_duration": 281,
  "sentences": [
    {
      "text": "Smoke from hundreds of wildfires in Canada is triggering air quality alerts throughout the US.",
      "start": 250,
      "end": 6350,
      "confidence": 0.72412,
      "speaker": null,
      "channel": null,
      "words": [
        { "text": "Smoke", "start": 250, "end": 650, "confidence": 0.97, "speaker": null }
      ]
    }
  ]
}
```
| Field | Type / unit |
|---|---|
| `sentences[].start` / `end` | integer, **milliseconds** |
| `words[].start` / `end` | integer, milliseconds |
| `audio_duration` | seconds |
| `speaker` | `null` unless `speaker_labels: true` |

Maps directly to `{index, text, startMs, endMs}`.

---

## 4. Gladia

**Base URL:** `https://api.gladia.io` · version `/v2` · header `x-gladia-key: <API_KEY>` [19]. No .NET SDK (JS/Python only) [28].

### 4.1 Submit

`POST /v2/pre-recorded` [19]
```json
{
  "audio_url": "https://downloads.bbc.co.uk/learningenglish/features/6min/..._download.mp3",
  "language_config": { "languages": ["en"], "code_switching": false },
  "callback": true,
  "callback_config": { "url": "https://example.com/hooks/gladia", "method": "POST" }
}
```
Response `201`:
```json
{ "id": "45463597-20b7-4af7-b3b3-f5fb778203ab", "result_url": "https://api.gladia.io/v2/transcription/45463597-20b7-4af7-b3b3-f5fb778203ab" }
```
Errors: `400` invalid request · `401` bad key · `422` bad parameters · `429` over concurrency [19][22].

### 4.2 Poll / result

`GET /v2/pre-recorded/{id}` (or the returned `result_url`) → `status`: `queued` · `processing` · `done` · `error`; `result` is filled when `done` [20].
```json
{
  "id": "45463597-...",
  "status": "done",
  "result": {
    "metadata": { "audio_duration": 423.1 },
    "transcription": {
      "full_transcript": "Hi Phil. Hi Georgie. ...",
      "languages": ["en"],
      "utterances": [
        {
          "start": 0.24,
          "end": 2.1,
          "confidence": 0.97,
          "channel": 0,
          "speaker": 0,
          "language": "en",
          "text": "Hi Phil.",
          "words": [ { "word": "Hi", "start": 0.24, "end": 0.41, "confidence": 0.98 } ]
        }
      ]
    }
  }
}
```
_Field names from the API reference [20]; values illustrative._

| Field | Type / unit |
|---|---|
| `utterances[].start` / `end` | float, **seconds** (× 1000 for ms) |
| `words[].word` | the word text is in `word`, not `text` |
| `utterances[]` | speech segments, not guaranteed to be single sentences |
| `speaker` | present only with `diarization: true` |

The `sentences: true` option is deprecated and returns `results` as **plain strings with no timestamps** [20]; the feature page [21] still shows timed objects, but the API reference is authoritative. Sentences with timings must be built from `words[]`.

---

## Sources

| # | Supports | Publisher | Pub date | Accessed | Confidence |
|---|---|---|---|---|---|
| [1] | RSS item format, 346 items, no transcript | [BBC — 6ME RSS feed](https://podcasts.files.bbci.co.uk/p02pc9tn.rss) | live | 2026-10-22 | high |
| [2] | Episode URL scheme | [BBC Learning English — 6 Minute English](https://www.bbc.co.uk/learningenglish/english/features/6-minute-english) | live | 2026-10-22 | high |
| [3] | Episode page markup, download links | [BBC Learning English — ep-261015](https://www.bbc.co.uk/learningenglish/english/features/6-minute-english_2026/ep-261015) | 2026-10-15 | 2026-10-22 | high |
| [4] | Enclosure redirect chain | [BBC mediaselector (live trace)](https://open.live.bbc.co.uk/mediaselector/6/redir/version/2.0/mediaset/audio-nondrm-download-rss-low/proto/https/vpid/p0phyzz9.mp3) | live | 2026-10-22 | medium |
| [9] | Endpoints, params, schema | [Tatoeba — OpenAPI spec](https://api.tatoeba.org/openapi.json) | live | 2026-10-22 | high |
| [10] | Audio licence semantics | [Tatoeba — Downloads](https://tatoeba.org/en/downloads) | live | 2026-10-22 | high |
| [11] | Live response, audio URL bug | [Tatoeba — /v1/sentences live call](https://api.tatoeba.org/v1/sentences?lang=eng&has_audio=yes&sort=created&include=audios&limit=2) | live | 2026-10-22 | high |
| [13] | Submit + poll format | [AssemblyAI — Submit transcript](https://www.assemblyai.com/docs/api-reference/transcripts/submit) | live | 2026-10-22 | high |
| [14] | Sentences response | [AssemblyAI — Get sentences](https://www.assemblyai.com/docs/api-reference/transcripts/get-sentences) | live | 2026-10-22 | high |
| [15] | Polling cap | [AssemblyAI — Concurrency](https://www.assemblyai.com/docs/pre-recorded-audio/concurrency) | live | 2026-10-22 | medium |
| [17] | C# SDK archived | [GitHub — assemblyai-csharp-sdk](https://github.com/AssemblyAI/assemblyai-csharp-sdk) | 2025-04-01 | 2026-10-22 | high |
| [19] | Submit format, errors | [Gladia — Init pre-recorded](https://docs.gladia.io/api-reference/v2/pre-recorded/init) | live | 2026-10-22 | high |
| [20] | Result format, sentences deprecated | [Gladia — Get pre-recorded](https://docs.gladia.io/api-reference/v2/pre-recorded/get) | live | 2026-10-22 | high |
| [21] | Feature page (contradicted) | [Gladia — Sentences feature](https://docs.gladia.io/chapters/pre-recorded-stt/features/sentences) | live | 2026-10-22 | low |
| [22] | 429 on concurrency | [Gladia — Concurrency](https://docs.gladia.io/chapters/limits-and-specifications/concurrency) | live | 2026-10-22 | medium |
| [28] | SDKs JS/Python only | [Gladia — SDKs](https://docs.gladia.io/chapters/integrations/sdk) | live | 2026-10-22 | medium |
| [29] | Range support on audio hosts | [BBC downloads mp3 (live range request)](https://downloads.bbc.co.uk/learningenglish/features/6min/261015_6_minute_english_why_do_people_buy_gold_download.mp3) | live | 2026-10-22 | high |

API formats change; re-check before building if past **2026-11-22**.
