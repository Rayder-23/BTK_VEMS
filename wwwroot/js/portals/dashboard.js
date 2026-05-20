/**
 * Fee ERP shell — tooltips, sidebar width, placeholder nav links.
 */
(function () {
    "use strict";

    const body = document.body;
    const collapseBtn = document.getElementById("feeErpSidebarCollapse");

    function initTooltips() {
        if (typeof bootstrap === "undefined" || !bootstrap.Tooltip) {
            return;
        }
        document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function (el) {
            if (!bootstrap.Tooltip.getInstance(el)) {
                new bootstrap.Tooltip(el);
            }
        });
    }

    document.querySelectorAll(".fee-erp-nav-link--soon").forEach(function (a) {
        a.addEventListener("click", function (e) {
            e.preventDefault();
        });
    });

    if (collapseBtn) {
        collapseBtn.addEventListener("click", function () {
            body.classList.toggle("fee-erp-sidebar-collapsed");
            try {
                localStorage.setItem("feeErpSidebarCollapsed", body.classList.contains("fee-erp-sidebar-collapsed") ? "1" : "0");
            } catch (_) {
                /* ignore */
            }
        });

        try {
            if (localStorage.getItem("feeErpSidebarCollapsed") === "1") {
                body.classList.add("fee-erp-sidebar-collapsed");
            }
        } catch (_) {
            /* ignore */
        }
    }

    window.addEventListener("load", initTooltips);
    document.addEventListener("DOMContentLoaded", initTooltips);
})();
