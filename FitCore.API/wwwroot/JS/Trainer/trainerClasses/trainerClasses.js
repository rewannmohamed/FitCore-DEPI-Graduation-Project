// trainer-classes.js

const DAY_LABELS = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
const user = getCurrentUser();
document.addEventListener('DOMContentLoaded', loadMyClasses);

function showMessage(text, type) {
    const banner = document.getElementById('msgBanner');
    banner.textContent = text;
    banner.className = `msg-banner show ${type}`;
    setTimeout(() => banner.classList.remove('show'), 4000);
}

async function loadMyClasses() {
    requireRole(["Trainer"]);
    const trainerName = user.fullName;
    const grid = document.getElementById('classesGrid');
    grid.innerHTML = `<div class="state-empty">Loading your classes…</div>`;

    try {
        const [classesData, occurrencesData] = await Promise.all([
            FitCoreApi.get('/api/Classes?Page=1&Page_Size=200'),
            loadWeeklyOccurrences(),
        ]);

        const allClasses = classesData.data || classesData.Data || [];
        // const myClasses = allClasses.filter(c => Number(pick(c, 'trainerID', 'TrainerID')) === Number(user.userId));
        const myClasses = allClasses.filter(c => pick(c, 'trainerName', 'trainerName') === trainerName);

        renderStats(myClasses, occurrencesData);
        renderGrid(myClasses, occurrencesData);
    } catch (error) {
        grid.innerHTML = `<div class="state-empty">Couldn't load your classes: ${escapeHtml(error.message)}</div>`;
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

function toDateInput(date) { return date.toISOString().substring(0, 10); }

function renderStats(myClasses, occurrences) {
    const myIds = new Set(myClasses.map(c => Number(pick(c, 'classID', 'ClassID'))));
    const myOccurrences = occurrences.filter(o => myIds.has(Number(pick(o, 'classID', 'ClassID'))));
    const totalStudents = myOccurrences.reduce((sum, o) => sum + (Number(pick(o, 'bookedCount', 'BookedCount')) || 0), 0);

    document.getElementById('assignedClassesValue').textContent = myClasses.length;
    document.getElementById('sessionsThisWeekValue').textContent = myOccurrences.length;
    document.getElementById('studentsBookedValue').textContent = totalStudents;
}

function renderGrid(myClasses, occurrences) {
    const grid = document.getElementById('classesGrid');

    if (myClasses.length === 0) {
        grid.innerHTML = `<div class="state-empty">You're not assigned to any classes yet.</div>`;
        return;
    }

    grid.innerHTML = myClasses.map(c => renderCard(c, occurrences)).join('');
}

function renderCard(c, occurrences) {
    const id = pick(c, 'classID', 'ClassID');
    const name = pick(c, 'className', 'ClassName');
    const description = pick(c, 'description', 'Description') || '';
    const status = Number(pick(c, 'status', 'Status'));
    const capacity = Number(pick(c, 'capacity', 'Capacity'));
    const schedules = pick(c, 'schedules', 'Schedules') || [];

    const classOccurrences = occurrences.filter(o => Number(pick(o, 'classID', 'ClassID')) === Number(id));
    const booked = classOccurrences.reduce((sum, o) => sum + (Number(pick(o, 'bookedCount', 'BookedCount')) || 0), 0);
    const totalCapacity = classOccurrences.reduce((sum, o) => sum + (Number(pick(o, 'capacity', 'Capacity')) || 0), 0);
    const percent = totalCapacity > 0 ? Math.round((booked / totalCapacity) * 100) : 0;

    return `
    <div class="class-card shadow">
        <div class="class-card-top">
            <div class="class-card-icon"><i class='bx bx-dumbbell'></i></div>
            <span class="pill ${status === 1 ? 'active' : 'inactive'}">${status === 1 ? 'Active' : 'Inactive'}</span>
        </div>
        <h3>${escapeHtml(name)}</h3>
        <div class="desc">${escapeHtml(description)}</div>

        <ul class="class-schedule-list">
            ${schedules.map(s => `<li><span>${DAY_LABELS[Number(pick(s, 'day', 'Day'))]}</span><span>${(pick(s, 'startTime', 'StartTime') || '').toString().substring(0, 5)} - ${(pick(s, 'endTime', 'EndTime') || '').toString().substring(0, 5)}</span></li>`).join('') || '<li>No time slots defined</li>'}
        </ul>

        ${classOccurrences.length > 0 ? `
            <div class="capacity-numbers"><span>${booked} / ${totalCapacity} booked this week</span><span>${percent}%</span></div>
            <div class="capacity-bar-track"><div class="capacity-bar-fill ${percent >= 90 ? 'high' : ''}" style="width:${Math.min(percent, 100)}%"></div></div>
        ` : `<div class="capacity-numbers"><span>No sessions this week</span></div>`}
    </div>`;
}

function escapeHtml(str) {
    const div = document.createElement('div');
    div.textContent = str ?? '';
    return div.innerHTML;
}
