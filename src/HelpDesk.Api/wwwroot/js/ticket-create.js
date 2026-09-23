(function () {
    if (!UI.initLayout()) {
        return;
    }

    const form = document.getElementById("ticketForm");
    const submitButton = document.getElementById("submitButton");
    const errorBox = document.getElementById("formError");
    const categorySelect = document.getElementById("categoryId");

    function showError(message) {
        errorBox.textContent = message;
        errorBox.classList.remove("hidden");
    }

    async function loadCategories() {
        try {
            const categories = await API.get("/categories");
            categories.forEach(function (category) {
                const option = document.createElement("option");
                option.value = category.id;
                option.textContent = category.name;
                categorySelect.appendChild(option);
            });
        } catch (error) {
            showError("Could not load categories: " + error.message);
        }
    }

    form.addEventListener("submit", async function (event) {
        event.preventDefault();
        errorBox.classList.add("hidden");
        submitButton.disabled = true;
        submitButton.textContent = "Creating...";

        try {
            const ticket = await API.post("/tickets", {
                title: document.getElementById("title").value.trim(),
                description: document.getElementById("description").value.trim(),
                categoryId: Number(categorySelect.value),
                priority: document.getElementById("priority").value
            });
            UI.toast("Ticket " + ticket.ticketNumber + " created", "success");
            location.href = "ticket-details.html?id=" + ticket.id;
        } catch (error) {
            showError(error.message);
            submitButton.disabled = false;
            submitButton.textContent = "Create ticket";
        }
    });

    loadCategories();
})();
