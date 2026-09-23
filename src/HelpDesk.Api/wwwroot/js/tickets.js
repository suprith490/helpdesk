(function () {
    if (!UI.initLayout()) {
        return;
    }

    const state = {
        page: 1,
        pageSize: 10,
        search: "",
        status: "",
        priority: "",
        categoryId: "",
        assignedToMe: false,
        sortBy: "createdAt",
        sortDirection: "desc"
    };

    let totalPages = 1;

    const form = document.getElementById("filterForm");
    const tbody = document.getElementById("ticketsBody");
    const pageInfo = document.getElementById("pageInfo");
    const prevButton = document.getElementById("prevButton");
    const nextButton = document.getElementById("nextButton");
    const pageSizeSelect = document.getElementById("pageSize");
    const assignedToMeGroup = document.getElementById("assignedToMeGroup");

    if (Auth.isAgent()) {
        assignedToMeGroup.style.display = "";
    }

    function buildQuery() {
        const params = new URLSearchParams();
        if (state.search) {
            params.set("search", state.search);
        }
        if (state.status) {
            params.set("status", state.status);
        }
        if (state.priority) {
            params.set("priority", state.priority);
        }
        if (state.categoryId) {
            params.set("categoryId", state.categoryId);
        }
        if (state.assignedToMe) {
            params.set("assignedToMe", "true");
        }
        params.set("sortBy", state.sortBy);
        params.set("sortDirection", state.sortDirection);
        params.set("page", state.page);
        params.set("pageSize", state.pageSize);
        return params.toString();
    }

    function renderRows(items) {
        if (!items || items.length === 0) {
            tbody.innerHTML = '<tr><td colspan="8"><div class="empty-state">No tickets match your filters.</div></td></tr>';
            return;
        }
        tbody.innerHTML = items.map(function (ticket) {
            return "<tr onclick=\"location.href='ticket-details.html?id=" + ticket.id + "'\">" +
                "<td><strong>" + UI.escapeHtml(ticket.ticketNumber) + "</strong></td>" +
                "<td>" + UI.escapeHtml(ticket.title) + "</td>" +
                "<td>" + UI.escapeHtml(ticket.categoryName) + "</td>" +
                "<td>" + UI.priorityBadge(ticket.priority) + "</td>" +
                "<td>" + UI.statusBadge(ticket.status) + "</td>" +
                "<td>" + UI.escapeHtml(ticket.assignedToName || "Unassigned") + "</td>" +
                "<td>" + UI.formatDate(ticket.createdAt) + "</td>" +
                "<td>" + UI.formatDateTime(ticket.updatedAt) + "</td>" +
                "</tr>";
        }).join("");
    }

    function renderPagination(result) {
        totalPages = result.totalPages || 1;
        pageInfo.textContent = "Page " + result.page + " of " + totalPages + " - " + result.totalCount + " ticket(s)";
        prevButton.disabled = !result.hasPrevious;
        nextButton.disabled = !result.hasNext;
    }

    async function load() {
        tbody.innerHTML = '<tr><td colspan="8"><div class="spinner"></div></td></tr>';
        try {
            const result = await API.get("/tickets?" + buildQuery());
            renderRows(result.items);
            renderPagination(result);
            markSortHeaders();
        } catch (error) {
            tbody.innerHTML = '<tr><td colspan="8"><div class="empty-state">' + UI.escapeHtml(error.message) + "</div></td></tr>";
        }
    }

    function markSortHeaders() {
        document.querySelectorAll("th[data-sort]").forEach(function (header) {
            const base = header.textContent.replace(/[ ]*[\u25B2\u25BC]$/, "");
            if (header.getAttribute("data-sort") === state.sortBy) {
                header.textContent = base + (state.sortDirection === "asc" ? " \u25B2" : " \u25BC");
            } else {
                header.textContent = base;
            }
        });
    }

    async function loadCategories() {
        try {
            const categories = await API.get("/categories");
            const select = document.getElementById("categoryId");
            categories.forEach(function (category) {
                const option = document.createElement("option");
                option.value = category.id;
                option.textContent = category.name;
                select.appendChild(option);
            });
        } catch (error) {
            UI.toast("Could not load categories", "error");
        }
    }

    form.addEventListener("submit", function (event) {
        event.preventDefault();
        state.search = document.getElementById("search").value.trim();
        state.status = document.getElementById("status").value;
        state.priority = document.getElementById("priority").value;
        state.categoryId = document.getElementById("categoryId").value;
        state.assignedToMe = document.getElementById("assignedToMe").checked;
        state.page = 1;
        load();
    });

    document.getElementById("resetButton").addEventListener("click", function () {
        form.reset();
        state.search = "";
        state.status = "";
        state.priority = "";
        state.categoryId = "";
        state.assignedToMe = false;
        state.page = 1;
        state.sortBy = "createdAt";
        state.sortDirection = "desc";
        load();
    });

    document.querySelectorAll("th[data-sort]").forEach(function (header) {
        header.addEventListener("click", function () {
            const field = header.getAttribute("data-sort");
            if (state.sortBy === field) {
                state.sortDirection = state.sortDirection === "asc" ? "desc" : "asc";
            } else {
                state.sortBy = field;
                state.sortDirection = "asc";
            }
            state.page = 1;
            load();
        });
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

    pageSizeSelect.addEventListener("change", function () {
        state.pageSize = Number(pageSizeSelect.value);
        state.page = 1;
        load();
    });

    loadCategories();
    load();
})();
