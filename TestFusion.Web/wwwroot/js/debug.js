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

    const logEntries = document.querySelectorAll(".log-entry");
    const logSourceFilter = document.getElementById("log-source-filter");
    const logLevelFilter = document.getElementById("log-level-filter");
    const logSearch = document.getElementById("log-search");
    const visibleLogCount = document.getElementById("visible-log-count");
    const noVisibleLogs = document.getElementById("no-visible-logs");

    function updateLogFilters() {
        const source = logSourceFilter?.value ?? "";
        const level = logLevelFilter?.value ?? "";
        const search = (logSearch?.value ?? "").trim().toLowerCase();
        let visibleCount = 0;

        logEntries.forEach(entry => {
            const matchesSource = source === "" || entry.dataset.source === source;
            const matchesLevel = level === "" || entry.dataset.level === level;
            const matchesSearch = search === "" || entry.textContent.toLowerCase().includes(search);
            const visible = matchesSource && matchesLevel && matchesSearch;

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
    }

    logSourceFilter?.addEventListener("change", updateLogFilters);
    logLevelFilter?.addEventListener("change", updateLogFilters);
    logSearch?.addEventListener("input", updateLogFilters);

    updateDatabaseSelection();
    updateLogFilters();
});
