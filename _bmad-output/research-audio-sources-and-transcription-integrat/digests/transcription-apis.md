# Build notes: AssemblyAI

API formats: see the main report, section 3.

## Common shape (one interface; AssemblyAI is the only implementation)
```
SubmitAsync(audioUrl, language) -> providerJobId
GetStatusAsync(providerJobId)   -> Pending | Processing | Completed | Failed(reason)
GetSentencesAsync(providerJobId) -> [ {index, text, startMs, endMs} ]
```
- The provider fetches the audio from a public URL itself, so no upload is needed.
- There is no maintained .NET SDK (AssemblyAI's C# SDK was archived 2025-04-01). Use plain `HttpClient` with your own DTOs.
- Run it as persisted job state rather than a blocking wait:
  - one tick submits and stores the job id;
  - later ticks poll once per tick until the job completes or fails;
  - this survives restarts and stays far under the polling limits.
- Webhooks are optional; polling is simpler for a local app.

## AssemblyAI
- **Submit:** `POST /v2/transcript` with `{audio_url, language_code}`. Header `authorization: <key>`.
- **Statuses:** `queued` / `processing` → Pending/Processing; `completed` → Completed; `error` → Failed (the reason is in `error`).
- **Sentences:** `GET /v2/transcript/{id}/sentences`. `start`/`end` are integer ms and map 1:1. Word timings are available if a sentence is too long for dictation.
- **Limits:**
  - Free tier runs 5 concurrent jobs; extra jobs queue rather than fail.
  - Polling is capped at 60 requests/min per transcript; over that returns `429` with `Retry-After`, so honour it.
  - Max 5 GB / 10 h per file.
- **Cost:** $50 free credit, $0.15–0.21 per audio hour, which is plenty for a demo.
- **Open:** whether `language_code` should be `en` or `en_us`. Confirm with the first call.

## Error handling to plan for
- Provider `error`/failed status: mark the lesson Failed with the reason; allow a manual retry.
- `429`: back off (use `Retry-After` when present).
- `401`: configuration error. Fail fast and don't retry.
- Audio URL not reachable by the provider: it shows up as a provider error. Check that the URL is the static BBC file, not the expiring enclosure.

## To test before relying on it
- How long AssemblyAI sentences are on two-speaker conversation. If they are too long, split them on word timings.
- Real processing time for a 6-minute episode (no vendor publishes one).
