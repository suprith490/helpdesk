(function (global) {
    const STATUS_LABELS = {
        New: "New",
        Assigned: "Assigned",
        InProgress: "In Progress",
        Resolved: "Resolved",
        Closed: "Closed"
    };

    const ROLE_LABELS = {
        Employee: "Employee",
        SupportAgent: "Support Agent",
        Admin: "Administrator"
    };

    const PRIORITY_LABELS = {
        Low: "Low",
        Medium: "Medium",
        High: "High",
        Critical: "Critical"
    };

    function escapeHtml(value) {
        if (value === null || value === undefined) {
            return "";
        }
        return String(value)
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#39;");
    }

    function formatDateTime(value) {
        if (!value) {
            return "-";
        }
        const date = new Date(value);
        if (isNaN(date.getTime())) {
            return "-";
        }
        return date.toLocaleString();
    }

    function formatDate(value) {
        if (!value) {
            return "-";
        }
        const date = new Date(value);
        if (isNaN(date.getTime())) {
            return "-";
        }
        return date.toLocaleDateString();
    }

    function initials(name) {
        if (!name) {
            return "?";
        }
        const parts = String(name).trim().split(/\s+/);
        if (parts.length === 1) {
            return parts[0].substring(0, 2).toUpperCase();
        }
        return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
    }

    function statusLabel(status) {
        return STATUS_LABELS[status] || status || "-";
    }

    function statusBadge(status) {
        const key = String(status || "").toLowerCase();
        return '<span class="badge badge-' + key + '">' + escapeHtml(statusLabel(status)) + "</span>";
    }

    function priorityBadge(priority) {
        const key = String(priority || "").toLowerCase();
        return '<span class="badge badge-' + key + '">' + escapeHtml(PRIORITY_LABELS[priority] || priority) + "</span>";
    }

    function roleLabel(role) {
        return ROLE_LABELS[role] || role || "-";
    }

    function roleBadge(role) {
        return '<span class="badge badge-role">' + escapeHtml(roleLabel(role)) + "</span>";
    }

    function toast(message, type) {
        let container = document.getElementById("toastContainer");
        if (!container) {
            container = document.createElement("div");
            container.id = "toastContainer";
            container.className = "toast-container";
            document.body.appendChild(container);
        }
        const toastElement = document.createElement("div");
        toastElement.className = "toast " + (type || "info");
        toastElement.textContent = message;
        container.appendChild(toastElement);
        setTimeout(function () {
            toastElement.remove();
        }, 3500);
    }

    function toggleSidebar() {
        const sidebar = document.getElementById("sidebar");
        if (sidebar) {
            sidebar.classList.toggle("open");
        }
    }

    function navItems(role) {
        const items = [
            { href: "dashboard.html", label: "Dashboard" },
            { href: "tickets.html", label: "Tickets" },
            { href: "ticket-create.html", label: "New Ticket" },
            { href: "categories.html", label: "Categories" }
        ];
        if (role === "Admin") {
            items.push({ href: "users.html", label: "Users" });
        }
        items.push({ href: "profile.html", label: "Profile" });
        return items;
    }

    function renderSidebar() {
        const sidebar = document.getElementById("sidebar");
        if (!sidebar) {
            return;
        }
        const user = Auth.getUser();
        const role = user ? user.role : "Employee";
        const current = location.pathname.split("/").pop() || "dashboard.html";
        const links = navItems(role).map(function (item) {
            const active = item.href === current ? " active" : "";
            return '<a class="' + active.trim() + '" href="' + item.href + '">' + item.label + "</a>";
        }).join("");

        sidebar.innerHTML =
            '<div class="brand"><span class="brand-badge">HD</span><span>HelpDesk</span></div>' +
            "<nav>" + links + "</nav>" +
            '<div class="sidebar-footer">HelpDesk v1.0 - IT Support</div>';
    }

    function renderUserArea() {
        const area = document.getElementById("userArea");
        if (!area) {
            return;
        }
        const user = Auth.getUser();
        if (!user) {
            return;
        }
        const fullName = user.fullName || (user.firstName + " " + user.lastName);
        area.innerHTML =
            '<div class="user-meta"><strong>' + escapeHtml(fullName) + "</strong><span>" + escapeHtml(roleLabel(user.role)) + "</span></div>" +
            '<div class="avatar">' + escapeHtml(initials(fullName)) + "</div>" +
            '<button class="btn btn-secondary btn-sm" id="logoutButton">Sign out</button>';
        document.getElementById("logoutButton").addEventListener("click", Auth.logout);
    }

    function setPageTitle() {
        const title = document.body.getAttribute("data-title");
        const target = document.querySelector("[data-page-title]");
        if (title && target) {
            target.textContent = title;
        }
    }

    function initLayout() {
        if (!Auth.isLoggedIn()) {
            location.href = "login.html";
            return false;
        }
        renderSidebar();
        renderUserArea();
        setPageTitle();
        return true;
    }

    global.UI = {
        escapeHtml: escapeHtml,
        formatDateTime: formatDateTime,
        formatDate: formatDate,
        initials: initials,
        statusLabel: statusLabel,
        statusBadge: statusBadge,
        priorityBadge: priorityBadge,
        roleLabel: roleLabel,
        roleBadge: roleBadge,
        toast: toast,
        toggleSidebar: toggleSidebar,
        initLayout: initLayout
    };
})(window);
