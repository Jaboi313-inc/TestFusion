document.addEventListener("DOMContentLoaded", function () {
    const config = document.getElementById("home-js-config");
    const testResultsList = document.getElementById("test-results-list");

    if (!config || !testResultsList) {
        return;
    }

    const checkboxes = document.querySelectorAll('input[type="checkbox"][name="selectedIds"]');
    const selectAllSameButtons = document.querySelectorAll(".select-all-same");
    const selectTimeGroupButtons = document.querySelectorAll(".select-time-group");
    const clearSelectedButton = document.getElementById("clear-selected");
    const generateAnalysisButton = document.getElementById("generate-analysis");
    const sortButtons = document.querySelectorAll(".sort-button");
    const resetSortButton = document.getElementById("reset-sort");
    const filterToggleButton = document.getElementById("filter-toggle");
    const filterToggleIndicator = document.getElementById("filter-toggle-indicator");
    const filterPanel = document.getElementById("filter-panel");
    const filterInputs = document.querySelectorAll("#filter-panel .filter-input");
    const applyFiltersButton = document.getElementById("apply-filters");
    const clearFiltersButton = document.getElementById("clear-filters");
    const hasAppliedFilters = config.dataset.hasAppliedFilters === "true";
    const clearFiltersUrl = config.dataset.clearFiltersUrl;
    const differentPartNumberMessage = config.dataset.differentPartNumberMessage;

    let selectedPartNumber = null;
    let currentSortColumn = null;
    let currentSortDirection = "asc";

    const originalRows = Array.from(testResultsList.querySelectorAll(".data-row"));

    function getCheckboxesInCurrentOrder() {
        return Array.from(document.querySelectorAll('input[type="checkbox"][name="selectedIds"]'));
    }

    function updateSelection() {
        const currentCheckboxes = getCheckboxesInCurrentOrder();
        const selected = currentCheckboxes.filter(checkbox => checkbox.checked);

        selectAllSameButtons.forEach(button => {
            button.style.display = "none";
        });

        selectTimeGroupButtons.forEach(button => {
            button.style.display = "none";
        });

        if (selected.length === 0) {
            selectedPartNumber = null;

            if (generateAnalysisButton) {
                generateAnalysisButton.disabled = true;
            }

            if (clearSelectedButton) {
                clearSelectedButton.disabled = true;
            }

            return;
        }

        if (clearSelectedButton) {
            clearSelectedButton.disabled = false;
        }

        const partNumbers = [...new Set(selected.map(checkbox => checkbox.dataset.partnumber))];

        if (partNumbers.length !== 1) {
            if (generateAnalysisButton) {
                generateAnalysisButton.disabled = true;
            }

            return;
        }

        selectedPartNumber = partNumbers[0];

        if (generateAnalysisButton) {
            generateAnalysisButton.disabled = false;
        }

        const samePartNumberCheckboxes = currentCheckboxes.filter(
            checkbox => checkbox.dataset.partnumber === selectedPartNumber
        );

        const allSamePartNumberSelected = samePartNumberCheckboxes.every(checkbox => checkbox.checked);
        const firstSelected = selected[0];
        const row = firstSelected.closest(".data-row");

        if (!row) {
            return;
        }

        if (!allSamePartNumberSelected) {
            const selectAllButton = row.querySelector(".select-all-same");

            if (selectAllButton) {
                const countElement = selectAllButton.querySelector(".select-all-count");

                if (countElement) {
                    countElement.textContent = samePartNumberCheckboxes.length;
                }

                selectAllButton.style.display = "inline-flex";
            }
        }

        const availableTimeGroups = new Set(
            samePartNumberCheckboxes.map(checkbox => checkbox.dataset.timegroup)
        );

        if (availableTimeGroups.size <= 1) {
            return;
        }

        const selectedTimeGroups = new Set(selected.map(checkbox => checkbox.dataset.timegroup));

        if (selectedTimeGroups.size !== 1) {
            return;
        }

        const selectedTimeGroup = firstSelected.dataset.timegroup;
        const sameTimeGroupCheckboxes = samePartNumberCheckboxes.filter(
            checkbox => checkbox.dataset.timegroup === selectedTimeGroup
        );

        const allTimeGroupSelected = sameTimeGroupCheckboxes.every(checkbox => checkbox.checked);

        if (allTimeGroupSelected) {
            return;
        }

        const timeGroupButton = row.querySelector(".select-time-group");

        if (!timeGroupButton) {
            return;
        }

        const countElement = timeGroupButton.querySelector(".select-time-group-count");

        if (countElement) {
            countElement.textContent = sameTimeGroupCheckboxes.length;
        }

        timeGroupButton.style.display = "inline-flex";
    }

    function hasFilterInputValue() {
        return Array.from(filterInputs).some(input => input.value.trim() !== "");
    }

    function updateFilterButtons() {
        const hasInputValue = hasFilterInputValue();

        if (applyFiltersButton) {
            applyFiltersButton.disabled = !hasInputValue;
        }

        if (clearFiltersButton) {
            clearFiltersButton.disabled = !hasInputValue && !hasAppliedFilters;
        }
    }

    filterInputs.forEach(input => {
        input.addEventListener("input", updateFilterButtons);
        input.addEventListener("change", updateFilterButtons);
    });

    clearFiltersButton?.addEventListener("click", function () {
        if (hasAppliedFilters) {
            window.location.href = clearFiltersUrl;
            return;
        }

        filterInputs.forEach(input => {
            input.value = "";
        });

        updateFilterButtons();
    });

    function comparePartNumbers(valueA, valueB) {
        valueA = (valueA ?? "").trim().toUpperCase();
        valueB = (valueB ?? "").trim().toUpperCase();

        function getCharacterGroup(character) {
            if (/[0-9]/.test(character)) {
                return 0;
            }

            if (/[A-Z]/.test(character)) {
                return 1;
            }

            return 2;
        }

        const maxLength = Math.max(valueA.length, valueB.length);

        for (let i = 0; i < maxLength; i++) {
            const charA = valueA[i];
            const charB = valueB[i];

            if (charA === undefined) {
                return -1;
            }

            if (charB === undefined) {
                return 1;
            }

            if (charA === charB) {
                continue;
            }

            const groupA = getCharacterGroup(charA);
            const groupB = getCharacterGroup(charB);

            if (groupA !== groupB) {
                return groupA - groupB;
            }

            if (charA < charB) {
                return -1;
            }

            if (charA > charB) {
                return 1;
            }
        }

        return 0;
    }

    function updateSortIndicators() {
        sortButtons.forEach(button => {
            const indicator = button.querySelector(".sort-indicator");

            if (!indicator) {
                return;
            }

            if (button.dataset.sort !== currentSortColumn) {
                indicator.textContent = testResultsList.dataset.symbolSortNeutral;
                return;
            }

            indicator.textContent = currentSortDirection === "asc" ? testResultsList.dataset.symbolSortAscending : testResultsList.dataset.symbolSortDescending;
        });
    }

    function sortRows(column) {
        if (currentSortColumn === column) {
            currentSortDirection = currentSortDirection === "asc" ? "desc" : "asc";
        }
        else {
            currentSortColumn = column;
            currentSortDirection = "asc";
        }

        const rows = Array.from(testResultsList.querySelectorAll(".data-row"));

        rows.sort((a, b) => {
            let comparison = 0;

            if (column === "datetime") {
                comparison = Number(a.dataset.datetime) - Number(b.dataset.datetime);
            }
            else if (column === "partnumber") {
                comparison = comparePartNumbers(a.dataset.partnumber, b.dataset.partnumber);
            }
            else {
                const valueA = a.dataset[column] ?? "";
                const valueB = b.dataset[column] ?? "";

                comparison = valueA.localeCompare(valueB, undefined, {
                    numeric: true,
                    sensitivity: "base"
                });
            }

            return currentSortDirection === "asc" ? comparison : -comparison;
        });

        rows.forEach(row => {
            testResultsList.appendChild(row);
        });

        if (resetSortButton) {
            resetSortButton.disabled = false;
        }

        updateSortIndicators();
        updateSelection();
    }

    function resetSorting() {
        originalRows.forEach(row => {
            testResultsList.appendChild(row);
        });

        currentSortColumn = null;
        currentSortDirection = "asc";

        if (resetSortButton) {
            resetSortButton.disabled = true;
        }

        updateSortIndicators();
        updateSelection();
    }

    checkboxes.forEach(checkbox => {
        checkbox.addEventListener("change", function () {
            const partNumber = this.dataset.partnumber;

            if (this.checked) {
                const selected = getCheckboxesInCurrentOrder().filter(checkbox => checkbox.checked);
                const differentPartNumber = selected.some(
                    checkbox => checkbox.dataset.partnumber !== partNumber
                );

                if (differentPartNumber) {
                    window.alert(differentPartNumberMessage);
                    this.checked = false;
                }
            }

            updateSelection();
        });
    });

    selectAllSameButtons.forEach(button => {
        button.addEventListener("click", function () {
            const partNumber = this.dataset.partnumber;

            getCheckboxesInCurrentOrder().forEach(checkbox => {
                if (checkbox.dataset.partnumber === partNumber) {
                    checkbox.checked = true;
                }
            });

            updateSelection();
        });
    });

    selectTimeGroupButtons.forEach(button => {
        button.addEventListener("click", function () {
            const partNumber = this.dataset.partnumber;
            const timeGroup = this.dataset.timegroup;

            getCheckboxesInCurrentOrder().forEach(checkbox => {
                if (
                    checkbox.dataset.partnumber === partNumber &&
                    checkbox.dataset.timegroup === timeGroup
                ) {
                    checkbox.checked = true;
                }
            });

            updateSelection();
        });
    });

    filterToggleButton?.addEventListener("click", function () {
        const isOpen = filterPanel?.classList.toggle("is-open") ?? false;

        filterToggleButton.setAttribute("aria-expanded", isOpen.toString());

        if (filterToggleIndicator) {
            filterToggleIndicator.textContent = isOpen ? filterToggleButton.dataset.symbolExpanded : filterToggleButton.dataset.symbolCollapsed;
        }
    });

    sortButtons.forEach(button => {
        button.addEventListener("click", function () {
            sortRows(this.dataset.sort);
        });
    });

    resetSortButton?.addEventListener("click", resetSorting);

    clearSelectedButton?.addEventListener("click", function () {
        getCheckboxesInCurrentOrder().forEach(checkbox => {
            checkbox.checked = false;
        });

        updateSelection();
    });

    updateSelection();
    updateSortIndicators();
    updateFilterButtons();

    if (resetSortButton) {
        resetSortButton.disabled = true;
    }

    window.addEventListener("pageshow", function () {
        updateSelection();
        updateSortIndicators();

        if (resetSortButton) {
            resetSortButton.disabled = currentSortColumn === null;
        }
    });
});
