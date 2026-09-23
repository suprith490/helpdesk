(function () {
    if (!UI.initLayout()) {
        return;
    }

    const profileForm = document.getElementById("profileForm");
    const profileError = document.getElementById("profileError");
    const passwordForm = document.getElementById("passwordForm");
    const passwordError = document.getElementById("passwordError");
    const passwordSuccess = document.getElementById("passwordSuccess");

    async function loadProfile() {
        try {
            const [user, departments] = await Promise.all([
                API.get("/users/me"),
                API.get("/departments")
            ]);

            document.getElementById("firstName").value = user.firstName;
            document.getElementById("lastName").value = user.lastName;
            document.getElementById("email").value = user.email;
            document.getElementById("roleDisplay").innerHTML = UI.roleBadge(user.role);

            const select = document.getElementById("departmentId");
            departments.forEach(function (department) {
                const option = document.createElement("option");
                option.value = department.id;
                option.textContent = department.name;
                select.appendChild(option);
            });
            select.value = user.departmentId || "";
        } catch (error) {
            UI.toast("Could not load profile: " + error.message, "error");
        }
    }

    profileForm.addEventListener("submit", async function (event) {
        event.preventDefault();
        profileError.classList.add("hidden");
        const departmentValue = document.getElementById("departmentId").value;

        try {
            const updated = await API.put("/users/me", {
                firstName: document.getElementById("firstName").value.trim(),
                lastName: document.getElementById("lastName").value.trim(),
                email: document.getElementById("email").value.trim(),
                departmentId: departmentValue ? Number(departmentValue) : null
            });

            const stored = Auth.getUser();
            stored.firstName = updated.firstName;
            stored.lastName = updated.lastName;
            stored.email = updated.email;
            stored.fullName = updated.fullName;
            stored.departmentId = updated.departmentId;
            stored.departmentName = updated.departmentName;
            localStorage.setItem(API.userKey, JSON.stringify(stored));

            UI.initLayout();
            UI.toast("Profile updated", "success");
        } catch (error) {
            profileError.textContent = error.message;
            profileError.classList.remove("hidden");
        }
    });

    passwordForm.addEventListener("submit", async function (event) {
        event.preventDefault();
        passwordError.classList.add("hidden");
        passwordSuccess.classList.add("hidden");

        try {
            await API.post("/users/me/change-password", {
                currentPassword: document.getElementById("currentPassword").value,
                newPassword: document.getElementById("newPassword").value
            });
            passwordForm.reset();
            passwordSuccess.textContent = "Password updated successfully.";
            passwordSuccess.classList.remove("hidden");
            UI.toast("Password updated", "success");
        } catch (error) {
            passwordError.textContent = error.message;
            passwordError.classList.remove("hidden");
        }
    });

    loadProfile();
})();
