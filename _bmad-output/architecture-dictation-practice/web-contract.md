# Web contract — Dictation Practice

Companion to `architecture-dictation-practice.md`. It implements AD-3, AD-9 and AD-11: the REST endpoints the UI calls, and the player's behaviour.

## REST endpoints

Only `Ready` lessons are exposed. A missing or non-`Ready` id returns 404 `ProblemDetails`.

| Endpoint | Returns |
| --- | --- |
| `GET /api/lessons?page&pageSize` | `{ items: [{ id, title, source, language, segmentCount, createdAt }], page, pageSize, totalCount }` |
| `GET /api/lessons/{id}` | `{ id, title, source, language, sourceUrl, segments: [{ index, audioUrl, startMs, endMs?, attribution? }] }` (no text) |
| `GET /api/lessons/{id}/transcript` | `[{ index, text }]` |

- **Paging:** `page` is 1-based (default 1). `pageSize` defaults to 20, range 1–100; out of range → 400 `ProblemDetails`.
- **Order:** `createdAt DESC, id DESC`.
- **Segments:** sorted by `index` ascending.
- **Attribution:** `{ author, license, url }`, omitted when null.

## Pages

- `index.html` shows the paginated lesson list.
- `practice.html` is the player, plus the transcript button. The button calls the transcript endpoint only when pressed.

## Player — `wwwroot/js`

**Preload — `audioLoader`**
- On lesson open, create one `<audio preload="auto">` per distinct `audioUrl`, pointing straight at the source host: 1 for a BBC lesson, 1 per clip for Tatoeba.
- Audio is held in memory only. Never use `fetch()`/blob, Cache Storage, IndexedDB, localStorage or a service worker.
- Controls stay disabled behind a loading indicator until the needed audio fires `canplaythrough`.

**Playback — `playSegment(segment)`**
- One function shared by play, replay, next and previous.
- Seek `startMs` after `loadedmetadata`, then play.
- Stop when a `requestAnimationFrame` check sees `currentTime × 1000 ≥ endMs`, or at the file end when `endMs` is absent. Don't rely on `timeupdate` alone.
- Never inspect `source`. The UI shows `index + 1`.
- Next and previous stop at the first and last segment.

**Load errors**
- Handle the audio `error` event, plus a 20 s timeout if `canplaythrough` never fires.
- Show a visible error panel with a **Retry** button that reloads that audio element.

| Cause | Message |
| --- | --- |
| `MEDIA_ERR_NETWORK` | "Couldn't download the audio. Check your connection, or the source may be temporarily unavailable." |
| `MEDIA_ERR_SRC_NOT_SUPPORTED` | "The audio is no longer available at its source (the link may have changed or been removed)." |
| `MEDIA_ERR_DECODE` | "The audio file is damaged and can't be played." |
| Timeout | "The audio is taking too long to load." |

- **BBC:** a failure blocks the whole lesson.
- **Tatoeba:** a clip failure marks only its segment unavailable. Next, previous and the transcript still work.
