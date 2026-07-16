// receptionist-dashboard.js

document.addEventListener('DOMContentLoaded', init);

function toDateInput(date) { return date.toISOString().substring(0, 10); }

async function init() {
    requireRole(["Receptionist"]);
    const today = new Date();
    document.getElementById('todayLabel').textContent =
        `Today's operations at a glance — ${today.toLocaleDateString(undefined, { weekday: 'long', month: 'long', day: 'numeric' })}.`;

    await Promise.all([loadTodayClasses(), loadTodayPrivateSessions()]);
}

async function loadTodayClasses() {
    const tbody = document.getElementById('todayClassesBody');
    const today = toDateInput(new Date());

    try {
        const data = await FitCoreApi.get(`/api/Classes/browse?fromDate=${today}&toDate=${today}&Page=1&Page_Size=200`);
        const occurrences = data.data || data.Data || [];

        document.getElementById('classesTodayValue').textContent = occurrences.length;
        document.getElementById('expectedCheckinsValue').textContent =
            occurrences.reduce((sum, o) => sum + (Number(pick(o, 'bookedCount', 'BookedCount')) || 0), 0);

        if (occurrences.length === 0) {
            tbody.innerHTML = `<tr><td colspan="4" class="text-center text-muted py-3">No classes scheduled today.</td></tr>`;
            return;
        }

        tbody.innerHTML = occurrences.map(o => {
            const name = pick(o, 'className', 'ClassName');
            const start = (pick(o, 'startTime', 'StartTime') || '').toString().substring(0, 5);
            const end = (pick(o, 'endTime', 'EndTime') || '').toString().substring(0, 5);
            const trainerName = pick(o, 'trainerName', 'TrainerName') || '—';
            const booked = pick(o, 'bookedCount', 'BookedCount');
            const capacity = pick(o, 'capacity', 'Capacity');
            return `<tr><td class="fw-semibold">${escapeHtml(name)}</td><td>${start} - ${end}</td><td>${escapeHtml(trainerName)}</td><td>${booked}/${capacity}</td></tr>`;
        }).join('');
    } catch (error) {
        tbody.innerHTML = `<tr><td colspan="4" class="text-center text-danger py-3">Couldn't load today's classes.</td></tr>`;
    }
}

async function loadTodayPrivateSessions() {
    try {
        const trainersData = await FitCoreApi.get('/api/Trainers?Page=1&Page_Size=50');
        const trainers = trainersData.data || trainersData.Data || [];
        const today = toDateInput(new Date());

        const results = await Promise.all(trainers.map(async t => {
            const id = pick(t, 'userID', 'userID');
            try {
                const sessions = await FitCoreApi.get(`/api/PrivateSessions/trainer/${id}`);
                return sessions || [];
            } catch { return []; }
        }));

        const todaySessions = results.flat().filter(s => (pick(s, 'sessionDate', 'SessionDate') || '').toString().substring(0, 10) === today);
        document.getElementById('privateSessionsTodayValue').textContent = todaySessions.length;
    } catch (error) {
        document.getElementById('privateSessionsTodayValue').textContent = '—';
    }
}

function escapeHtml(str) {
    const div = document.createElement('div');
    div.textContent = str ?? '';
    return div.innerHTML;
}
