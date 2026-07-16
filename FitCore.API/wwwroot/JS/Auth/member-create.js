// ============================================================
// member-create.js
// Receptionist (or Admin) front-desk flow: create a new Member
// account. POST /api/Auth/register-member — protected by
// [Authorize(Roles = "Receptionist,Admin")] on the backend.
// ============================================================

document.addEventListener("DOMContentLoaded", () => {
    requireRole(["Receptionist", "Admin"]);
    document.getElementById("memberForm").addEventListener("submit", onSubmit);
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
    };

    btn.disabled = true;
    btn.innerHTML = "Creating…";

    try {
        const result = await authFetch("/api/Auth/register-member", {
            method: "POST",
            body: JSON.stringify(payload),
        });

        showBanner(`Member account created for ${result.fullName}. They can now log in from the Member tab.`, "success");
        document.getElementById("memberForm").reset();
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
