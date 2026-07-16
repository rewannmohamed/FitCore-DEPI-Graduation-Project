// Member Home / Dashboard — wired ONLY to MemberDashboardController.
// (Attendance history / QR key / reception are handled on their own pages
// via AttendanceController, on purpose — kept separate.)
//
// GET  /api/member/MemberDashboard/profile?userId=X         -> ProfileStatsDto { attendancePercentage, membershipStatus }
// GET  /api/member/MemberDashboard/next-class?userId=X      -> NextClassDto    { className, studioName, trainerName, startTime }
// GET  /api/member/MemberDashboard/notifications?userId=X   -> NotificationDto[] { title, content, timeAgo }
// GET  /api/member/MemberDashboard/digital-pass?userId=X    -> DigitalPassDto  { memberName, membershipType, validUntil, qrCodeData }


const user = getCurrentUser();
const token = getToken();

const MEMBER_DASHBOARD_ENDPOINTS = {
    profile: '/api/member/MemberDashboard/profile',
    nextClass: '/api/member/MemberDashboard/next-class',
    notifications: '/api/member/MemberDashboard/notifications',
    digitalPass: '/api/member/MemberDashboard/digital-pass',
};

const MY_QR_PAGE_URL = '/html/user/Attendance/access-key.html';

document.addEventListener('DOMContentLoaded', () => {
    if (!token) {
        window.location.href = "/html/Auth/login.html";
        return null;
    }
    requireRole(["Member"]);
    loadMemberDashboard();
    document.getElementById('checkInBtn')?.addEventListener('click', goToMyQrCode);
});

// Appends ?userId=... (or &userId=... if the endpoint already has a query string).
function withUserId(url) {
    const separator = url.includes('?') ? '&' : '?';
    return `${url}${separator}userId=${user?.userId}`;
}

async function loadMemberDashboard() {
    const results = await Promise.allSettled([
        getJson(withUserId(MEMBER_DASHBOARD_ENDPOINTS.profile)),
        getJson(withUserId(MEMBER_DASHBOARD_ENDPOINTS.nextClass)),
        getJson(withUserId(MEMBER_DASHBOARD_ENDPOINTS.notifications)),
        getJson(withUserId(MEMBER_DASHBOARD_ENDPOINTS.digitalPass)),
    ]);
    const [profileRes, nextClassRes, notificationsRes, passRes] = results;

    if (profileRes.status === 'fulfilled') {
        renderProfile(profileRes.value);
    } else {
        console.error('Error loading profile stats:', profileRes.reason);
    }

    if (nextClassRes.status === 'fulfilled') {
        renderNextClass(nextClassRes.value);
    } else {
        console.error('Error loading next class:', nextClassRes.reason);
    }

    if (notificationsRes.status === 'fulfilled') {
        renderNotifications(notificationsRes.value || []);
    } else {
        console.error('Error loading notifications:', notificationsRes.reason);
    }

    if (passRes.status === 'fulfilled') {
        renderDigitalPass(passRes.value);
    } else {
        console.error('Error loading digital pass:', passRes.reason);
    }

    if (results.some(r => r.status === 'rejected')) {
        showBanner('Some parts of your dashboard failed to load. Please refresh the page.');
    }
}

// ---------------------------------------------------------------
// Profile stats — attendance % + membership status
// ---------------------------------------------------------------
function renderProfile(profile) {
    if (!profile) return;
    const attendancePercentage = Number(pick(profile, 'attendancePercentage', 'AttendancePercentage') ?? 0);
    const membershipStatus = pick(profile, 'membershipStatus', 'MembershipStatus') ?? 'Unknown';

    const clamped = Math.max(0, Math.min(100, attendancePercentage));
    setText('attendanceRingValue', `${Math.round(clamped)}%`);
    setText('attendanceValue', `${clamped.toFixed(0)}%`);

    const ring = document.getElementById('attendanceRing');
    if (ring) {
        const circumference = 175.9;
        const offset = circumference - (clamped / 100) * circumference;
        ring.style.strokeDashoffset = offset;
    }

    setText('membershipStatusValue', membershipStatus);
    setText('membershipStatusText', membershipStatus);

    const badge = document.getElementById('membershipBadge');
    if (badge) {
        badge.classList.toggle('is-inactive', membershipStatus.toLowerCase() !== 'active');
    }
}

