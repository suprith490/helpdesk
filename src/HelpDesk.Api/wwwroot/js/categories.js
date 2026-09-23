(function () {
    if (!UI.initLayout()) {
        return;
    }

    const admin = Auth.isAdmin();
    const tbody = document.getElementById("categoriesBody");
    const modal = document.getElementById("categoryModal");
    const modalTitle = document.getElementById("modalTitle");
    const modalError = document.getElementById("modalError");
    const form = document.getElementById("categoryForm");
    const nameInput = document.getElementById("categoryName");
    const descriptionInput = document.getElementById("categoryDescription");
    const activeInput = document.getElementById("categoryActive");

    let editingId = null;

    function renderRows(categories) {
        if (!categories || categories.length === 0) {
            tbody.innerHTML = '<tr><td colspan="5"><div class="empty-state">No categories found.</div></td></tr>';
            return;
        }
        tbody.innerHTML = categories.map(function (category) {
            const statusBadge = category.isActive
                ? '<span class="badge badge-resolved">Active</span>'
                : '<span class="badge badge-inactive">Inactive</span>';
            const actions = admin
                ? '<button class="btn btn-secondary btn-sm" data-edit="' + category.id + '" type="button">Edit</button>'
                : '<span class="text-muted">-</span>';
            return "<tr>" +
                "<td><strong>" + UI.escapeHtml(category.name) + "</strong></td>" +
                "<td>" + UI.escapeHtml(category.description || "-") + "</td>" +
                "<td>" + category.ticketCount + "</td>" +
                "<td>" + statusBadge + "</td>" +
                '<td class="actions">' + actions + "</td>" +
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
        tbody.innerHTML = '<tr><td colspan="5"><div class="spinner"></div></td></tr>';
        try {
            cache = await API.get("/categories?includeInactive=true");
            renderRows(cache);
        } catch (error) {
            tbody.innerHTML = '<tr><td colspan="5"><div class="empty-state">' + UI.escapeHtml(error.message) + "</div></td></tr>";
        }
    }

    function openModal() {
        modal.classList.remove("hidden");
        modalError.classList.add("hidden");
    }

    function closeModal() {
        modal.classList.add("hidden");
        editingId = null;
    }

    function openCreate() {
        editingId = null;
        modalTitle.textContent = "New category";
        nameInput.value = "";
        descriptionInput.value = "";
        activeInput.checked = true;
        openModal();
    }

    function openEdit(id) {
        const category = cache.find(function (item) { return item.id === id; });
        if (!category) {
            return;
        }
        editingId = id;
        modalTitle.textContent = "Edit category";
        nameInput.value = category.name;
        descriptionInput.value = category.description || "";
        activeInput.checked = category.isActive;
        openModal();
    }

    form.addEventListener("submit", async function (event) {
        event.preventDefault();
        modalError.classList.add("hidden");

        const payload = {
            name: nameInput.value.trim(),
            description: descriptionInput.value.trim(),
            isActive: activeInput.checked
        };

        try {
            if (editingId) {
                await API.put("/categories/" + editingId, payload);
                UI.toast("Category updated", "success");
            } else {
                await API.post("/categories", { name: payload.name, description: payload.description });
                UI.toast("Category created", "success");
            }
            closeModal();
            load();
        } catch (error) {
            modalError.textContent = error.message;
            modalError.classList.remove("hidden");
        }
    });

    document.getElementById("modalCloseButton").addEventListener("click", closeModal);
    document.getElementById("modalCancelButton").addEventListener("click", closeModal);

    if (admin) {
        document.getElementById("newCategoryButton").classList.remove("hidden");
        document.getElementById("newCategoryButton").addEventListener("click", openCreate);
    }

    load();
})();
