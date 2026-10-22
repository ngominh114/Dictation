# Build notes: Tatoeba

API formats: see the main report, section 2.

## Ingestion flow
1. **Fetch a page.** `GET /v1/sentences?lang=eng&sort=created&has_audio=yes&include=audios&showtrans=none&limit=100`
   - Optional: add `word_count=5-12` to keep sentences a sensible length for dictation.
2. **Page through results** by following `paging.next` until `has_next` is false.
   - Stop after a configured number of sentences; the job does not need all of them (about 850k English sentences have audio).
3. **Filter each sentence's `audios[]` by licence.**
   - Keep an allowlist in config.
   - An empty licence means "no reuse": always exclude it.
   - Most English audio is `CC BY-NC-ND 3.0`, which is acceptable for personal, non-commercial use.
4. **Build the audio URL from the id:** `https://api.tatoeba.org/v1/audios/{audio.id}/file`.
   - Do not use `download_url`; it returns 404.
5. **Store attribution per clip** (`author`, `license`, `attribution_url`) and show it in the UI.
6. **Group N sentences (e.g. 10) into one practice item.** Each sentence becomes one segment with its own clip URL. No transcription is needed.
7. **Deduplicate** on sentence `id` + audio `id`.

## Notes
- The live JSON field is `license`, while the OpenAPI schema says `licence`. Bind the DTO to `license`.
- `sort=created` returned newest first. Rely on the cursor, not on ordering.
- `limit` is effectively capped at 500 (undocumented).
- There is no documented rate limit. Throttle anyway, e.g. 1 request per second, and cache.
- The audio endpoint supports range requests and sends `Access-Control-Allow-Origin: *`, so the browser can play it directly.
- Don't modify the audio (no trimming or transcoding): the ND licences forbid derivatives. Play the original file.
- Other languages: `lang` takes ISO 639-3 codes (`fra`, `deu`, …). Transcription APIs take `en`/`fr`, so keep a mapping.
- Bulk alternative (not needed for the demo): weekly exports at `https://downloads.tatoeba.org/exports/`.
  - `sentences_with_audio.tar.bz2` contains sentence id, audio id, username, licence and attribution URL.
  - Join it with `per_language/eng/eng_sentences.tsv.bz2`.
