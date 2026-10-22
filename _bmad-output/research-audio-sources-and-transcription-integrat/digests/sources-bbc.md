# Build notes: BBC 6 Minute English

API formats: see the main report, section 1.

## Ingestion flow
1. **Read the feed.** The URL comes from config: `https://podcasts.files.bbci.co.uk/p02pc9tn.rss`. It lists 346 episodes, back to 2018-04.
2. **Deduplicate** on RSS `<guid>` (`urn:bbc:podcast:<pid>`).
3. **Find the episode page** from `pubDate` → `ep-YYMMDD`:
   - 2022 onward: `/learningenglish/english/features/6-minute-english_{YYYY}/ep-{YYMMDD}`
   - before 2022: `/learningenglish/english/features/6-minute-english/ep-{YYMMDD}`
   - This date join was verified on one episode only. Fallback: scrape the index page `/learningenglish/english/features/6-minute-english`, which lists every `ep-` link.
4. **Scrape the page** for:
   - the `…_download.mp3` link, which becomes the lesson audio URL;
   - the transcript HTML, which is optional reference text.
5. **Hand the MP3 URL to the transcriber.**

## Audio: which URL
- **Use the static `downloads.bbc.co.uk/…_download.mp3`** for both transcription and playback.
  - It is served directly from S3.
  - It supports byte-range requests (`206`), so the player can seek to a sentence.
- **Do not use the RSS `enclosure`.**
  - It redirects 3 times to an ad-insertion host.
  - The URLs are signed and expire after about 6 h.
  - The delivered file was larger than the declared length, so timestamps may not match between two fetches.
- Download file names contain a topic slug and can't be built from the date. Always take them from the page.

## Transcript parsing
- Speaker turns look like `<strong>Name</strong><br/>text<br/>&nbsp;<br/>` inside a single `<p>`.
- `<strong>` is also used for labels ("Introduction") and vocabulary words, so speaker detection needs a heuristic, e.g. a known speaker name followed by `<br/>`.
- The page says "This is not a word-for-word transcript". Use it for display only; segment text and timings come from ASR.
- The transcript may be missing (the RSS currently says the site is having "technical issues"). The import must not fail when it is.

## HTTP client
- Follow redirects. Send a normal `User-Agent`: one fetch tool was blocked on bbc.co.uk, while plain curl worked.

## Usage constraint
- BBC terms permit personal use only. Don't re-host or redistribute the audio; link or stream from the BBC URL.
