// Lesson list: pages through GET /api/lessons. The page number lives in the URL (?page=N).
(() => {
  const pageSize = 20;
  const status = document.getElementById('status');
  const list = document.getElementById('lessons');
  const previous = document.getElementById('previous');
  const next = document.getElementById('next');
  const pageLabel = document.getElementById('page');

  const page = Math.max(1, Number.parseInt(new URLSearchParams(location.search).get('page'), 10) || 1);

  async function load() {
    try {
      const response = await fetch(`/api/lessons?page=${page}&pageSize=${pageSize}`);
      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }

      render(await response.json());
    } catch {
      status.textContent = "Couldn't load the lessons. Check that the app is running, then reload the page.";
    }
  }

  const badge = text => {
    const span = document.createElement('span');
    span.className = 'badge';
    span.textContent = text;
    return span;
  };

  const plural = (count, word) => `${count} ${word}${count === 1 ? '' : 's'}`;

  function render(result) {
    const pageCount = Math.max(1, Math.ceil(result.totalCount / result.pageSize));
    status.textContent = result.totalCount === 0
      ? 'No lessons yet. They appear here once the import has run.'
      : plural(result.totalCount, 'lesson');

    list.replaceChildren(...result.items.map(lesson => {
      const item = document.createElement('li');
      const link = document.createElement('a');
      link.href = `practice.html?id=${encodeURIComponent(lesson.id)}`;
      link.textContent = lesson.title;
      const meta = document.createElement('div');
      meta.className = 'muted';
      meta.append(badge(lesson.source), plural(lesson.segmentCount, 'sentence'));
      item.append(link, meta);
      return item;
    }));

    pageLabel.textContent = `Page ${result.page} of ${pageCount}`;
    previous.disabled = result.page <= 1;
    next.disabled = result.page >= pageCount;
  }

  previous.addEventListener('click', () => { location.search = `?page=${page - 1}`; });
  next.addEventListener('click', () => { location.search = `?page=${page + 1}`; });

  load();
})();
