// Practice page: plays one sentence of a lesson at a time (GET /api/lessons/{id}).
(() => {
  const el = id => document.getElementById(id);
  const ui = {
    title: el('title'),
    source: el('source'),
    status: el('status'),
    error: el('error'),
    errorMessage: el('error-message'),
    retry: el('retry'),
    position: el('position'),
    previous: el('previous'),
    play: el('play'),
    replay: el('replay'),
    next: el('next'),
    toggleTranscript: el('toggle-transcript'),
    transcript: el('transcript'),
  };

  let lesson = null;
  let audios = null;
  let current = 0;
  let stopCheck = 0;
  let playing = null;
  let stopAtEnd = null;
  let pendingPlay = false;

  const segment = () => lesson.segments[current];
  const currentAudio = () => audios.get(segment().audioUrl);

  async function start() {
    const id = new URLSearchParams(location.search).get('id');
    if (!id) {
      return fail('This lesson was not found.');
    }

    try {
      const response = await fetch(`/api/lessons/${encodeURIComponent(id)}`);
      if (response.status === 404) {
        return fail('This lesson was not found.');
      }

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }

      lesson = await response.json();
    } catch {
      return fail("Couldn't load the lesson. Check that the app is running, then reload the page.");
    }

    document.title = `${lesson.title} · Dictation Practice`;
    ui.title.textContent = lesson.title;
    const link = document.createElement('a');
    link.href = lesson.sourceUrl;
    link.textContent = 'Source';
    link.target = '_blank';
    link.rel = 'noopener';
    ui.source.replaceChildren(`${lesson.source} · `, link);
    renderTranscript();
    ui.toggleTranscript.disabled = false;

    audios = audioLoader.load(lesson.segments.map(s => s.audioUrl), url => {
      render();
      // Next/Previous pressed while the clip was still loading: play it once it is ready.
      if (pendingPlay && url === segment().audioUrl && currentAudio().state === 'ready') {
        playSegment(segment());
      }
    });
    render();
  }

  function fail(message) {
    ui.title.textContent = 'Lesson unavailable';
    ui.status.textContent = message;
  }

  /** Plays exactly `s`'s audio range: seek to startMs, stop at endMs (or the end of the file). */
  function playSegment(s) {
    stop();
    const entry = audios.get(s.audioUrl);
    pendingPlay = entry?.state === 'loading';
    if (entry?.state !== 'ready') {
      return;
    }

    const audio = entry.audio;
    playing = audio;
    const begin = () => {
      audio.currentTime = s.startMs / 1000;
      audio.play().catch(() => {});
      if (s.endMs != null) {
        const reachedEnd = () => audio.currentTime * 1000 >= s.endMs;
        const check = () => {
          if (reachedEnd()) {
            stop();
          } else {
            stopCheck = requestAnimationFrame(check);
          }
        };
        stopCheck = requestAnimationFrame(check);
        // rAF pauses in a background tab; timeupdate keeps stopping at endMs there.
        stopAtEnd = () => {
          if (reachedEnd()) {
            stop();
          }
        };
        audio.addEventListener('timeupdate', stopAtEnd);
      }
    };

    if (audio.readyState >= HTMLMediaElement.HAVE_METADATA) {
      begin();
    } else {
      audio.addEventListener('loadedmetadata', begin, { once: true });
    }
  }

  function stop() {
    cancelAnimationFrame(stopCheck);
    stopCheck = 0;
    if (stopAtEnd) {
      playing?.removeEventListener('timeupdate', stopAtEnd);
      stopAtEnd = null;
    }
    playing?.pause();
    playing = null;
  }

  function go(index) {
    current = index;
    render();
    playSegment(segment());
  }

  function render() {
    const count = lesson.segments.length;
    const entry = currentAudio();
    const ready = entry?.state === 'ready';

    ui.position.textContent = `Sentence ${segment().index + 1} of ${count}`;
    ui.status.hidden = entry?.state !== 'loading';
    ui.status.textContent = 'Loading the audio…';
    ui.error.hidden = entry?.state !== 'error';
    ui.errorMessage.textContent = entry?.message ?? '';

    ui.play.disabled = !ready;
    ui.replay.disabled = !ready;
    ui.previous.disabled = current === 0;
    ui.next.disabled = current === count - 1;

    for (const item of ui.transcript.children) {
      item.classList.toggle('current', Number(item.dataset.index) === current);
    }
  }

  function renderTranscript() {
    ui.transcript.replaceChildren(...lesson.segments.map((s, i) => {
      const item = document.createElement('li');
      item.dataset.index = String(i);
      item.textContent = s.text;
      return item;
    }));
  }

  ui.play.addEventListener('click', () => playSegment(segment()));
  ui.replay.addEventListener('click', () => playSegment(segment()));
  ui.previous.addEventListener('click', () => go(current - 1));
  ui.next.addEventListener('click', () => go(current + 1));
  ui.retry.addEventListener('click', () => audios.retry(segment().audioUrl));
  ui.toggleTranscript.addEventListener('click', () => {
    ui.transcript.hidden = !ui.transcript.hidden;
    ui.toggleTranscript.textContent = ui.transcript.hidden ? 'Show transcript' : 'Hide transcript';
  });

  start();
})();
