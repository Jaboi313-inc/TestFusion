document.addEventListener("DOMContentLoaded", function () {
    const form = document.querySelector(".settings-form");
    const saveButton = document.getElementById("settings-save");
    const navigationLinks = Array.from(document.querySelectorAll(".settings-navigation a"));
    const sections = Array.from(document.querySelectorAll(".settings-section, .settings-subsection[id]"));
    const messages = document.querySelectorAll(".settings-message-temporary");

    let isNavigationScrolling = false;
    let navigationScrollTimeout = null;
    let clickedSectionId = null;

    if (form && saveButton) {
        const getFormState = () => new URLSearchParams(new FormData(form)).toString();
        const initialState = getFormState();

        function updateSaveButton() {
            saveButton.disabled = getFormState() === initialState;
        }

        form.addEventListener("input", updateSaveButton);
        form.addEventListener("change", updateSaveButton);
    }

    messages.forEach(function (message) {
        const closeButton = message.querySelector(".settings-message-close");
        const duration = parseInt(message.dataset.duration || "5000", 10);
        let hideTimeout;

        message.style.setProperty("--message-duration", duration + "ms");

        function hideMessage() {
            window.clearTimeout(hideTimeout);
            message.classList.add("is-hiding");

            window.setTimeout(function () {
                message.remove();
            }, 300);
        }

        requestAnimationFrame(function () {
            message.classList.add("is-counting");
        });

        hideTimeout = window.setTimeout(hideMessage, duration);

        if (closeButton) {
            closeButton.addEventListener("click", hideMessage);
        }
    });

    function getNavbarHeight() {
        const value = getComputedStyle(document.documentElement).getPropertyValue("--navbar-height");
        const height = parseFloat(value);
        return Number.isNaN(height) ? 70 : height;
    }

    function setActiveNavigation(sectionId) {
        navigationLinks.forEach(function (link) {
            link.classList.toggle("active", link.getAttribute("href") === "#" + sectionId);
        });
    }

    function getCurrentSection() {
        const activationLine = getNavbarHeight() + 40;
        let activeSection = sections[0];

        sections.forEach(function (section) {
            if (section.getBoundingClientRect().top <= activationLine) {
                activeSection = section;
            }
        });

        const scrollBottom = window.scrollY + window.innerHeight;
        const pageBottom = document.documentElement.scrollHeight;

        if (scrollBottom >= pageBottom - 5) {
            activeSection = sections[sections.length - 1];
        }

        return activeSection;
    }

    function updateActiveNavigation() {
        if (sections.length === 0) {
            return;
        }

        if (isNavigationScrolling) {
            if (clickedSectionId) {
                setActiveNavigation(clickedSectionId);
            }

            window.clearTimeout(navigationScrollTimeout);
            navigationScrollTimeout = window.setTimeout(function () {
                isNavigationScrolling = false;

                if (clickedSectionId) {
                    setActiveNavigation(clickedSectionId);
                }
            }, 150);

            return;
        }

        const activeSection = getCurrentSection();

        if (activeSection) {
            setActiveNavigation(activeSection.id);
        }
    }

    navigationLinks.forEach(function (link) {
        link.addEventListener("click", function (event) {
            event.preventDefault();

            const href = link.getAttribute("href");

            if (!href) {
                return;
            }

            const section = document.getElementById(href.substring(1));

            if (!section) {
                return;
            }

            clickedSectionId = section.id;
            isNavigationScrolling = true;
            window.clearTimeout(navigationScrollTimeout);
            setActiveNavigation(clickedSectionId);

            section.scrollIntoView({
                behavior: "smooth",
                block: "start"
            });
        });
    });

    window.addEventListener("scroll", updateActiveNavigation, { passive: true });

    window.addEventListener("resize", function () {
        if (!isNavigationScrolling) {
            updateActiveNavigation();
        }
    });

    updateActiveNavigation();
});