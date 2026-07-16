// ============================================================
// profile.js
// Renders the Profile page from a UserDto returned by the API.
// Expected shape (see backend UserDto):
// {
//   fullName, email, phoneNumber, status, joinDate,
//   userRoles: [{ role }],
//   trainerDto: { specialization, bio, workingHours } | null,
//   memberDto: { qrCodeData } | null
// }
// ============================================================

const API_ENDPOINT = "/api/Profile";
const token = getToken();
document.addEventListener("DOMContentLoaded", init);
const userRole = getCurrentUser();
async function init() {
    try {
        const user = await fetchProfile();
        window.user = user;
        renderProfile(user);
        dynamicLoadLayout(user.userRoles);
    } catch (err) {
        console.error("Could not load profile, showing sample data instead:", err);
        const sampleUser = getSampleUser();
        window.user = sampleUser;
        renderProfile(sampleUser);

        dynamicLoadLayout(sampleUser.userRoles);
    }

    bindActions();
}

function dynamicLoadLayout(userRoles) {
    if (!userRole.roles || userRole.roles.length === 0) return;
    console.log(userRole.roles);
    
    const primaryRole = userRole.roles[0];
    let scriptSrc = "";
    console.log(primaryRole);
 
    switch (primaryRole) {
        case "Admin":
        case 0:
            scriptSrc = "/JS/admin/Components/layout.js";
            break;
        case "Trainer":
        case 1:
            scriptSrc = "/JS/Trainer/Components/layout.js"; 
            break;
        case "Receptionist":
        case 3:
            scriptSrc = "/JS/Receptionist/Components/layout.js"; 
            break;
        default:
            scriptSrc = "/JS/user/Components/layout.js"; 
            break;
    }


    const script = document.createElement("script");
    script.src = scriptSrc;
    script.defer = true; 

    document.body.appendChild(script);

}

async function fetchProfile() {

    if (!token) {
        console.warn("No token found, redirecting to login...");
        window.location.href = "/html/Auth/login.html";
        return;
    }

    const response = await fetch(API_ENDPOINT, {
        method: "GET",
        headers: {
            "Accept": "application/json",
            "Authorization": `Bearer ${token}`
        },
        credentials: "include"
    });
    
    if (!response.ok) {
        if (response.status === 401) {
            logout(); 
        }
        throw new Error(`Request failed with status ${response.status}`);
    }

    return response.json();
}

// Sample fallback so the page is viewable before the API is wired up.
function getSampleUser() {
    return {
        fullName: "Nourhan Adel",
        email: "nourhan.adel@fitcore.app",
        phoneNumber: "+20 100 123 4567",
        status: "Active",
        joinDate: "2024-03-18T00:00:00",
        userRoles: [{ role: "Member" }, { role: "Trainer" }],
        trainerDto: {
            specialization: "Strength & Conditioning",
            bio: "Certified strength coach focused on injury-safe progressive overload. Works with beginners transitioning into structured programs.",
            workingHours: "Sat–Thu, 4:00 PM – 9:00 PM"
        },
        memberDto: {
            qrCodeData: "FITCORE-MEMBER-84213"
        }
    };
}

