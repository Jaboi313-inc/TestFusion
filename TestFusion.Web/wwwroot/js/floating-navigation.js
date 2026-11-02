document.addEventListener(
    "DOMContentLoaded",
    function () {
        const backToLeft =
            document.getElementById(
                "backToLeftToast");

        const backToTop =
            document.getElementById(
                "backToTopToast");

        const backToStart =
            document.getElementById(
                "backToStartToast");

        function updateFloatingNavigation() {
            const horizontallyScrolled =
                window.scrollX > 100;

            const verticallyScrolled =
                window.scrollY > 250;

            backToLeft?.classList.toggle(
                "show",
                horizontallyScrolled);

            backToTop?.classList.toggle(
                "show",
                verticallyScrolled);

            backToStart?.classList.toggle(
                "show",
                horizontallyScrolled ||
                verticallyScrolled);
        }

        backToLeft?.addEventListener(
            "click",
            function () {
                window.scrollTo({
                    left: 0,
                    top: window.scrollY,
                    behavior: "smooth"
                });
            });

        backToTop?.addEventListener(
            "click",
            function () {
                window.scrollTo({
                    left: window.scrollX,
                    top: 0,
                    behavior: "smooth"
                });
            });

        backToStart?.addEventListener(
            "click",
            function () {
                window.scrollTo({
                    left: 0,
                    top: 0,
                    behavior: "smooth"
                });
            });

        window.addEventListener(
            "scroll",
            updateFloatingNavigation,
            { passive: true });

        window.addEventListener(
            "resize",
            updateFloatingNavigation);

        updateFloatingNavigation();
    });