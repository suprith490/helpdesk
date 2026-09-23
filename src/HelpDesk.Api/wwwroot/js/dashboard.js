(function () {
    if (!UI.initLayout()) {
        return;
    }

    const user = Auth.getUser();
    const role = user.role;

    function statCard(label, value, accent) {
        return '<div class="stat-card accent-' + accent + '">' +
            '<div class="label">' + UI.escapeHtml(label) + "</div>" +
            '<div class="value">' + value + "</div>" +
            "</div>";
    }

    function renderBars(containerId, rows, colorClass) {
        const container = document.getElementById(containerId);
        if (!rows || rows.length === 0) {
            container.innerHTML = '<div class="empty-state">No data yet.</div>';
            return;
        }
        const max = Math.max.apply(null, rows.map(function (row) { return row.count; })) || 1;
        container.innerHTML = rows.map(function (row) {
            const width = Math.round((row.count / max) * 100);
            return '<div class="bar-row">' +
                '<span>' + UI.escapeHtml(row.label) + "</span>" +
                '<span class="bar-track"><span class="bar-fill ' + (row.color || colorClass || "") + '" style="width:' + width + '%"></span></span>' +
                '<strong>' + row.count + "</strong>" +
                "</div>";
        }).join("");
    }

    function buildStatCards(data) {
        const tickets = data.tickets;
        const cards = [];

        if (role === "Employee") {
            cards.push(statCard("My tickets", tickets.createdByMe, "primary"));
            cards.push(statCard("Open", tickets.new + tickets.assigned + tickets.inProgress, "warning"));
            cards.push(statCard("Resolved", tickets.resolved, "success"));
            cards.push(statCard("Closed", tickets.closed, "info"));
        } else if (role === "SupportAgent") {
            cards.push(statCard("Assigned to me", tickets.assignedToMe, "primary"));
            cards.push(statCard("Unassigned", tickets.unassigned, "danger"));
            cards.push(statCard("In progress", tickets.inProgress, "warning"));
            cards.push(statCard("Resolved", tickets.resolved, "success"));
            cards.push(statCard("Total tickets", tickets.total, "info"));
        } else {
            cards.push(statCard("Total tickets", tickets.total, "primary"));
            cards.push(statCard("New", tickets.new, "info"));
            cards.push(statCard("In progress", tickets.inProgress, "warning"));
            cards.push(statCard("Resolved", tickets.resolved, "success"));
            cards.push(statCard("Unassigned", tickets.unassigned, "danger"));
            if (data.users) {
                cards.push(statCard("Users", data.users.total, "primary"));
                cards.push(statCard("Support agents", data.users.supportAgents, "info"));
            }
        }

        document.getElementById("statCards").innerHTML = cards.join("");
    }

    function statusColor(status) {
        return {
            New: "",
            Assigned: "warning",
            InProgress: "info",
            Resolved: "success",
            Closed: ""
        }[status] || "";
    }

    function renderRecent(data) {
        const tbody = document.getElementById("recentTickets");
        if (!data.recentTickets || data.recentTickets.length === 0) {
            tbody.innerHTML = '<tr><td colspan="6"><div class="empty-state">No tickets yet.</div></td></tr>';
            return;
        }
        tbody.innerHTML = data.recentTickets.map(function (ticket) {
            return "<tr onclick=\"location.href='ticket-details.html?id=" + ticket.id + "'\">" +
                "<td><strong>" + UI.escapeHtml(ticket.ticketNumber) + "</strong></td>" +
                "<td>" + UI.escapeHtml(ticket.title) + "</td>" +
                "<td>" + UI.escapeHtml(ticket.categoryName) + "</td>" +
                "<td>" + UI.priorityBadge(ticket.priority) + "</td>" +
                "<td>" + UI.statusBadge(ticket.status) + "</td>" +
                "<td>" + UI.formatDateTime(ticket.updatedAt) + "</td>" +
                "</tr>";
        }).join("");
    }

    async function load() {
        try {
            const data = await API.get("/dashboard");
            buildStatCards(data);
            renderBars("statusBars", data.byStatus.map(function (item) {
                return { label: UI.statusLabel(item.status), count: item.count, color: statusColor(item.status) };
            }));
            renderBars("priorityBars", data.byPriority.map(function (item) {
                const color = { Low: "success", Medium: "info", High: "warning", Critical: "danger" }[item.priority] || "";
                return { label: item.priority, count: item.count, color: color };
            }));
            renderBars("categoryBars", data.byCategory.map(function (item) {
                return { label: item.categoryName, count: item.count };
            }));
            renderRecent(data);

            document.getElementById("loading").classList.add("hidden");
            document.getElementById("dashboardBody").classList.remove("hidden");
        } catch (error) {
            UI.toast(error.message, "error");
            document.getElementById("loading").classList.add("hidden");
        }
    }

    load();
})();