// ---------------------------------------------------------------
// Next class / hero card
// ---------------------------------------------------------------
function renderNextClass(nextClass) {
    const className = pick(nextClass, 'className', 'ClassName');
    const studioName = pick(nextClass, 'studioName', 'StudioName');
    const trainerName = pick(nextClass, 'trainerName', 'TrainerName');
    const startTime = pick(nextClass, 'startTime', 'StartTime');

    if (!className) {
        setText('nextClassLabel', 'No Upcoming Classes');
        setText('nextClassName', 'You have no classes scheduled');
        setText('nextClassMeta', 'Browse the class schedule to book your next session.');
        return;
    }

    setText('nextClassLabel', startTime ? `Up Next • ${formatTime(startTime)}` : 'Up Next');
    setText('nextClassName', className);

    const metaParts = [studioName, trainerName].filter(Boolean);
    setText('nextClassMeta', metaParts.length ? metaParts.join(' • ') : 'Details coming soon.');
}

// ---------------------------------------------------------------
// Check-in — takes the member to their fixed QR code so it can be
// scanned at the reception terminal (the real check-in mechanism).
// ---------------------------------------------------------------
function goToMyQrCode() {
    window.location.href = MY_QR_PAGE_URL;
}

// ---------------------------------------------------------------
// Notifications
// ---------------------------------------------------------------
function renderNotifications(notifications) {
    const container = document.getElementById('notificationsList');
    const emptyState = document.getElementById('notificationsEmptyState');
    if (!container) return;

    container.innerHTML = '';

    if (!notifications || notifications.length === 0) {
        if (emptyState) emptyState.style.display = 'block';
        return;
    }
    if (emptyState) emptyState.style.display = 'none';

    notifications.forEach(n => {
        const title = pick(n, 'title', 'Title') ?? '';
        const content = pick(n, 'content', 'Content') ?? '';
        const timeAgo = pick(n, 'timeAgo', 'TimeAgo') ?? '';

        const item = document.createElement('div');
        item.className = 'notification-item';
        item.innerHTML = `
            <div class="notification-icon"><i class="fa-regular fa-bell"></i></div>
            <div class="notification-body">
                <p class="notification-title">${escapeHtml(title)}</p>
                <p class="notification-content">${escapeHtml(content)}</p>
                <p class="notification-time">${escapeHtml(timeAgo)}</p>
            </div>
        `;
        container.appendChild(item);
    });
}

// ---------------------------------------------------------------
// Digital pass
// ---------------------------------------------------------------
function renderDigitalPass(pass) {
    if (!pass) return;
    const memberName = pick(pass, 'memberName', 'MemberName') ?? '—';
    const membershipType = pick(pass, 'membershipType', 'MembershipType') ?? '—';
    const validUntil = pick(pass, 'validUntil', 'ValidUntil');
    const qrCodeData = pick(pass, 'qrCodeData', 'QrCodeData') ?? '';

    setText('welcomeName', firstName(memberName));
    setText('passMemberName', memberName);
    setText('passMembershipType', membershipType);
    setText('passValidUntil', formatDate(validUntil));

    const qrImg = document.getElementById('passQrImage');
    if (qrImg) {
        qrImg.src = qrCodeData
            ? `https://api.qrserver.com/v1/create-qr-code/?size=240x240&data=${encodeURIComponent(qrCodeData)}`
            : '';
    }
}

// ---------------------------------------------------------------
// Small helpers
// ---------------------------------------------------------------
async function getJson(url, options = {}) {

    options.headers = options.headers || {};

    if (token) {
        options.headers['Authorization'] = `Bearer ${token}`;
    }

    const response = await fetch(url, options);

    if (!response.ok) throw new Error(`${url} failed (${response.status})`);
    return response.json();
}

function pick(obj, ...keys) {
    for (const key of keys) {
        if (obj && obj[key] !== undefined && obj[key] !== null) return obj[key];
    }
    return undefined;
}

function setText(id, value) {
    const el = document.getElementById(id);
    if (el) el.innerText = value;
}

function firstName(fullName) {
    if (!fullName) return 'Member';
    return fullName.split(' ')[0];
}

function formatTime(value) {
    const date = new Date(value);
    if (isNaN(date.getTime())) return '';
    return date.toLocaleTimeString(undefined, { hour: 'numeric', minute: '2-digit' });
}

function formatDate(value) {
    if (!value) return '—';
    const date = new Date(value);
    if (isNaN(date.getTime())) return String(value);
    return date.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
}

function escapeHtml(str) {
    return String(str).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}

function showBanner(message, isError = true) {
    const banner = document.getElementById('homeMsgBanner');
    if (!banner) return;
    banner.innerText = message;
    banner.style.display = 'block';
    banner.style.color = isError ? 'var(--status-red)' : 'var(--status-green)';
    banner.style.backgroundColor = isError ? 'var(--status-red-bg)' : 'var(--status-green-bg)';
}
