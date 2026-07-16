// Attendance.js (Activity History page)
// Wires to AttendanceController's self-service endpoints:
//   GET /api/Attendance/me/history?userId=&page=&pageSize=
//   GET /api/Attendance/me/stats?userId=
// userId is a required query param since there is no auth yet.

const ATTENDANCE_BASE = '/api/Attendance';
const PAGE_SIZE = 10;

let currentPage = 1;
let totalRecords = 0;
const user = getCurrentUser();
document.addEventListener('DOMContentLoaded', () => {
    requireRole(["Member"]);
    loadStats();
    loadHistory(1);

    document.getElementById('loadMoreBtn')?.addEventListener('click', () => {
        currentPage += 1;
        loadHistory(currentPage, true);
    });
});


async function loadStats() {
    const grid = document.getElementById('statsGrid');
    try {
        const stats = await FitCoreApi.get(`${ATTENDANCE_BASE}/me/stats`);

        document.getElementById('statAttendanceRate').textContent =
            pick(stats, 'attendanceRate', 'AttendanceRate') ?? '--';
        document.getElementById('statTotalDays').textContent =
            pick(stats, 'totalDays', 'TotalDays') ?? '--';
        document.getElementById('statAllowedDays').textContent =
            pick(stats, 'allowedDays', 'AllowedDays') ?? '--';
    } catch (error) {
        console.error('Error loading attendance stats:', error);
        showBanner('Could not load your attendance stats.');
    } finally {
        grid.setAttribute('aria-busy', 'false');
    }
}

async function loadHistory(page, append = false) {
    const tbody = document.getElementById('historyTableBody');
    const emptyState = document.getElementById('historyEmptyState');
    const loadMoreBtn = document.getElementById('loadMoreBtn');

    try {
        const data = await FitCoreApi.get(
            `${ATTENDANCE_BASE}/me/history?userId=${user.userId}&page=${page}&pageSize=${PAGE_SIZE}`
        );

        const history = pick(data, 'history', 'History') || [];
        totalRecords = pick(data, 'totalRecords', 'TotalRecords') ?? history.length;

        if (!append) tbody.innerHTML = '';

        if (history.length === 0 && !append) {
            emptyState.style.display = 'block';
        } else {
            emptyState.style.display = 'none';
            tbody.insertAdjacentHTML('beforeend', history.map(renderHistoryRow).join(''));
        }

        loadMoreBtn.style.display = (page * PAGE_SIZE >= totalRecords) ? 'none' : 'inline-flex';
    } catch (error) {
        console.error('Error loading attendance history:', error);
        showBanner('Could not load your attendance history.');
    }
}

function renderHistoryRow(entry) {
    const date = escapeHtml(pick(entry, 'date', 'Date') || '--');
    const time = escapeHtml(pick(entry, 'time', 'Time') || '--');
    const type = escapeHtml(pick(entry, 'type', 'Type') || '--');

    return `
        <tr>
            <td>${date}</td>
            <td>${time}</td>
            <td><span class="type-pill">${type}</span></td>
        </tr>
    `;
}

function showBanner(message) {
    const banner = document.getElementById('msgBanner');
    banner.textContent = message;
    banner.style.display = 'block';
}

function escapeHtml(str) {
    const div = document.createElement('div');
    div.textContent = str;
    return div.innerHTML;
}
