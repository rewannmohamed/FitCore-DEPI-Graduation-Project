// trainer-dashboard.js
const user = getCurrentUser();

document.addEventListener('DOMContentLoaded', init);

function setGreeting() {
    const hour = new Date().getHours();
    const label = hour < 12 ? 'Good morning' : (hour < 18 ? 'Good afternoon' : 'Good evening');
    document.getElementById('greetingText').textContent = `${label}, ${user.fullName} `;
}

async function init() {
    requireRole(["Trainer"]);
    setGreeting();
    await Promise.all([loadClasses(), loadPrivateSessions()]);
}

function toDateInput(date) { return date.toISOString().substring(0, 10); }

async function loadClasses() {
    const trainerName = user.fullName;
    const container = document.getElementById('classesList');
    try {
        const [classesData, occurrencesData] = await Promise.all([
            FitCoreApi.get('/api/Classes?Page=1&Page_Size=200'),
            loadWeeklyOccurrences(),
        ]);
        
        const allClasses = classesData.data || classesData.Data || [];
        const myClasses = allClasses.filter(c => pick(c, 'trainerName', 'trainerName') === trainerName);
        const myIds = new Set(myClasses.map(c => Number(pick(c, 'classID', 'ClassID'))));
        const myOccurrences = occurrencesData.filter(o => myIds.has(Number(pick(o, 'classID', 'ClassID'))));
        const totalStudents = myOccurrences.reduce((sum, o) => sum + (Number(pick(o, 'bookedCount', 'BookedCount')) || 0), 0);
       
        document.getElementById('assignedClassesValue').textContent = myClasses.length;
        document.getElementById('sessionsThisWeekValue').textContent = myOccurrences.length;
        document.getElementById('studentsBookedValue').textContent = totalStudents;

        if (myClasses.length === 0) {
            container.innerHTML = `<div class="text-muted small">You're not assigned to any classes yet.</div>`;
            return;
        }

        container.innerHTML = myClasses.slice(0, 5).map(c => {
            const name = pick(c, 'className', 'ClassName');
            const capacity = pick(c, 'capacity', 'Capacity');
            const status = Number(pick(c, 'status', 'Status'));
            return `
            <div class="d-flex justify-content-between align-items-center border-bottom py-2">
                <div>
                    <div class="fw-semibold small">${escapeHtml(name)}</div>
                    <div class="text-muted" style="font-size:12px;">Capacity: ${capacity}</div>
                </div>
                <span class="badge rounded-pill text-bg-${status === 1 ? 'success' : 'secondary'}">${status === 1 ? 'Active' : 'Inactive'}</span>
            </div>`;
        }).join('');
    } catch (error) {
        container.innerHTML = `<div class="text-danger small">Couldn't load classes.</div>`;
    }
}

async function loadWeeklyOccurrences() {
    const today = new Date();
    const weekEnd = new Date(today.getTime() + 6 * 24 * 60 * 60 * 1000);
    try {
        const data = await FitCoreApi.get(`/api/Classes/browse?fromDate=${toDateInput(today)}&toDate=${toDateInput(weekEnd)}&Page=1&Page_Size=500`);
        return data.data || data.Data || [];
    } catch {
        return [];
    }
}

async function loadPrivateSessions() {
    const container = document.getElementById('privateSessionsList');
    const TranierId = user.userId;
    try {
        const sessions = await FitCoreApi.get(`/api/PrivateSessions/trainer/${TranierId}`);
        const today = new Date();
        today.setHours(0, 0, 0, 0);

        console.log(sessions);

        const upcoming = (sessions || [])
            .filter(s => Number(pick(s, 'status', 'Status')) === 0 && new Date((pick(s, 'sessionDate', 'SessionDate') || '').substring(0, 10)) >= today)
            .sort((a, b) => new Date(pick(a, 'sessionDate', 'SessionDate')) - new Date(pick(b, 'sessionDate', 'SessionDate')));

        document.getElementById('upcomingPrivateSessionsValue').textContent = upcoming.length;

        if (upcoming.length === 0) {
            container.innerHTML = `<div class="text-muted small">No upcoming private sessions.</div>`;
            return;
        }

        container.innerHTML = upcoming.slice(0, 5).map(s => {
            const memberName = pick(s, 'memberName', 'MemberName') || `Member #${pick(s, 'memberUserId', 'MemberUserId')}`;
            const sessionDate = (pick(s, 'sessionDate', 'SessionDate') || '').toString().substring(0, 10);
            const start = (pick(s, 'startTime', 'StartTime') || '').toString().substring(0, 5);
            return `
            <div class="d-flex justify-content-between align-items-center border-bottom py-2">
                <div>
                    <div class="fw-semibold small">${escapeHtml(memberName)}</div>
                    <div class="text-muted" style="font-size:12px;">${sessionDate} • ${start}</div>
                </div>
                <i class='bx bx-chevron-right text-muted'></i>
            </div>`;
        }).join('');
    } catch (error) {
        container.innerHTML = `<div class="text-danger small">Couldn't load private sessions.</div>`;
    }
}

function escapeHtml(str) {
    const div = document.createElement('div');
    div.textContent = str ?? '';
    return div.innerHTML;
}
