document.addEventListener("DOMContentLoaded", function () {
    const rowCheckboxes = document.querySelectorAll(".row-checkbox");
    const selectAllRows = document.getElementById("select-all-rows");
    const deleteSelectedButton = document.getElementById("delete-selected");

    function updateDatabaseSelection() {
        const selectedCount = Array.from(rowCheckboxes).filter(checkbox => checkbox.checked).length;

        if (deleteSelectedButton) {
            deleteSelectedButton.disabled = selectedCount === 0;
        }

        if (!selectAllRows) {
            return;
        }

        selectAllRows.checked = rowCheckboxes.length > 0 && selectedCount === rowCheckboxes.length;
        selectAllRows.indeterminate = selectedCount > 0 && selectedCount < rowCheckboxes.length;
    }

    rowCheckboxes.forEach(checkbox => {
        checkbox.addEventListener("change", updateDatabaseSelection);
    });

    if (selectAllRows) {
        selectAllRows.addEventListener("change", function () {
            rowCheckboxes.forEach(checkbox => {
                checkbox.checked = this.checked;
            });

            updateDatabaseSelection();
        });
    }

    const jsonToggleButtons = document.querySelectorAll("[data-json-toggle]");

    jsonToggleButtons.forEach(button => {
        button.addEventListener("click", function () {
            const targetId = button.dataset.target;

            if (!targetId) {
                return;
            }

            const row = document.getElementById(targetId);

            if (!row) {
                return;
            }

            const isOpen = !row.hidden;
            row.hidden = isOpen;
            button.setAttribute("aria-expanded", (!isOpen).toString());

            const icon = button.querySelector(".debug-json-toggle-icon");

            if (icon) {
                icon.textContent = isOpen ? button.dataset.symbolCollapsed : button.dataset.symbolExpanded;
            }
        });
    });

    const logEntries = document.querySelectorAll(".log-entry");
    const logApplicationFilter = document.getElementById("log-application-filter");
    const logCategoryFilter = document.getElementById("log-category-filter");
    const logLevelFilter = document.getElementById("log-level-filter");
    const logSearch = document.getElementById("log-search");
    const visibleLogCount = document.getElementById("visible-log-count");
    const noVisibleLogs = document.getElementById("no-visible-logs");
    const exportLogsTxt = document.getElementById("export-logs-txt");
    const exportLogsJson = document.getElementById("export-logs-json");

    function updateExportLinks() {
        [exportLogsTxt, exportLogsJson].forEach(link => {
            if (!link?.dataset.exportBase) return;
            const url = new URL(link.dataset.exportBase, window.location.origin);
            if (logApplicationFilter?.value) url.searchParams.set("application", logApplicationFilter.value);
            if (logCategoryFilter?.value) url.searchParams.set("category", logCategoryFilter.value);
            if (logLevelFilter?.value) url.searchParams.set("level", logLevelFilter.value);
            if (logSearch?.value.trim()) url.searchParams.set("search", logSearch.value.trim());
            link.href = url.pathname + url.search;
        });
    }

    function updateLogFilters() {
        const application = logApplicationFilter?.value ?? "";
        const category = logCategoryFilter?.value ?? "";
        const level = logLevelFilter?.value ?? "";
        const search = (logSearch?.value ?? "").trim().toLowerCase();
        let visibleCount = 0;

        logEntries.forEach(entry => {
            const matchesApplication = application === "" || entry.dataset.application === application;
            const matchesCategory = category === "" || entry.dataset.category === category;
            const matchesLevel = level === "" || entry.dataset.level === level;
            const matchesSearch = search === "" || entry.textContent.toLowerCase().includes(search);
            const visible = matchesApplication && matchesCategory && matchesLevel && matchesSearch;

            entry.style.display = visible ? "" : "none";

            if (visible) {
                visibleCount++;
            }
        });

        if (visibleLogCount) {
            visibleLogCount.textContent = visibleCount;
        }

        if (noVisibleLogs) {
            noVisibleLogs.style.display = visibleCount === 0 ? "block" : "none";
        }

        updateExportLinks();
    }

    logApplicationFilter?.addEventListener("change", updateLogFilters);
    logCategoryFilter?.addEventListener("change", updateLogFilters);
    logLevelFilter?.addEventListener("change", updateLogFilters);
    logSearch?.addEventListener("input", updateLogFilters);

    updateDatabaseSelection();
    updateLogFilters();
});
