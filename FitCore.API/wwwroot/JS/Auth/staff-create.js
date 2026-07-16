// ============================================================
// staff-create.js
// Admin-only flow: create a Trainer or Receptionist account.
// POST /api/Auth/create-staff — protected by [Authorize(Roles = "Admin")].
// ============================================================

document.addEventListener("DOMContentLoaded", () => {
    // requireRole(["Admin"]);
    document.getElementById("staffForm").addEventListener("submit", onSubmit);
});

async function onSubmit(e) {
    e.preventDefault();
    const btn = document.getElementById("createBtn");
    const original = btn.innerHTML;

    const payload = {
        fullName: document.getElementById("fullName").value.trim(),
        email: document.getElementById("email").value.trim(),
        phoneNumber: document.getElementById("phoneNumber").value.trim(),
        password: document.getElementById("password").value,
        role: document.getElementById("role").value,
    };

    btn.disabled = true;
    btn.innerHTML = "Creating…";

    try {
        const result = await authFetch("/api/Auth/create-staff", {
            method: "POST",
            body: JSON.stringify(payload),
        });

        showBanner(`${payload.role} account created for ${result.fullName}.`, "success");
        document.getElementById("staffForm").reset();
    } catch (err) {
        const detail = err.errors && err.errors.length ? ": " + err.errors.join(", ") : "";
        showBanner((err.message || "Could not create the account.") + detail, "error");
    } finally {
        btn.disabled = false;
        btn.innerHTML = original;
    }
}

function showBanner(message, type) {
    const el = document.getElementById("msgBanner");
    el.textContent = message;
    el.className = `msg-banner show ${type}`;
    window.scrollTo({ top: 0, behavior: "smooth" });
}
