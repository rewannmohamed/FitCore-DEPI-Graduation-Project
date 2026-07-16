// trainer-private-sessions.js
const user = getCurrentUser();
document.addEventListener('DOMContentLoaded', loadSessions);

function showMessage(text, type) {
    const banner = document.getElementById('msgBanner');
    banner.textContent = text;
    banner.className = `msg-banner show ${type}`;
    setTimeout(() => banner.classList.remove('show'), 4000);
}

async function loadSessions() {
    requireRole(["Trainer"]);
    const tbody = document.getElementById('sessionsTableBody');
    tbody.innerHTML = `<tr class="state-row"><td colspan="6">Loading sessions…</td></tr>`;
    try {
        const sessions = await FitCoreApi.get(`/api/PrivateSessions/trainer/${user.userId}`);
        if (!sessions || sessions.length === 0) {
            tbody.innerHTML = `<tr class="state-row"><td colspan="6">No private sessions scheduled.</td></tr>`;
            return;
        }

        tbody.innerHTML = sessions.map(renderRow).join('');
        wireActions();
    } catch (error) {
        tbody.innerHTML = `<tr class="state-row is-error"><td colspan="6">Couldn't load sessions: ${escapeHtml(error.message)}</td></tr>`;
    }
}

const STATUS_LABELS = ['scheduled', 'completed', 'cancelled'];

function renderRow(s) {
    const id = pick(s, 'privateSessionID', 'PrivateSessionID');
    const memberName = pick(s, 'memberName', 'MemberName') || `Member #${pick(s, 'memberUserId', 'MemberUserId')}`;
    const sessionDate = (pick(s, 'sessionDate', 'SessionDate') || '').toString().substring(0, 10);
    const start = (pick(s, 'startTime', 'StartTime') || '').toString().substring(0, 5);
    const end = (pick(s, 'endTime', 'EndTime') || '').toString().substring(0, 5);
    const notes = pick(s, 'notes', 'Notes') || '—';
    const status = Number(pick(s, 'status', 'Status'));
    const statusLabel = STATUS_LABELS[status] || 'unknown';

    return `
    <tr>
        <td>${escapeHtml(memberName)}</td>
        <td>${sessionDate}</td>
        <td>${start} - ${end}</td>
        <td>${escapeHtml(notes)}</td>
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
    document.querySelectorAll('[data-complete]').forEach(btn => {
        btn.addEventListener('click', () => updateSession(btn.dataset.complete, 'complete'));
    });
    document.querySelectorAll('[data-cancel]').forEach(btn => {
        btn.addEventListener('click', () => updateSession(btn.dataset.cancel, 'cancel'));
    });
}

async function updateSession(id, action) {
    try {
        await FitCoreApi.patch(`/api/PrivateSessions/${id}/${action}`);
        showMessage(`Session marked as ${action === 'complete' ? 'completed' : 'cancelled'}.`, 'success');
        await loadSessions();
    } catch (error) {
        showMessage(error.message, 'error');
    }
}

function escapeHtml(str) {
    const div = document.createElement('div');
    div.textContent = str ?? '';
    return div.innerHTML;
}