// ------------------------------------------------------------
// Rendering
// ------------------------------------------------------------
function renderProfile(user) {
    const initials = getInitials(user.fullName);

    // Avatars
    setText("headerAvatar", initials);
    setText("heroAvatar", initials);

    // Header / hero basics
    setText("profileName", user.fullName || "Unnamed user");
    setText("profileEmail", user.email || "—");
    setText("profilePhone", user.phoneNumber || "—");
    const joinDate = formatDate(user.joinDate);
    setText("profileJoinDate", joinDate);

    // Personal information card
    setText("infoFullName", user.fullName || "—");
    setText("infoEmail", user.email || "—");
    setText("infoPhone", user.phoneNumber || "—");
    setText("infoJoinDate", joinDate);
    const statusEl = document.getElementById("infoStatus");
    statusEl.innerHTML = "";
    statusEl.appendChild(buildStatusPill(user.status));

    // Badges under the name (primary role + status)
    const badgesWrap = document.getElementById("profileBadges");
    badgesWrap.innerHTML = "";
    const primaryRole = user.userRoles && user.userRoles.length ? user.userRoles[0].role : null;
    if (primaryRole) badgesWrap.appendChild(buildRoleChip(primaryRole));
    badgesWrap.appendChild(buildStatusPill(user.status));

    // Sidebar role badge
    setText("sidebarRoleBadge", primaryRole || "Member");

    // Roles card
    const rolesWrap = document.getElementById("rolesWrap");
    rolesWrap.innerHTML = "";
    if (user.userRoles && user.userRoles.length) {
        user.userRoles.forEach(r => rolesWrap.appendChild(buildRoleChip(r.role)));
    } else {
        rolesWrap.innerHTML = '<span class="role-chip">No roles assigned</span>';
    }

    // Trainer section
    const trainerCard = document.getElementById("trainerCard");
    if (user.trainerDto) {
        trainerCard.hidden = false;
        setText("trainerSpecialization", user.trainerDto.specialization || "—");
        setText("trainerWorkingHours", user.trainerDto.workingHours || "—");
        setText("trainerBio", user.trainerDto.bio || "");
    } else {
        trainerCard.hidden = true;
    }

    // Member / QR section
    const memberCard = document.getElementById("memberCard");
    if (user.memberDto) {
        memberCard.hidden = false;
        setText("ticketName", user.fullName || "—");
        renderQrCode(user.memberDto.qrCodeData);
    } else {
        memberCard.hidden = true;
    }
}

// ------------------------------------------------------------
// Small render helpers
// ------------------------------------------------------------
function setText(id, text) {
    const el = document.getElementById(id);
    if (el) el.textContent = text;
}

function getInitials(fullName) {
    if (!fullName) return "?";
    const parts = fullName.trim().split(/\s+/);
    const initials = parts.slice(0, 2).map(p => p[0]?.toUpperCase() || "").join("");
    return initials || "?";
}

function formatDate(value) {
    if (!value) return "—";
    const date = new Date(value);
    if (isNaN(date.getTime())) return "—";
    return date.toLocaleDateString(undefined, { year: "numeric", month: "long", day: "numeric" });
}

// Normalizes status whether it arrives as a string ("Active") or a numeric enum (0,1,2,3)
function normalizeStatus(status) {
    const map = { 0: "In Active", 1: "Active", 2: "Suspended", 3: "Pending" };
    const label = typeof status === "number" ? (map[status] || "Unknown") : (status || "Unknown");
    return label;
}

function buildStatusPill(status) {
    const label = normalizeStatus(status);
    const span = document.createElement("span");
    span.className = `status-pill ${label.toLowerCase()}`;
    span.textContent = label;
    return span;
}

function buildRoleChip(role) {
    const roleMap = {
        0: "Admin",
        1: "Trainer",
        2: "Member",
        3: "Receptionist"
    };

    let label = "Member";

    if (typeof role === "number") {
        label = roleMap[role] || "Unknown Role";
    } else if (typeof role === "string" && role.trim() !== "") {
        label = role;
    }

    const span = document.createElement("span");
    span.className = "role-chip";
    span.textContent = label;

    return span;
}

// ------------------------------------------------------------
// QR code (membership pass)
// ------------------------------------------------------------
function renderQrCode(data) {
    const holder = document.getElementById("qrCodeHolder");
    holder.innerHTML = "";

    if (!data) {
        holder.textContent = "No QR data";
        return;
    }

    if (typeof QRCode === "undefined") {
        holder.textContent = data; // graceful fallback if the CDN script failed to load
        return;
    }

    new QRCode(holder, {
        text: data,
        width: 128,
        height: 128,
        colorDark: "#0F172A",
        colorLight: "#FFFFFF",
        correctLevel: QRCode.CorrectLevel.M
    });
}

// ------------------------------------------------------------
// Actions
// ------------------------------------------------------------
function bindActions() {
    document.getElementById("editProfileBtn")?.addEventListener("click", () => {
        window.location.href = "/HTML/Profile/editprofile.html";
    });

    document.getElementById("shareProfileBtn")?.addEventListener("click", async () => {
        try {
            await navigator.clipboard.writeText(window.location.href);
            alert("Profile link copied to clipboard.");
        } catch {
            alert("Couldn't copy the link automatically — copy it from the address bar.");
        }
    });
}