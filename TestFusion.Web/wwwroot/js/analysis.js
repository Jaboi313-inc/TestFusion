document.addEventListener("DOMContentLoaded", function () {
    const validationDetails = document.querySelector(".analysis-validation-details");

    if (validationDetails) {
        document.addEventListener("click", function (event) {
            if (!validationDetails.open) {
                return;
            }

            if (!validationDetails.contains(event.target)) {
                validationDetails.removeAttribute("open");
            }
        });
    }

    function clearAnalysisHighlights() {
        const analysisTable = document.querySelector(".analysis-table");

        if (!analysisTable) {
            return;
        }

        analysisTable.classList.remove("analysis-has-highlight");
        analysisTable
            .querySelectorAll(
                ".analysis-highlight-row, " +
                ".analysis-highlight-column, " +
                ".analysis-highlight-target, " +
                ".analysis-highlight-target-first, " +
                ".analysis-highlight-target-last"
            )
            .forEach(element => {
                element.classList.remove(
                    "analysis-highlight-row",
                    "analysis-highlight-column",
                    "analysis-highlight-target",
                    "analysis-highlight-target-first",
                    "analysis-highlight-target-last"
                );
            });
    }

    const validationJumpButtons = document.querySelectorAll(".analysis-validation-jump");

    validationJumpButtons.forEach(button => {
        button.addEventListener("click", function (event) {
            event.preventDefault();

            const targetId = button.dataset.scrollTarget;
            const target = document.getElementById(targetId);
            const analysisTable = document.querySelector(".analysis-table");

            if (!target || !analysisTable) {
                return;
            }

            clearAnalysisHighlights();

            const targetCell = target.closest("td, th");
            const targetRow = target.closest("tr");

            if (!targetCell || !targetRow) {
                return;
            }

            const columnIndex = targetCell.cellIndex;
            let firstTestRow = targetRow;

            while (
                firstTestRow.previousElementSibling &&
                !firstTestRow.classList.contains("test-header")
            ) {
                firstTestRow = firstTestRow.previousElementSibling;
            }

            const testRows = [];
            let currentRow = firstTestRow;

            while (currentRow) {
                if (currentRow !== firstTestRow && currentRow.classList.contains("test-header")) {
                    break;
                }

                testRows.push(currentRow);
                currentRow = currentRow.nextElementSibling;
            }

            testRows.forEach(row => {
                row.querySelectorAll("td, th").forEach(cell => {
                    cell.classList.add("analysis-highlight-row");
                });
            });

            analysisTable.querySelectorAll("tr").forEach(row => {
                const cell = row.cells[columnIndex];

                if (cell) {
                    cell.classList.add("analysis-highlight-column");
                }
            });

            const highlightedTargetCells = [];

            testRows.forEach(row => {
                const cell = row.cells[columnIndex];

                if (cell) {
                    cell.classList.add("analysis-highlight-target");
                    highlightedTargetCells.push(cell);
                }
            });

            if (highlightedTargetCells.length > 0) {
                highlightedTargetCells[0].classList.add("analysis-highlight-target-first");
                highlightedTargetCells[highlightedTargetCells.length - 1].classList.add("analysis-highlight-target-last");
            }

            analysisTable.classList.add("analysis-has-highlight");

            const details = button.closest("details");

            if (details) {
                details.removeAttribute("open");
            }

            const navbar = document.querySelector("header");
            const tableHeader = analysisTable.querySelector("thead");
            const navbarHeight = navbar?.getBoundingClientRect().height ?? 0;
            const tableHeaderHeight = tableHeader?.getBoundingClientRect().height ?? 0;
            const firstTestRect = firstTestRow.getBoundingClientRect();
            const targetRect = targetCell.getBoundingClientRect();
            const topOffset = navbarHeight + tableHeaderHeight;
            const scrollTop = firstTestRect.top + window.scrollY - topOffset;
            const scrollLeft = targetRect.left + window.scrollX - (window.innerWidth / 2) + (targetRect.width / 2);

            window.scrollTo({
                top: Math.max(0, scrollTop),
                left: Math.max(0, scrollLeft),
                behavior: "smooth"
            });

            window.history.replaceState({}, "", `#${targetId}`);
        });
    });

    document.addEventListener("click", function (event) {
        const analysisTable = document.querySelector(".analysis-table");

        if (!analysisTable || !analysisTable.classList.contains("analysis-has-highlight")) {
            return;
        }

        if (event.target.closest(".analysis-highlight-row")) {
            return;
        }

        if (event.target.closest(".analysis-validation-details")) {
            return;
        }

        if (event.target.closest("#backToLeftToast, #backToTopToast, #backToStartToast")) {
            return;
        }

        clearAnalysisHighlights();
    });

    const layoutSelect = document.getElementById("layoutMode");

    if (layoutSelect) {
        const storageKey = "analysisPdfLayoutMode";
        const savedLayout = sessionStorage.getItem(storageKey);

        if (savedLayout) {
            const optionExists = Array.from(layoutSelect.options).some(option => option.value === savedLayout);

            if (optionExists) {
                layoutSelect.value = savedLayout;
            }
        }

        layoutSelect.addEventListener("change", function () {
            sessionStorage.setItem(storageKey, layoutSelect.value);

            const url = new URL(window.location.href);
            url.searchParams.set("layoutMode", layoutSelect.value);
            window.history.replaceState({}, "", url);
        });
    }

    const pdfTestSelector = document.getElementById("pdf-test-selector");
    const pdfTestCheckboxes = Array.from(document.querySelectorAll(".analysis-pdf-test-checkbox"));
    const pdfTestSelectorCount = document.getElementById("pdf-test-selector-count");
    const pdfSelectAllButton = document.getElementById("pdf-test-select-all");
    const pdfSelectNoneButton = document.getElementById("pdf-test-select-none");
    const pdfDownloadButton = document.getElementById("pdf-download-button");

    function updatePdfTestSelection() {
        const selectedCount = pdfTestCheckboxes.filter(checkbox => checkbox.checked).length;

        if (pdfTestSelectorCount) {
            pdfTestSelectorCount.textContent = `${selectedCount}/${pdfTestCheckboxes.length}`;
        }

        if (pdfDownloadButton) {
            pdfDownloadButton.disabled = selectedCount === 0;
        }
    }

    pdfTestCheckboxes.forEach(checkbox => {
        checkbox.addEventListener("change", updatePdfTestSelection);
    });

    if (pdfSelectAllButton) {
        pdfSelectAllButton.addEventListener("click", function () {
            pdfTestCheckboxes.forEach(checkbox => checkbox.checked = true);
            updatePdfTestSelection();
        });
    }

    if (pdfSelectNoneButton) {
        pdfSelectNoneButton.addEventListener("click", function () {
            pdfTestCheckboxes.forEach(checkbox => checkbox.checked = false);
            updatePdfTestSelection();
        });
    }

    document.addEventListener("click", function (event) {
        if (!pdfTestSelector?.open || pdfTestSelector.contains(event.target)) {
            return;
        }

        pdfTestSelector.removeAttribute("open");
    });

    document.addEventListener("keydown", function (event) {
        if (event.key !== "Escape") {
            return;
        }

        clearAnalysisHighlights();

        if (validationDetails?.open) {
            validationDetails.removeAttribute("open");
        }

        if (pdfTestSelector?.open) {
            pdfTestSelector.removeAttribute("open");
        }
    });

    updatePdfTestSelection();
});