// Admin Dashboard — wires the page to the real AdminDashboardController endpoints.
// Base path: /api/admin/AdminDashboard/...
// Uses the shared FitCoreApi wrapper + pick() helper from /JS/admin/Components/api.js

const DASHBOARD_ENDPOINTS = {
    stats: '/api/admin/AdminDashboard/stats',
    planDistribution: '/api/admin/AdminDashboard/plan-distribution',
    recentAlerts: '/api/admin/AdminDashboard/alerts/recent',
    recentEnrolments: '/api/admin/AdminDashboard/members/recent',
    revenueChart: '/api/admin/AdminDashboard/revenue-chart',
};

let lastRevenueChartData = [];

document.addEventListener('DOMContentLoaded', () => {
    requireRole(["Admin"]);
    loadDashboardData();
});

async function loadDashboardData() {
    showBanner(null);

    const results = await Promise.allSettled([
        FitCoreApi.get(DASHBOARD_ENDPOINTS.stats),
        FitCoreApi.get(DASHBOARD_ENDPOINTS.revenueChart),
        FitCoreApi.get(DASHBOARD_ENDPOINTS.planDistribution),
        FitCoreApi.get(DASHBOARD_ENDPOINTS.recentAlerts),
        FitCoreApi.get(DASHBOARD_ENDPOINTS.recentEnrolments),
    ]);

    const [statsRes, chartRes, plansRes, alertsRes, enrolmentsRes] = results;

    if (statsRes.status === 'fulfilled') {
        renderStats(statsRes.value);
    } else {
        console.error('Error loading stats:', statsRes.reason);
    }

    if (chartRes.status === 'fulfilled') {
        lastRevenueChartData = chartRes.value || [];
        renderRevenueChart(lastRevenueChartData);
    } else {
        console.error('Error loading revenue chart:', chartRes.reason);
    }

    if (plansRes.status === 'fulfilled') {
        renderPlanDistribution(plansRes.value || []);
    } else {
        console.error('Error loading plan distribution:', plansRes.reason);
    }

    if (alertsRes.status === 'fulfilled') {
        renderAlerts(alertsRes.value || []);
    } else {
        console.error('Error loading alerts:', alertsRes.reason);
    }

    if (enrolmentsRes.status === 'fulfilled') {
        renderRecentEnrolments(enrolmentsRes.value || []);
    } else {
        console.error('Error loading recent enrolments:', enrolmentsRes.reason);
    }

    if (results.some(r => r.status === 'rejected')) {
        showBanner('Some dashboard widgets failed to load. Check your connection and try again.');
    }

    document.getElementById('statsGrid')?.removeAttribute('aria-busy');
    document.getElementById('exportReportBtn')?.addEventListener('click', exportReportCsv); }

// ---------------------------------------------------------------
// KPI cards — DashboardStatsDto { totalMembers, monthlyRevenue, activePlans, dailyAttendance }
// ---------------------------------------------------------------
function renderStats(stats) {
    if (!stats) return;
    const totalMembers = pick(stats, 'totalMembers', 'TotalMembers') ?? 0;
    const monthlyRevenue = pick(stats, 'monthlyRevenue', 'MonthlyRevenue') ?? 0;
    const activePlans = pick(stats, 'activePlans', 'ActivePlans') ?? 0;
    const dailyAttendance = pick(stats, 'dailyAttendance', 'DailyAttendance') ?? 0;

    setText('statTotalMembers', Number(totalMembers).toLocaleString());
    setText('statMonthlyRevenue', formatCurrency(monthlyRevenue));
    setText('statActivePlans', Number(activePlans).toLocaleString());
    setText('statDailyAttendance', Number(dailyAttendance).toLocaleString());
}

