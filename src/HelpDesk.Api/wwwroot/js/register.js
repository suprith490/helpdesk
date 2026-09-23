(function () {
    if (Auth.isLoggedIn()) {
        location.href = "dashboard.html";
        return;
    }

    const form = document.getElementById("registerForm");
    const button = document.getElementById("registerButton");
    const errorBox = document.getElementById("formError");
    const departmentSelect = document.getElementById("departmentId");

    function showError(message) {
        errorBox.textContent = message;
        errorBox.classList.remove("hidden");
    }

    async function loadDepartments() {
        try {
            const departments = await API.get("/departments");
            departments.forEach(function (department) {
                const option = document.createElement("option");
                option.value = department.id;
                option.textContent = department.name;
                departmentSelect.appendChild(option);
            });
        } catch (error) {
            showError("Could not load departments: " + error.message);
        }
    }

    form.addEventListener("submit", async function (event) {
        event.preventDefault();
        errorBox.classList.add("hidden");
        button.disabled = true;
        button.textContent = "Creating account...";

        const departmentValue = departmentSelect.value;

        try {
            const response = await API.post("/auth/register", {
                firstName: document.getElementById("firstName").value.trim(),
                lastName: document.getElementById("lastName").value.trim(),
                email: document.getElementById("email").value.trim(),
                password: document.getElementById("password").value,
                departmentId: departmentValue ? Number(departmentValue) : null
            });
            Auth.setSession(response);
            location.href = "dashboard.html";
        } catch (error) {
            showError(error.message);
            button.disabled = false;
            button.textContent = "Create account";
        }
    });

    loadDepartments();
})();
