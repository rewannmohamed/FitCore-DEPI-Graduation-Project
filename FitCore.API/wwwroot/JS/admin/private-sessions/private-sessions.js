const STATUS_LABELS = ['scheduled', 'completed', 'cancelled'];
let allTrainers = [];
let allSessions = [];
let allMembers = [];
const userRole = getCurrentUser();

document.addEventListener('DOMContentLoaded', () => {
    requireRole(["Receptionist", "Admin"]);
    init();
    document.getElementById('createSessionBtn').addEventListener('click', createSession);
    document.getElementById('filterTrainer').addEventListener('change', renderTable);
    document.getElementById('filterStatus').addEventListener('change', renderTable);
});

function dynamicLoadLayout(userRoles) {
    if (!userRole.roles || userRole.roles.length === 0) return;

    const primaryRole = userRole.roles[0];
    let scriptSrc = "";

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

function showMessage(text, type) {
    const banner = document.getElementById('msgBanner');
    banner.textContent = text;
    banner.className = `msg-banner show ${type}`;
    setTimeout(() => banner.classList.remove('show'), 4000);
}

async function init() {
    try {
        dynamicLoadLayout(userRole.roles[0]);


        const trainersRes = await FitCoreApi.get('/api/Trainers?Page=1&Page_Size=50');
        allTrainers = trainersRes.data || trainersRes.Data || [];

        const membersRes = await FitCoreApi.get('/api/Auth/users');
        const usersList = membersRes.data || [];
        const membersOnly = usersList.filter(user => user.roles && user.roles.includes('Member'));

        const options = allTrainers.map(t => {
            const id = pick(t, 'trainerID', 'TrainerID');
            const name = pick(t, 'fullName', 'FullName') || `Trainer #${id}`;
            return { id, name };
        });

        const Memberoptions = membersOnly.map(t => {
            const id = pick(t, 'userID', 'userID');
            const name = pick(t, 'fullName', 'FullName') || `Member #${id}`;
            return { id, name };
        });

        document.getElementById('memberSelect').innerHTML = Memberoptions.map(o => `<option value="${o.id}">${escapeHtml(o.name)}</option>`).join('');
        document.getElementById('trainerSelect').innerHTML = options.map(o => `<option value="${o.id}">${escapeHtml(o.name)}</option>`).join('');
        document.getElementById('filterTrainer').innerHTML = '<option value="">All Trainers</option>' + options.map(o => `<option value="${o.id}">${escapeHtml(o.name)}</option>`).join('');

        await loadAllSessions();
    } catch (error) {
        const errorMsg = error.response?.data?.message || error.response?.data || error.message;
        showMessage(`Couldn't load trainers: ${errorMsg}`, 'error');
    }
}

async function loadAllSessions() {
    const tbody = document.getElementById('sessionsTableBody');
    tbody.innerHTML = `<tr class="state-row"><td colspan="6">Loading sessions…</td></tr>`;

    try {
        const response = await FitCoreApi.get(`/api/PrivateSessions?Page=1&Page_Size=50`);

        allSessions = response.data || [];
        renderTable();
    } catch (error) {
        tbody.innerHTML = `<tr class="state-row is-error"><td colspan="6">Couldn't load sessions: ${escapeHtml(error.message)}</td></tr>`;
    }
}

function renderTable() {
    const tbody = document.getElementById('sessionsTableBody');
    const trainerFilter = document.getElementById('filterTrainer').value;
    const statusFilter = document.getElementById('filterStatus').value;

    const filtered = allSessions.filter(s => {

        const sTrainerId = pick(s, 'trainerID', 'TrainerID');
        if (trainerFilter && String(sTrainerId) !== trainerFilter) return false;

        if (statusFilter !== '' && String(Number(pick(s, 'status', 'Status'))) !== statusFilter) return false;
        return true;
    }).sort((a, b) => new Date(pick(b, 'sessionDate', 'SessionDate')) - new Date(pick(a, 'sessionDate', 'SessionDate')));

    if (filtered.length === 0) {
        tbody.innerHTML = `<tr class="state-row"><td colspan="6">No private sessions found.</td></tr>`;
        return;
    }

    tbody.innerHTML = filtered.map(renderRow).join('');
    wireActions();
}

function renderRow(s) {
    const id = pick(s, 'privateSessionID', 'privateSessionID') || pick(s, 'privateSessionId', 'PrivateSessionId');
    const memberName = pick(s, 'memberName', 'MemberName') || `Member #${pick(s, 'memberUserId', 'MemberUserId')}`;
    const sessionDate = (pick(s, 'sessionDate', 'SessionDate') || '').toString().substring(0, 10);
    const start = (pick(s, 'startTime', 'StartTime') || '').toString().substring(0, 5);
    const end = (pick(s, 'endTime', 'EndTime') || '').toString().substring(0, 5);
    const status = Number(pick(s, 'status', 'Status'));
    const statusLabel = STATUS_LABELS[status] || 'unknown';
    const trainerName = pick(s, 'trainerName', 'TrainerName') || pick(s, 'trainerName', 'trainerName');

    return `
    <tr>
        <td>${escapeHtml(trainerName)}</td>
        <td>${escapeHtml(memberName)}</td>
        <td>${sessionDate}</td>
        <td>${start} - ${end}</td>
        <td><span class="pill ${statusLabel}">${statusLabel.charAt(0).toUpperCase() + statusLabel.slice(1)}</span></td>
        <td class="row-actions">
            ${status === 0 ? `
                <button class="btn-outline btn-sm" data-complete="${id}">Complete</button>
                <button class="btn-outline btn-sm" data-cancel="${id}">Cancel</button>
            ` : '—'}
        </td>
    </tr>`;
}

function wireActions() {
    document.querySelectorAll('[data-complete]').forEach(btn => btn.addEventListener('click', () => updateSession(btn.dataset.complete, 'complete')));
    document.querySelectorAll('[data-cancel]').forEach(btn => btn.addEventListener('click', () => updateSession(btn.dataset.cancel, 'cancel')));
}

async function updateSession(id, action) {
    try {
        await FitCoreApi.patch(`/api/PrivateSessions/${id}/${action}`);
        showMessage(`Session marked as ${action === 'complete' ? 'completed' : 'cancelled'}.`, 'success');
        await loadAllSessions();
    } catch (error) {
        const errorMsg = error.response?.data?.message || error.response?.data || error.message;
        showMessage(errorMsg, 'error');
    }
}

async function createSession() {
    const dto = {
        trainerID: parseInt(document.getElementById('trainerSelect').value, 10),
        memberUserId: parseInt(document.getElementById('memberSelect').value, 10),
        sessionDate: document.getElementById('sessionDate').value,
        startTime: document.getElementById('startTime').value + ':00',
        endTime: document.getElementById('endTime').value + ':00',
        notes: document.getElementById('notes').value,
    };

    try {
        const response = await FitCoreApi.post('/api/PrivateSessions', dto);

        showMessage('Private session scheduled.', 'success');

        document.getElementById('memberSelect').selectedIndex = 0;
        document.getElementById('notes').value = '';

        await loadAllSessions();
    } catch (error) {
        const errorMsg = error.response?.data?.message || error.response?.data || error.message;
        showMessage(errorMsg, 'error');
    }
}

function escapeHtml(str) {
    const div = document.createElement('div');
    div.textContent = str ?? '';
    return div.innerHTML;
}