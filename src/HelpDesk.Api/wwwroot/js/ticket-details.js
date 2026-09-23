(function () {
    if (!UI.initLayout()) {
        return;
    }

    const params = new URLSearchParams(location.search);
    const ticketId = Number(params.get("id"));

    if (!ticketId) {
        location.href = "tickets.html";
        return;
    }

    const currentUser = Auth.getUser();
    const agent = Auth.isAgent();
    const admin = Auth.isAdmin();

    const transitions = {
        New: ["InProgress"],
        Assigned: ["InProgress"],
        InProgress: ["Resolved"],
        Resolved: ["Closed", "InProgress"],
        Closed: ["InProgress"]
    };

    let currentTicket = null;

    const loading = document.getElementById("loading");
    const notFound = document.getElementById("notFound");
    const ticketBody = document.getElementById("ticketBody");

    function statusActionLabel(from, to) {
        if (to === "InProgress" && (from === "Resolved" || from === "Closed")) {
            return "Reopen";
        }
        if (to === "InProgress") {
            return "Move to In Progress";
        }
        if (to === "Resolved") {
            return "Mark Resolved";
        }
        if (to === "Closed") {
            return "Close Ticket";
        }
        return "Set " + UI.statusLabel(to);
    }

    function renderHeader(ticket) {
        document.getElementById("ticketNumber").textContent = ticket.ticketNumber;
        document.getElementById("ticketTitle").textContent = ticket.title;
        document.getElementById("ticketDescription").textContent = ticket.description;
        document.getElementById("ticketCategory").textContent = ticket.categoryName;
        document.getElementById("ticketPriority").innerHTML = UI.priorityBadge(ticket.priority);
        document.getElementById("ticketStatus").innerHTML = UI.statusBadge(ticket.status);
        document.getElementById("ticketCreatedBy").textContent = ticket.createdByName;
        document.getElementById("ticketAssignedTo").textContent = ticket.assignedToName || "Unassigned";
        document.getElementById("ticketCreatedAt").textContent = UI.formatDateTime(ticket.createdAt);
        document.getElementById("ticketUpdatedAt").textContent = UI.formatDateTime(ticket.updatedAt);
        document.getElementById("ticketResolvedAt").textContent = UI.formatDateTime(ticket.resolvedAt);
        document.getElementById("ticketClosedAt").textContent = UI.formatDateTime(ticket.closedAt);

        const deleteButton = document.getElementById("deleteTicketButton");
        if (admin) {
            deleteButton.classList.remove("hidden");
        }
    }

    async function renderAgentPanel(ticket) {
        const panel = document.getElementById("agentPanel");
        if (!agent) {
            panel.classList.add("hidden");
            return;
        }

        panel.classList.remove("hidden");

        const select = document.getElementById("assignSelect");
        select.innerHTML = '<option value="">Select an agent</option>';

        try {
            const agents = await API.get("/users/agents");
            agents.forEach(function (item) {
                const option = document.createElement("option");
                option.value = item.id;
                option.textContent = item.firstName + " " + item.lastName;
                select.appendChild(option);
            });
        } catch (error) {
            UI.toast("Could not load agents", "error");
        }

        select.value = ticket.assignedToId || "";

        const statusButtons = document.getElementById("statusButtons");
        const allowed = transitions[ticket.status] || [];
        if (allowed.length === 0) {
            statusButtons.innerHTML = '<span class="text-muted">No further transitions available.</span>';
        } else {
            statusButtons.innerHTML = allowed.map(function (target) {
                return '<button class="btn btn-secondary" type="button" data-target="' + target + '">' +
                    UI.escapeHtml(statusActionLabel(ticket.status, target)) + "</button>";
            }).join("");
            statusButtons.querySelectorAll("button[data-target]").forEach(function (button) {
                button.addEventListener("click", function () {
                    changeStatus(button.getAttribute("data-target"));
                });
            });
        }
    }

    function canEdit(ticket) {
        if (agent) {
            return true;
        }
        return ticket.status === "New" && ticket.createdById === currentUser.id;
    }

    async function renderEditPanel(ticket) {
        const panel = document.getElementById("editPanel");
        if (!canEdit(ticket)) {
            panel.classList.add("hidden");
            return;
        }

        panel.classList.remove("hidden");
        document.getElementById("editTitle").value = ticket.title;
        document.getElementById("editDescription").value = ticket.description;
        document.getElementById("editPriority").value = ticket.priority;

        const select = document.getElementById("editCategory");
        if (select.options.length === 0) {
            try {
                const categories = await API.get("/categories");
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
        select.value = ticket.categoryId;
    }

    function renderComments(comments) {
        const container = document.getElementById("commentsList");
        if (!comments || comments.length === 0) {
            container.innerHTML = '<div class="empty-state">No comments yet.</div>';
            return;
        }

        container.innerHTML = comments.map(function (comment) {
            const internal = comment.isInternal ? " internal" : "";
            const canDelete = admin || comment.userId === currentUser.id;
            const internalBadge = comment.isInternal ? ' <span class="badge badge-internal">Internal</span>' : "";
            return '<div class="comment' + internal + '">' +
                '<div class="avatar">' + UI.escapeHtml(UI.initials(comment.userName)) + "</div>" +
                '<div class="body">' +
                '<div class="meta"><strong>' + UI.escapeHtml(comment.userName) + "</strong> " +
                "(" + UI.escapeHtml(UI.roleLabel(comment.userRole)) + ") - " + UI.formatDateTime(comment.createdAt) + internalBadge + "</div>" +
                "<div>" + UI.escapeHtml(comment.body) + "</div>" +
                "</div>" +
                (canDelete ? '<button class="btn btn-danger btn-sm" data-comment-id="' + comment.id + '" type="button">Delete</button>' : "") +
                "</div>";
        }).join("");

        container.querySelectorAll("button[data-comment-id]").forEach(function (button) {
            button.addEventListener("click", function () {
                deleteComment(Number(button.getAttribute("data-comment-id")));
            });
        });
    }

    function renderHistory(history) {
        const container = document.getElementById("historyList");
        if (!history || history.length === 0) {
            container.innerHTML = '<div class="empty-state">No history recorded.</div>';
            return;
        }
        container.innerHTML = history.map(function (entry) {
            let text;
            if (entry.oldValue && entry.newValue) {
                text = entry.fieldName + ": " + entry.oldValue + " -> " + entry.newValue;
            } else if (entry.newValue) {
                text = entry.fieldName + ": " + entry.newValue;
            } else {
                text = entry.fieldName + " cleared";
            }
            return '<div class="timeline-item">' +
                '<div class="tl-text">' + UI.escapeHtml(text) + "</div>" +
                '<div class="tl-time">' + UI.escapeHtml(entry.changedByName) + " - " + UI.formatDateTime(entry.createdAt) + "</div>" +
                "</div>";
        }).join("");
    }

    async function refresh() {
        const detail = await API.get("/tickets/" + ticketId + "/details");
        currentTicket = detail.ticket;
        renderHeader(detail.ticket);
        await renderAgentPanel(detail.ticket);
        await renderEditPanel(detail.ticket);
        renderComments(detail.comments);
        renderHistory(detail.history);
        loading.classList.add("hidden");
        ticketBody.classList.remove("hidden");
    }

    async function changeStatus(target) {
        try {
            await API.post("/tickets/" + ticketId + "/status", { status: target });
            UI.toast("Status updated", "success");
            await refresh();
        } catch (error) {
            UI.toast(error.message, "error");
        }
    }

    async function deleteComment(commentId) {
        if (!confirm("Delete this comment?")) {
            return;
        }
        try {
            await API.del("/tickets/" + ticketId + "/comments/" + commentId);
            UI.toast("Comment deleted", "success");
            await refresh();
        } catch (error) {
            UI.toast(error.message, "error");
        }
    }

    document.getElementById("assignButton").addEventListener("click", async function () {
        const value = document.getElementById("assignSelect").value;
        if (!value) {
            UI.toast("Select an agent first", "warning");
            return;
        }
        try {
            await API.post("/tickets/" + ticketId + "/assign", { assignedToId: Number(value) });
            UI.toast("Ticket assigned", "success");
            await refresh();
        } catch (error) {
            UI.toast(error.message, "error");
        }
    });

    document.getElementById("unassignButton").addEventListener("click", async function () {
        try {
            await API.post("/tickets/" + ticketId + "/assign", { assignedToId: null });
            UI.toast("Ticket unassigned", "success");
            await refresh();
        } catch (error) {
            UI.toast(error.message, "error");
        }
    });

    document.getElementById("deleteTicketButton").addEventListener("click", async function () {
        if (!confirm("Delete this ticket permanently?")) {
            return;
        }
        try {
            await API.del("/tickets/" + ticketId);
            UI.toast("Ticket deleted", "success");
            location.href = "tickets.html";
        } catch (error) {
            UI.toast(error.message, "error");
        }
    });

    document.getElementById("editForm").addEventListener("submit", async function (event) {
        event.preventDefault();
        try {
            await API.put("/tickets/" + ticketId, {
                title: document.getElementById("editTitle").value.trim(),
                description: document.getElementById("editDescription").value.trim(),
                categoryId: Number(document.getElementById("editCategory").value),
                priority: document.getElementById("editPriority").value
            });
            UI.toast("Ticket updated", "success");
            await refresh();
        } catch (error) {
            UI.toast(error.message, "error");
        }
    });

    if (agent) {
        document.getElementById("internalRow").classList.remove("hidden");
    }

    document.getElementById("commentForm").addEventListener("submit", async function (event) {
        event.preventDefault();
        const body = document.getElementById("commentBody").value.trim();
        if (!body) {
            return;
        }
        try {
            await API.post("/tickets/" + ticketId + "/comments", {
                body: body,
                isInternal: agent && document.getElementById("commentInternal").checked
            });
            document.getElementById("commentBody").value = "";
            document.getElementById("commentInternal").checked = false;
            UI.toast("Comment added", "success");
            await refresh();
        } catch (error) {
            UI.toast(error.message, "error");
        }
    });

    refresh().catch(function (error) {
        loading.classList.add("hidden");
        if (error.status === 404 || error.status === 403) {
            notFound.classList.remove("hidden");
        } else {
            UI.toast(error.message, "error");
        }
    });
})();
