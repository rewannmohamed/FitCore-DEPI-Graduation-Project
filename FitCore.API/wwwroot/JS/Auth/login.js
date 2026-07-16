// ============================================================
// login.js
// One login form serves every role. The Member/Staff/Admin tabs
// only change the welcome copy — the backend is the single source
// of truth for which roles an account actually has, and we redirect
// based on what it returns, not what tab was clicked.
// ============================================================

const TAB_COPY = {
    Member: {
        subtitle: "Log in to access your member dashboard.",
        visualTitle: "Precision in every movement.",
        visualSubtitle: "Access your personalized training metrics and real-time studio updates with the FitCore ecosystem.",
    },
    Staff: {
        subtitle: "Log in to manage classes, members, and daily operations.",
        visualTitle: "Run the floor, not just the front desk.",
        visualSubtitle: "Trainers and receptionists get the tools to manage members, sessions, and check-ins in one place.",
    },
    Admin: {
        subtitle: "Log in to manage staff, roles, and gym-wide settings.",
        visualTitle: "Full visibility, full control.",
        visualSubtitle: "Oversee every member, every trainer, and every policy across the FitCore ecosystem.",
    },
};

document.addEventListener("DOMContentLoaded", () => {
    // If already logged in, skip straight to the right landing page.
    const existing = getCurrentUser();
    if (existing && getToken()) {
        redirectForRoles(existing.roles);
        return;
    }

    document.querySelectorAll("#roleTabs button").forEach(btn => {
        btn.addEventListener("click", () => {
            document.querySelectorAll("#roleTabs button").forEach(b => b.classList.remove("active"));
            btn.classList.add("active");
            applyTabCopy(btn.dataset.role);
        });
    });

    document.getElementById("togglePassword").addEventListener("click", () => {
        const input = document.getElementById("password");
        const icon = document.querySelector("#togglePassword i");
        const isHidden = input.type === "password";
        input.type = isHidden ? "text" : "password";
        icon.className = isHidden ? "fa-regular fa-eye-slash" : "fa-regular fa-eye";
    });

    document.getElementById("loginForm").addEventListener("submit", onSubmit);
});

function applyTabCopy(role) {
    const copy = TAB_COPY[role] || TAB_COPY.Member;
    document.getElementById("formSubtitle").textContent = copy.subtitle;
    document.getElementById("visualTitle").textContent = copy.visualTitle;
    document.getElementById("visualSubtitle").textContent = copy.visualSubtitle;
}

async function onSubmit(e) {
    e.preventDefault();
    hideError();

    const email = document.getElementById("email").value.trim();
    const password = document.getElementById("password").value;
    const btn = document.getElementById("loginBtn");

    btn.disabled = true;
    btn.textContent = "Logging in…";

    try {
        const response = await fetch("/api/Auth/login", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ email, password }),
        });

        const data = await response.json();

        if (!response.ok) {
            throw new Error(data.message || data.Message || "Invalid email or password.");
        }

        saveSession(data);
        redirectForRoles(data.roles);
    } catch (err) {
        showError(err.message || "Something went wrong. Please try again.");
    } finally {
        btn.disabled = false;
        btn.textContent = "Login";
    }
}

function redirectForRoles(roles) {
    roles = roles || [];
    if (roles.includes("Admin")) {
        window.location.href = "/html/admin/dashboard/dashboard.html";
    } else if (roles.includes("Receptionist")) {
        window.location.href = "/html/Receptionist/Dashboard/receptionist-dashboard.html";
    } else if (roles.includes("Trainer")) {
        window.location.href = "/html/Trainer/Dashboard/trainer-dashboard.html";
    } else {
        window.location.href = "/html/user/MemberDashboard/member-dashboard.html";
    }
}

function showError(message) {
    const el = document.getElementById("loginError");
    el.textContent = message;
    el.classList.add("show");
}

function hideError() {
    const el = document.getElementById("loginError");
    el.classList.remove("show");
    el.textContent = "";
}
