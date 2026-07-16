// reception-terminal.js
// Wires the Admin Reception Terminal to AttendanceController's ops endpoints:
//   POST /api/Attendance/checkin/scan?searchInput=
//   POST /api/Attendance/checkin/manual?searchInput=
//   GET  /api/Attendance/members/search?query=
//   GET  /api/Attendance/members/{userId}/checkin-summary
//   GET  /api/Attendance/recent-scans
//   GET  /api/Attendance/daily-logs
//
// Note: checkin/scan and checkin/manual currently hit the exact same backend
// handler (CheckInManual) — there's no dedicated QR-payload endpoint yet, so
// "Simulate Scan" and "Manual Check-In" both just pass the typed value as
// searchInput to their respective routes.

const ATTENDANCE_BASE = '/api/Attendance';

document.addEventListener('DOMContentLoaded', () => {
    requireRole(["Receptionist"]);
    loadRecentScans();

    document.getElementById('manualCheckInBtn')?.addEventListener('click', () => doCheckIn('manual'));
    document.getElementById('simulateScanBtn')?.addEventListener('click', () => doCheckIn('scan'));
    document.getElementById('memberSearchBtn')?.addEventListener('click', searchMembers);
    document.getElementById('memberSearchInput')?.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') searchMembers();
    });
    document.getElementById('searchInput')?.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') doCheckIn('manual');
    });
    document.getElementById('viewDailyLogsBtn')?.addEventListener('click', loadDailyLogs);
});

async function doCheckIn(mode) {
    const input = document.getElementById('searchInput');
    const value = input.value.trim();
    if (!value) {
        showBanner('Enter a Member ID, email or QR code first.');
        return;
    }

    const btn = document.getElementById(mode === 'manual' ? 'manualCheckInBtn' : 'simulateScanBtn');
    const originalHtml = btn.innerHTML;
    btn.disabled = true;
    btn.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Verifying…';

    try {
        const endpoint = mode === 'manual' ? 'checkin/manual' : 'checkin/scan';
        const result = await FitCoreApi.post(`${ATTENDANCE_BASE}/${endpoint}?searchInput=${encodeURIComponent(value)}`);
        renderCheckInResult(result);
        loadRecentScans();
    } catch (error) {
        console.error('Error checking in member:', error);
        showScanBanner('Check-in failed. Please try again.', true);
    } finally {
        btn.disabled = false;
        btn.innerHTML = originalHtml;
    }
}

function renderCheckInResult(result) {
    const isSuccess = pick(result, 'isSuccess', 'IsSuccess');
    const message = pick(result, 'message', 'Message');
    const memberName = pick(result, 'memberName', 'MemberName');
    const membershipType = pick(result, 'membershipType', 'MembershipType');
    const remainingSessions = pick(result, 'remainingSessions', 'RemainingSessions');
    const expiryDate = pick(result, 'expiryDate', 'ExpiryDate');
    const checkInTime = pick(result, 'checkInTime', 'CheckInTime');

    showScanBanner(`${isSuccess ? 'Access Granted' : 'Access Denied'}: ${memberName || message}`, !isSuccess, checkInTime);

    const body = document.getElementById('profileCardBody');
    body.innerHTML = `
        <div class="profile-view">
            <div class="profile-avatar"><i class="fa-solid fa-user"></i></div>
            <div class="profile-name">${escapeHtml(memberName || 'Unknown Member')}</div>
            <div class="profile-role">${escapeHtml(membershipType || '--')}</div>
            <div class="profile-info-grid">
                <div class="profile-info-item">
                    <span>Remaining Sessions</span>
                    <strong>${remainingSessions ?? '--'}</strong>
                </div>
                <div class="profile-info-item">
                    <span>Expiry Date</span>
                    <strong>${expiryDate ? formatDate(expiryDate) : '--'}</strong>
                </div>
            </div>
            <p class="empty-state">${escapeHtml(message || '')}</p>
        </div>
    `;
}

async function searchMembers() {
    const input = document.getElementById('memberSearchInput');
    const query = input.value.trim();
    const resultsBox = document.getElementById('memberSearchResults');
    if (!query) {
        resultsBox.innerHTML = '';
        return;
    }

    resultsBox.innerHTML = '<p class="empty-state">Searching…</p>';

    try {
        const members = await FitCoreApi.get(`${ATTENDANCE_BASE}/members/search?query=${encodeURIComponent(query)}`);
        if (!Array.isArray(members) || members.length === 0) {
            resultsBox.innerHTML = '<p class="empty-state">No members found.</p>';
            return;
        }

        resultsBox.innerHTML = members.map((m) => {
            const userId = pick(m, 'userId', 'UserId');
            const name = pick(m, 'name', 'Name');
            const email = pick(m, 'email', 'Email');
            const phone = pick(m, 'phone', 'Phone');
            return `
                <div class="search-result-item" data-user-id="${userId}">
                    <div>
                        <div class="search-result-name">${escapeHtml(name || 'Unknown')}</div>
                        <div class="search-result-meta">${escapeHtml(email || phone || '')}</div>
                    </div>
                    <i class="fa-solid fa-chevron-right"></i>
                </div>
            `;
        }).join('');

        resultsBox.querySelectorAll('.search-result-item').forEach((item) => {
            item.addEventListener('click', () => loadCheckInSummary(item.dataset.userId));
        });
    } catch (error) {
        console.error('Error searching members:', error);
        resultsBox.innerHTML = '<p class="empty-state">Could not search members.</p>';
    }
}

