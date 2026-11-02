document.addEventListener("click", function (event) {
    const confirmElement = event.target.closest("[data-confirm]:not(form)");

    if (confirmElement) {
        const message = confirmElement.dataset.confirm;

        if (message && !window.confirm(message)) {
            event.preventDefault();
            return;
        }
    }

    const smoothScrollLink = event.target.closest('a[data-smooth-scroll][href^="#"]');

    if (!smoothScrollLink) {
        return;
    }

    const href = smoothScrollLink.getAttribute("href");

    if (!href || href === "#") {
        return;
    }

    const target = document.querySelector(href);

    if (!target) {
        return;
    }

    event.preventDefault();

    target.scrollIntoView({
        behavior: "smooth",
        block: "start"
    });
});

document.addEventListener("submit", function (event) {
    const form = event.target.closest("form[data-confirm]");

    if (!form) {
        return;
    }

    const message = form.dataset.confirm;

    if (message && !window.confirm(message)) {
        event.preventDefault();
    }
});