// ---------------------------------------------------------------
// Revenue vs Expenses — List<RevenueChartDto> { monthName, totalRevenue, totalExpenses }
// ---------------------------------------------------------------
function renderRevenueChart(data) {
    const container = document.getElementById('revenueChart');
    const emptyState = document.getElementById('revenueEmptyState');
    if (!container) return;

    container.innerHTML = '';

    if (!data || data.length === 0) {
        if (emptyState) emptyState.style.display = 'block';
        return;
    }
    if (emptyState) emptyState.style.display = 'none';

    const maxValue = Math.max(
        1,
        ...data.map(d => Math.max(
            Number(pick(d, 'totalRevenue', 'TotalRevenue') ?? 0),
            Number(pick(d, 'totalExpenses', 'TotalExpenses') ?? 0)
        ))
    );

    data.forEach(entry => {
        const month = pick(entry, 'monthName', 'MonthName') ?? '';
        const revenue = Number(pick(entry, 'totalRevenue', 'TotalRevenue') ?? 0);
        const expenses = Number(pick(entry, 'totalExpenses', 'TotalExpenses') ?? 0);

        const revenueHeight = Math.round((revenue / maxValue) * 100);
        const expenseHeight = Math.round((expenses / maxValue) * 100);

        const group = document.createElement('div');
        group.className = 'bar-group';
        group.title = `${month}: Revenue ${formatCurrency(revenue)} \u2022 Expenses ${formatCurrency(expenses)}`;
        group.innerHTML = `
            <div class="bar-pair">
                <div class="bar bar-revenue" style="height:${revenueHeight}%"></div>
                <div class="bar bar-expense" style="height:${expenseHeight}%"></div>
            </div>
            <span class="bar-month-label">${escapeHtml(month.slice(0, 3))}</span>
        `;
        container.appendChild(group);
    });
}

// ---------------------------------------------------------------
// Plan Distribution — List<PlanDistributionDto> { planName, count, percentage }
// ---------------------------------------------------------------
function renderPlanDistribution(plans) {
    const container = document.getElementById('planDistribution');
    const emptyState = document.getElementById('planEmptyState');
    if (!container) return;

    container.innerHTML = '';

    if (!plans || plans.length === 0) {
        if (emptyState) emptyState.style.display = 'block';
        return;
    }
    if (emptyState) emptyState.style.display = 'none';

    plans
        .slice()
        .sort((a, b) => (pick(b, 'percentage', 'Percentage') ?? 0) - (pick(a, 'percentage', 'Percentage') ?? 0))
        .forEach(plan => {
            const name = pick(plan, 'planName', 'PlanName') ?? 'Unknown Plan';
            const percentage = Number(pick(plan, 'percentage', 'Percentage') ?? 0);

            const row = document.createElement('div');
            row.className = 'plan-row';
            row.innerHTML = `
                <div class="plan-row-top">
                    <span>${escapeHtml(name)}</span>
                    <span>${percentage.toFixed(0)}%</span>
                </div>
                <div class="plan-bar-track">
                    <div class="plan-bar-fill" style="width:${Math.min(100, percentage)}%"></div>
                </div>
            `;
            container.appendChild(row);
        });
}

// ---------------------------------------------------------------
// Recent System Alerts — List<RecentAlertDto> { title, content, type, timeAgo }
// ---------------------------------------------------------------
function renderAlerts(alerts) {
    const container = document.getElementById('alertsList');
    const emptyState = document.getElementById('alertsEmptyState');
    if (!container) return;

    container.innerHTML = '';

    if (!alerts || alerts.length === 0) {
        if (emptyState) emptyState.style.display = 'block';
        return;
    }
    if (emptyState) emptyState.style.display = 'none';

    alerts.forEach(alert => {
        const title = pick(alert, 'title', 'Title') ?? '';
        const content = pick(alert, 'content', 'Content') ?? '';
        const type = (pick(alert, 'type', 'Type') ?? '').toString().toLowerCase();
        const timeAgo = pick(alert, 'timeAgo', 'TimeAgo') ?? '';

        const { icon, className } = alertVisualsForType(type);

        const item = document.createElement('div');
        item.className = 'alert-item';
        item.innerHTML = `
            <div class="alert-icon ${className}"><i class="fa-solid ${icon}"></i></div>
            <div class="alert-body">
                <div class="alert-title-row">
                    <span class="alert-title">${escapeHtml(title)}</span>
                    <span class="alert-time">${escapeHtml(timeAgo)}</span>
                </div>
                <p class="alert-desc">${escapeHtml(content)}</p>
            </div>
        `;
        container.appendChild(item);
    });
}

function alertVisualsForType(type) {
    if (type.includes('error') || type.includes('fail') || type.includes('danger')) {
        return { icon: 'fa-triangle-exclamation', className: 'icon-danger' };
    }
    if (type.includes('warn') || type.includes('stock')) {
        return { icon: 'fa-clipboard-list', className: 'icon-warning' };
    }
    if (type.includes('security') || type.includes('login') || type.includes('admin')) {
        return { icon: 'fa-shield-halved', className: 'icon-info' };
    }
    return { icon: 'fa-circle-info', className: 'icon-info' };
}