async function loadCheckInSummary(userId) {
    const body = document.getElementById('profileCardBody');
    body.innerHTML = '<p class="empty-state">Loading member summary…</p>';

    try {
        const summary = await FitCoreApi.get(`${ATTENDANCE_BASE}/members/${userId}/checkin-summary`);

        const fullName = pick(summary, 'fullName', 'FullName');
        const status = pick(summary, 'status', 'Status');
        const pkg = pick(summary, 'package', 'Package');
        const warnings = pick(summary, 'warnings', 'Warnings') || [];
        const lastCheckIn = pick(summary, 'lastCheckIn', 'LastCheckIn');

        body.innerHTML = `
            <div class="profile-view">
                <div class="profile-avatar"><i class="fa-solid fa-user"></i></div>
                <div class="profile-name">${escapeHtml(fullName || 'Unknown Member')}</div>
                <div class="profile-role">${escapeHtml(pkg || '--')}</div>
                <div class="profile-info-grid">
                    <div class="profile-info-item">
                        <span>Status</span>
                        <strong>${escapeHtml(status || '--')}</strong>
                    </div>
                    <div class="profile-info-item">
                        <span>Last Check-In</span>
                        <strong>${escapeHtml(lastCheckIn || '--')}</strong>
                    </div>
                </div>
                ${warnings.map((w) => `<div class="profile-warning"><i class="fa-solid fa-triangle-exclamation"></i> ${escapeHtml(w)}</div>`).join('')}
            </div>
        `;
    } catch (error) {
        console.error('Error loading check-in summary:', error);
        body.innerHTML = '<p class="empty-state">Could not load member summary.</p>';
    }
}

async function loadRecentScans() {
    const list = document.getElementById('recentScansList');
    try {
        const scans = await FitCoreApi.get(`${ATTENDANCE_BASE}/recent-scans`);
        renderScansList(list, scans);
    } catch (error) {
        console.error('Error loading recent scans:', error);
        list.innerHTML = '<p class="empty-state">Could not load recent scans.</p>';
    } finally {
        list.setAttribute('aria-busy', 'false');
    }
}

async function loadDailyLogs() {
    const list = document.getElementById('recentScansList');
    list.innerHTML = '<p class="empty-state">Loading daily logs…</p>';

    try {
        const data = await FitCoreApi.get(`${ATTENDANCE_BASE}/daily-logs`);
        const logs = pick(data, 'logs', 'Logs') || [];
        const totalLogsToday = pick(data, 'totalLogsToday', 'TotalLogsToday');

        renderScansList(list, logs);
        showBanner(`Showing ${totalLogsToday ?? logs.length} logs for today.`);
    } catch (error) {
        console.error('Error loading daily logs:', error);
        list.innerHTML = '<p class="empty-state">Could not load daily logs.</p>';
    }
}

function renderScansList(container, scans) {
    if (!Array.isArray(scans) || scans.length === 0) {
        container.innerHTML = '<p class="empty-state">No scans yet.</p>';
        return;
    }

    container.innerHTML = scans.map((scan) => {
        const fullName = pick(scan, 'fullName', 'FullName');
        const type = pick(scan, 'type', 'Type');
        const checkInTime = pick(scan, 'checkInTime', 'CheckInTime');
        return `
            <div class="scan-row">
                <div>
                    <div class="scan-row-name">${escapeHtml(fullName || 'Unknown')}</div>
                    <div class="scan-row-meta">${escapeHtml(type || '')}${checkInTime ? ' • ' + formatTime(checkInTime) : ''}</div>
                </div>
                <i class="fa-solid fa-circle-check" style="color: var(--status-green);"></i>
            </div>
        `;
    }).join('');
}

function showBanner(message) {
    const banner = document.getElementById('msgBanner');
    banner.textContent = message;
    banner.style.display = 'block';
}

function showScanBanner(message, isError = false, time = null) {
    const banner = document.getElementById('scanResultBanner');
    const text = document.getElementById('scanResultText');
    const timeEl = document.getElementById('scanResultTime');

    text.textContent = message;
    timeEl.textContent = time ? formatTime(time) : '';
    banner.classList.toggle('is-error', isError);
    banner.style.display = 'flex';
}

function formatTime(value) {
    const date = new Date(value);
    if (isNaN(date.getTime())) return '';
    return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' });
}

function formatDate(value) {
    const date = new Date(value);
    if (isNaN(date.getTime())) return '--';
    return date.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
}

function escapeHtml(str) {
    const div = document.createElement('div');
    div.textContent = String(str ?? '');
    return div.innerHTML;
}
