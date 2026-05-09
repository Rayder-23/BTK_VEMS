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
})();