// ---------------------------------------------------------------
// Recent Enrolments — List<RecentEnrolmentDto> { memberName, planType, status, joinDate }
// ---------------------------------------------------------------
function renderRecentEnrolments(members) {
    const tableBody = document.getElementById('enrolmentsTableBody');
    const emptyState = document.getElementById('enrolmentsEmptyState');
    if (!tableBody) return;

    tableBody.innerHTML = '';

    if (!members || members.length === 0) {
        if (emptyState) emptyState.style.display = 'block';
        return;
    }
    if (emptyState) emptyState.style.display = 'none';

    members.forEach(member => {
        const name = pick(member, 'memberName', 'MemberName') ?? '\u2014';
        const plan = pick(member, 'planType', 'PlanType') ?? '\u2014';
        const status = (pick(member, 'status', 'Status') ?? '').toString();
        const joinDate = pick(member, 'joinDate', 'JoinDate');

        const row = document.createElement('tr');
        row.innerHTML = `
            <td>
                <div class="member-cell">
                    <div class="avatar">${initialsFor(name)}</div>
                    <span>${escapeHtml(name)}</span>
                </div>
            </td>
            <td>${escapeHtml(plan)}</td>
            <td><span class="status-pill ${statusClass(status)}">${escapeHtml(status)}</span></td>
            <td>${formatDate(joinDate)}</td>
        `;
        tableBody.appendChild(row);
    });
}

function statusClass(status) {
    const s = status.toLowerCase();
    if (s.includes('active')) return 'status-active';
    if (s.includes('pending')) return 'status-pending';
    if (s.includes('expired') || s.includes('cancel')) return 'status-expired';
    return 'status-pending';
}

function initialsFor(name) {
    if (!name) return '?';
    return name
        .split(' ')
        .filter(Boolean)
        .slice(0, 2)
        .map(part => part[0].toUpperCase())
        .join('');
}

// ---------------------------------------------------------------
// Export Report — client-side CSV of the currently loaded revenue chart.
// (The API doesn't expose a dedicated export endpoint yet, so this
// builds the file from data already on the page rather than calling
// a non-existent /export route.)
// ---------------------------------------------------------------
//function exportReportCsv() {
//    console.log("Export button clicked!");

//    if (!lastRevenueChartData || lastRevenueChartData.length === 0) {
//        showBanner('Nothing to export yet — the revenue chart is still loading.');
//        return;
//    }

//    window.location.href = '/api/admin/AdminDashboard/export-report';
//}



async function exportReportCsv() {
    console.log("Export button clicked!");

    if (!lastRevenueChartData || lastRevenueChartData.length === 0) {
        showBanner('Nothing to export yet — the revenue chart is still loading.');
        return;
    }

    const btn = document.getElementById('exportReportBtn');
    if (btn) btn.disabled = true;

    try {
        const token = getToken();
        const response = await fetch('/api/admin/AdminDashboard/export-report', {
            method: 'GET',
            headers: token ? { 'Authorization': `Bearer ${token}` } : {}
        });

        if (response.status === 401) {
            logout();
            throw new Error('Session expired. Please log in again.');
        }
        if (!response.ok) throw new Error(`Export failed (${response.status})`);

        const blob = await response.blob();

        // Prefer the filename the server sent, fall back to a sensible default.
        const contentDisposition = response.headers.get('Content-Disposition') || '';
        const fileNameMatch = contentDisposition.match(/filename="?([^"]+)"?/i);
        const fileName = fileNameMatch ? fileNameMatch[1] : `RevenueReport_${new Date().toISOString().slice(0, 10)}.csv`;

        const downloadUrl = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = downloadUrl;
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        link.remove();
        window.URL.revokeObjectURL(downloadUrl);
    } catch (error) {
        console.error('Error exporting report:', error);
        showBanner('Could not export the report. Please try again.');
    } finally {
        if (btn) btn.disabled = false;
    }
}






// ---------------------------------------------------------------
// Small helpers
// ---------------------------------------------------------------
function setText(id, value) {
    const el = document.getElementById(id);
    if (el) el.innerText = value;
}

function formatCurrency(value) {
    const num = Number(value) || 0;
    return num.toLocaleString(undefined, { style: 'currency', currency: 'USD', maximumFractionDigits: 0 });
}

function formatDate(value) {
    if (!value) return '\u2014';
    const date = new Date(value);
    if (isNaN(date.getTime())) return String(value);
    return date.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
}

function escapeHtml(str) {
    return String(str)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;');
}

function showBanner(message) {
    const banner = document.getElementById('msgBanner');
    if (!banner) return;
    if (!message) {
        banner.style.display = 'none';
        banner.innerText = '';
        return;
    }
    banner.innerText = message;
    banner.style.display = 'block';
}
