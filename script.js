// Dark mode toggle with preference persistence
const root = document.documentElement;
const themeToggle = document.getElementById('theme-toggle');
const saved = localStorage.getItem('theme');
if (saved === 'dark') root.classList.add('dark');

function updateTogglePressed() {
  const isDark = root.classList.contains('dark');
  themeToggle?.setAttribute('aria-pressed', String(isDark));
}
updateTogglePressed();

themeToggle?.addEventListener('click', () => {
  root.classList.toggle('dark');
  const isDark = root.classList.contains('dark');
  localStorage.setItem('theme', isDark ? 'dark' : 'light');
  updateTogglePressed();
});

// Mobile menu
const menuBtn = document.getElementById('menu-toggle');
const nav = document.getElementById('primary-nav');
menuBtn?.addEventListener('click', () => {
  const isOpen = nav.classList.toggle('open');
  menuBtn.setAttribute('aria-expanded', String(isOpen));
});

// Smooth scroll for internal links
document.querySelectorAll('a[href^="#"]').forEach(a => {
  a.addEventListener('click', (e) => {
    const id = a.getAttribute('href').slice(1);
    const target = document.getElementById(id);
    if (target) {
      e.preventDefault();
      target.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
  });
});

// Project filters (aria-pressed state)
const filterChips = document.querySelectorAll('.filters .chip');
const cards = document.querySelectorAll('.project');
filterChips.forEach(chip => {
  chip.addEventListener('click', () => {
    filterChips.forEach(c => { c.classList.remove('active'); c.setAttribute('aria-pressed', 'false'); });
    chip.classList.add('active');
    chip.setAttribute('aria-pressed', 'true');

    const tag = chip.dataset.filter;
    cards.forEach(card => {
      const includes = card.dataset.tags.includes(tag) || tag === 'all';
      card.style.display = includes ? '' : 'none';
    });
  });
});

// Footer year
document.getElementById('year').textContent = new Date().getFullYear();

// Contact form (simulated submit)
const form = document.querySelector('.contact-form');
const statusEl = document.getElementById('form-status');
form?.addEventListener('submit', (e) => {
  e.preventDefault();
  statusEl.textContent = 'Sending…';
  setTimeout(() => {
    statusEl.textContent = 'Message sent! (Demo)';
    form.reset();
  }, 800);
});
