// Virtual Education Management System — lightweight client helpers

(function () {
  'use strict';

  // Close the mobile navbar after clicking an in-page anchor for smoother UX
  document.querySelectorAll('.public-main-nav .nav-link[href^="#"]').forEach(function (link) {
    link.addEventListener('click', function () {
      var nav = document.getElementById('publicNavbar');
      if (nav && nav.classList.contains('show') && window.bootstrap) {
        var collapse = window.bootstrap.Collapse.getOrCreateInstance(nav);
        collapse.hide();
      }
    });
  });

  // Hero stats: reveal cards + count up when the band scrolls into view
  function runStatCounters() {
    document.querySelectorAll('.hero-stat-number[data-stat-target]').forEach(function (el) {
      var target = parseInt(el.getAttribute('data-stat-target'), 10);
      var suffix = el.getAttribute('data-stat-suffix') || '';
      if (isNaN(target)) return;
      var duration = 1450;
      var t0 = performance.now();
      function tick(now) {
        var p = Math.min((now - t0) / duration, 1);
        var eased = 1 - Math.pow(1 - p, 3);
        var val = Math.round(target * eased);
        el.textContent = val.toLocaleString('en-US') + suffix;
        if (p < 1) requestAnimationFrame(tick);
      }
      requestAnimationFrame(tick);
    });
  }

  var statsRoot = document.querySelector('.hero-stats-section');
  if (statsRoot) {
    var statsActivated = false;
    function activateStats() {
      if (statsActivated) return;
      statsActivated = true;
      statsRoot.classList.add('is-visible');
      runStatCounters();
    }

    if (window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
      statsRoot.classList.add('is-visible');
      document.querySelectorAll('.hero-stat-number[data-stat-target]').forEach(function (el) {
        var t = parseInt(el.getAttribute('data-stat-target'), 10);
        var s = el.getAttribute('data-stat-suffix') || '';
        if (!isNaN(t)) el.textContent = t.toLocaleString('en-US') + s;
      });
    } else if ('IntersectionObserver' in window) {
      var obs = new IntersectionObserver(
        function (entries) {
          entries.forEach(function (entry) {
            if (entry.isIntersecting) {
              activateStats();
              obs.disconnect();
            }
          });
        },
        { threshold: 0.15, rootMargin: '0px 0px -8% 0px' }
      );
      obs.observe(statsRoot);
    } else {
      activateStats();
    }
  }
})();
