(function () {
    document.querySelectorAll('.portal-sidebar-link').forEach(function (link) {
        link.addEventListener('click', function () {
            var offcanvas = document.getElementById('portalSidebar');
            if (!offcanvas || !window.bootstrap || !offcanvas.classList.contains('show')) {
                return;
            }

            var instance = bootstrap.Offcanvas.getInstance(offcanvas);
            if (instance) {
                instance.hide();
            }
        });
    });
})();
