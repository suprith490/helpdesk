(function () {
    if (Auth.isLoggedIn()) {
        location.href = "dashboard.html";
        return;
    }

    const params = new URLSearchParams(location.search);
    if (params.get("expired") === "1") {
        showError("Your session expired. Please sign in again.");
    }

    const form = document.getElementById("loginForm");
    const button = document.getElementById("loginButton");
    const errorBox = document.getElementById("formError");

    function showError(message) {
        errorBox.textContent = message;
        errorBox.classList.remove("hidden");
    }

    function hideError() {
        errorBox.classList.add("hidden");
    }

    form.addEventListener("submit", async function (event) {
        event.preventDefault();
        hideError();
        button.disabled = true;
        button.textContent = "Signing in...";

        try {
            const response = await API.post("/auth/login", {
                email: document.getElementById("email").value.trim(),
                password: document.getElementById("password").value
            });
            Auth.setSession(response);
            location.href = "dashboard.html";
        } catch (error) {
            showError(error.message);
            button.disabled = false;
            button.textContent = "Sign in";
        }
    });
})();
