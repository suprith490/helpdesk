(function () {
    if (!Auth.requireAuth() || !Auth.requireRole(["Admin"])) {
        return;
    }
    if (!UI.initLayout()) {
        return;
    }

    const state = {
        page: 1,
        pageSize: 10,
        search: "",
        role: "",
        isActive: ""
    };

    let totalPages = 1;
    let departments = [];
    let editingId = null;

    const form = document.getElementById("filterForm");
    const tbody = document.getElementById("usersBody");
    const pageInfo = document.getElementById("pageInfo");
    const prevButton = document.getElementById("prevButton");
    const nextButton = document.getElementById("nextButton");
    const modal = document.getElementById("userModal");
    const modalError = document.getElementById("modalError");
    const userForm = document.getElementById("userForm");

    function buildQuery() {
        const params = new URLSearchParams();
        if (state.search) {
            params.set("search", state.search);
        }
        if (state.role) {
            params.set("role", state.role);
        }
        if (state.isActive !== "") {
            params.set("isActive", state.isActive);
        }
        params.set("page", state.page);
        params.set("pageSize", state.pageSize);
        return params.toString();
    }

    function renderRows(users) {
        if (!users || users.length === 0) {
            tbody.innerHTML = '<tr><td colspan="7"><div class="empty-state">No users found.</div></td></tr>';
            return;
        }
        tbody.innerHTML = users.map(function (user) {
            const status = user.isActive
                ? '<span class="badge badge-resolved">Active</span>'
                : '<span class="badge badge-inactive">Inactive</span>';
            return "<tr>" +
                "<td><strong>" + UI.escapeHtml(user.firstName + " " + user.lastName) + "</strong></td>" +
                "<td>" + UI.escapeHtml(user.email) + "</td>" +
                "<td>" + UI.roleBadge(user.role) + "</td>" +
                "<td>" + UI.escapeHtml(user.departmentName || "-") + "</td>" +
                "<td>" + status + "</td>" +
                "<td>" + UI.formatDate(user.createdAt) + "</td>" +
                '<td class="actions"><button class="btn btn-secondary btn-sm" data-edit="' + user.id + '" type="button">Edit</button></td>' +
                "</tr>";
        }).join("");

        tbody.querySelectorAll("button[data-edit]").forEach(function (button) {
            button.addEventListener("click", function () {
                openEdit(Number(button.getAttribute("data-edit")));
            });
        });
    }

    let cache = [];

    async function load() {
        tbody.innerHTML = '<tr><td colspan="7"><div class="spinner"></div></td></tr>';
        try {
            const result = await API.get("/users?" + buildQuery());
            cache = result.items;
            renderRows(result.items);
            totalPages = result.totalPages || 1;
            pageInfo.textContent = "Page " + result.page + " of " + totalPages + " - " + result.totalCount + " user(s)";
            prevButton.disabled = !result.hasPrevious;
            nextButton.disabled = !result.hasNext;
        } catch (error) {
            tbody.innerHTML = '<tr><td colspan="7"><div class="empty-state">' + UI.escapeHtml(error.message) + "</div></td></tr>";
        }
    }

    function fillDepartments() {
        const select = document.getElementById("userDepartment");
        select.innerHTML = '<option value="">None</option>';
        departments.forEach(function (department) {
            const option = document.createElement("option");
            option.value = department.id;
            option.textContent = department.name;
            select.appendChild(option);
        });
    }

    function openEdit(id) {
        const user = cache.find(function (item) { return item.id === id; });
        if (!user) {
            return;
        }
        editingId = id;
        document.getElementById("userIdentity").textContent =
            user.firstName + " " + user.lastName + " - " + user.email;
        document.getElementById("userRole").value = user.role;
        document.getElementById("userDepartment").value = user.departmentId || "";
        document.getElementById("userActive").checked = user.isActive;
        modalError.classList.add("hidden");
        modal.classList.remove("hidden");
    }

    function closeModal() {
        modal.classList.add("hidden");
        editingId = null;
    }

    userForm.addEventListener("submit", async function (event) {
        event.preventDefault();
        if (!editingId) {
            return;
        }
        modalError.classList.add("hidden");
        const departmentValue = document.getElementById("userDepartment").value;
        try {
            await API.put("/users/" + editingId, {
                role: document.getElementById("userRole").value,
                departmentId: departmentValue ? Number(departmentValue) : null,
                isActive: document.getElementById("userActive").checked
            });
            UI.toast("User updated", "success");
            closeModal();
            load();
        } catch (error) {
            modalError.textContent = error.message;
            modalError.classList.remove("hidden");
        }
    });

    document.getElementById("modalCloseButton").addEventListener("click", closeModal);
    document.getElementById("modalCancelButton").addEventListener("click", closeModal);

    form.addEventListener("submit", function (event) {
        event.preventDefault();
        state.search = document.getElementById("search").value.trim();
        state.role = document.getElementById("role").value;
        state.isActive = document.getElementById("active").value;
        state.page = 1;
        load();
    });

    document.getElementById("resetButton").addEventListener("click", function () {
        form.reset();
        state.search = "";
        state.role = "";
        state.isActive = "";
        state.page = 1;
        load();
    });

    prevButton.addEventListener("click", function () {
        if (state.page > 1) {
            state.page -= 1;
            load();
        }
    });

    nextButton.addEventListener("click", function () {
        if (state.page < totalPages) {
            state.page += 1;
            load();
        }
    });

    async function init() {
        try {
            departments = await API.get("/departments");
            fillDepartments();
        } catch (error) {
            UI.toast("Could not load departments", "error");
        }
        load();
    }

    init();
})();
