// Only local HEAD requests; no Agent/API access. Future links remain disabled until built.
for (const link of document.querySelectorAll('[data-upcoming]')) {
  try {
    const response = await fetch(link.dataset.upcoming, { method: 'HEAD' });
    if (response.ok) { link.href = link.dataset.upcoming; link.hidden = false; link.parentElement.querySelector('[data-pending]').hidden = true; }
  } catch { /* Static launcher remains usable if a future artifact is absent. */ }
}
