// Preloads one <audio preload="auto"> per distinct audio URL, straight from the source host.
// Audio stays in memory only: no fetch()/blob, Cache Storage, IndexedDB, localStorage or service worker.
const audioLoader = (() => {
  const loadTimeoutMs = 20000;

  const messages = {
    network: "Couldn't download the audio. Check your connection, or the source may be temporarily unavailable.",
    unsupported: 'The audio is no longer available at its source (the link may have changed or been removed).',
    decode: "The audio file is damaged and can't be played.",
    timeout: 'The audio is taking too long to load.',
  };

  function errorMessage(audio) {
    switch (audio.error?.code) {
      case MediaError.MEDIA_ERR_DECODE: return messages.decode;
      case MediaError.MEDIA_ERR_SRC_NOT_SUPPORTED: return messages.unsupported;
      default: return messages.network;
    }
  }

  /**
   * Creates the audio elements for `urls`. `onChange(url)` fires whenever one becomes ready or fails.
   * Each entry is `{ audio, state: 'loading' | 'ready' | 'error', message }`.
   */
  function load(urls, onChange) {
    const entries = new Map();

    for (const url of new Set(urls)) {
      const entry = { audio: new Audio(), state: 'loading', message: '', timer: 0 };
      entries.set(url, entry);

      const settle = (state, message = '') => {
        clearTimeout(entry.timer);
        entry.state = state;
        entry.message = message;
        onChange(url);
      };

      entry.audio.preload = 'auto';
      entry.audio.addEventListener('canplaythrough', () => {
        if (entry.state !== 'ready') {
          settle('ready');
        }
      });
      entry.audio.addEventListener('error', () => settle('error', errorMessage(entry.audio)));

      entry.start = () => {
        clearTimeout(entry.timer);
        entry.state = 'loading';
        entry.timer = setTimeout(() => {
          if (entry.state === 'loading') {
            settle('error', messages.timeout);
          }
        }, loadTimeoutMs);
        entry.audio.src = url;
        entry.audio.load();
      };

      entry.start();
    }

    return {
      get: url => entries.get(url),
      /** Reloads the audio element for `url` after a failure. */
      retry: url => {
        entries.get(url)?.start();
        onChange(url);
      },
    };
  }

  return { load };
})();
