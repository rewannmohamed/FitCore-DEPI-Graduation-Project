// ============================================================
// manage-users.js
// Admin-only. Lists every user (GET /api/Auth/users) and lets the
// Admin promote a Member to Trainer (PUT /api/Auth/promote-to-trainer/{id}).
// ============================================================

let allUsers = [];

document.addEventListener("DOMContentLoaded", async () => {
    requireRole(["Admin"]);

    document.getElementById("searchInput").addEventListener("input", renderTable);
    document.getElementById("roleFilter").addEventListener("change", renderTable);

    await loadUsers();
});

async function loadUsers() {
    try {
        allUsers = await authFetch("/api/Auth/users");
        renderTable();
    } catch (err) {
        const tbody = document.getElementById("usersTableBody");
        tbody.innerHTML = `<tr class="state-row is-error"><td colspan="6">${err.message || "Could not load users."}</td></tr>`;
    }
}

function renderTable() {
    const search = document.getElementById("searchInput").value.trim().toLowerCase();
    const roleFilter = document.getElementById("roleFilter").value;

    const filtered = allUsers.filter(u => {
        const matchesSearch = !search ||
            u.fullName.toLowerCase().includes(search) ||
            u.email.toLowerCase().includes(search);
        const matchesRole = !roleFilter || u.roles.includes(roleFilter);
        return matchesSearch && matchesRole;
    });

    const tbody = document.getElementById("usersTableBody");

    if (filtered.length === 0) {
        tbody.innerHTML = `<tr class="state-row"><td colspan="6">No users match your search.</td></tr>`;
        return;
    }

    tbody.innerHTML = filtered.map(u => {
        const rolesBadges = u.roles.map(r => `<span class="pill active" style="margin-right:4px;">${r}</span>`).join("");
        const statusPillClass = u.status === "Active" ? "active" : (u.status === "Blocked" || u.status === "Suspended" ? "cancelled" : "inactive");
        const joined = new Date(u.joinDate).toLocaleDateString();
        const canPromote = u.roles.includes("Member") && !u.roles.includes("Trainer");

        return `
            <tr>
                <td>${escapeHtml(u.fullName)}</td>
                <td>${escapeHtml(u.email)}</td>
                <td>${rolesBadges}</td>
                <td><span class="pill ${statusPillClass}">${u.status}</span></td>
                <td>${joined}</td>
                <td>
                    ${canPromote
                        ? `<button class="btn-outline btn-sm" onclick="promoteUser(${u.userID}, '${escapeHtml(u.fullName)}')"><i class="fa-solid fa-arrow-up"></i> Promote to Trainer</button>`
                        : `<span style="color: var(--text-faint); font-size: var(--fs-sm);">—</span>`}
                </td>
            </tr>
        `;
    }).join("");
}

async function promoteUser(userId, fullName) {
    if (!confirm(`Promote ${fullName} from Member to Trainer?`)) return;

    try {
        await authFetch(`/api/Auth/promote-to-trainer/${userId}`, { method: "PUT" });
        showBanner(`${fullName} is now a Trainer.`, "success");
        await loadUsers();
    } catch (err) {
        showBanner(err.message || "Could not promote this user.", "error");
    }
}

function showBanner(message, type) {
    const el = document.getElementById("msgBanner");
    el.textContent = message;
    el.className = `msg-banner show ${type}`;
    window.scrollTo({ top: 0, behavior: "smooth" });
}

function escapeHtml(str) {
    const div = document.createElement("div");
    div.textContent = str;
    return div.innerHTML;
}
