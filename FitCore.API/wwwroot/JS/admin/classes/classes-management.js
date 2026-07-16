// classes-management.js

const CLASS_ICONS = [
    { match: /hiit|sprint|inferno/i, icon: 'bx-bolt' },
    { match: /yoga|flow|yin|zen/i, icon: 'bx-leaf' },
    { match: /spin|cycle|cycling/i, icon: 'bx-cycling' },
    { match: /lift|strength|power|core/i, icon: 'bx-dumbbell' },
    { match: /aqua|swim|pool/i, icon: 'bx-water' },
];
const DAY_LABELS = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];

let allClasses = [];
let allTrainers = [];
let weeklyOccurrences = [];
let currentPage = 1;
const PAGE_SIZE = 5;

let editClassId = null;
let editClassModal;

let filters = { trainerId: '', type: '', status: '' };
let createClassModal;

document.addEventListener('DOMContentLoaded', () => {
    requireRole(["Admin"]);
    createClassModal = new bootstrap.Modal(document.getElementById('createClassModal'));
    editClassModal = new bootstrap.Modal(
        document.getElementById('editClassModal')
    );

    loadTrainers();
    loadClasses();
 
    document.getElementById('submitCreateClassBtn').addEventListener('click', submitCreateClass);
    document.getElementById('submitEditClassBtn').addEventListener('click', submitEditClass);
    document.getElementById('addScheduleRowBtn').addEventListener('click', () => addScheduleRow());
    document.getElementById('exportPdfBtn').addEventListener('click', () => window.print());

    document.getElementById('trainerFilter').addEventListener('change', (e) => { filters.trainerId = e.target.value; currentPage = 1; renderTable(); });
    document.getElementById('typeFilter').addEventListener('change', (e) => { filters.type = e.target.value; currentPage = 1; renderTable(); });
    document.getElementById('statusFilter').addEventListener('change', (e) => { filters.status = e.target.value; currentPage = 1; renderTable(); });

    addScheduleRow();
});

function showMessage(text, type) {
    const banner = document.getElementById('msgBanner');
    banner.textContent = text;
    banner.className = `alert alert-${type === 'success' ? 'success' : 'danger'}`;
    setTimeout(() => banner.classList.add('d-none'), 4000);
}

function classIconFor(name) {
    const found = CLASS_ICONS.find(c => c.match.test(name));
    return found ? found.icon : 'bx-body';
}

/* ---------------- Data loading ---------------- */

async function loadTrainers() {
    try {
        const data = await FitCoreApi.get('/api/Trainers?Page=1&Page_Size=50');
        allTrainers = data.data || data.Data || [];

        const filterSelect = document.getElementById('trainerFilter');
        const modalSelect = document.getElementById('classTrainerSelect');
        const options = allTrainers.map(t => {
            const id = pick(t, 'trainerID', 'trainerID');
            const name = pick(t, 'fullName', 'FullName') || `Trainer #${id}`;
            return { id, name };
        });
        
        filterSelect.innerHTML = '<option value="">All Trainers</option>' + options.map(o => `<option value="${o.id}">${escapeHtml(o.name)}</option>`).join('');
        modalSelect.innerHTML = options.map(o => `<option value="${o.id}">${escapeHtml(o.name)}</option>`).join('');
    } catch (error) {
        console.error(error);
    }
}

async function loadClasses() {
    const tbody = document.getElementById('classesTableBody');
    tbody.innerHTML = `<tr><td colspan="6" class="text-center text-muted py-5">Loading classes…</td></tr>`;

    try {
        const data = await FitCoreApi.get(`/api/Classes?Page=1&Page_Size=100`);
        console.log(data);
        allClasses = data.data || data.Data || [];
      
        await loadWeeklyOccurrences();
        renderTable();
        renderStats();
    } catch (error) {
        tbody.innerHTML = `<tr><td colspan="6" class="text-center text-danger py-5">Couldn't load classes: ${escapeHtml(error.message)}</td></tr>`;
    }
}

async function loadWeeklyOccurrences() {
    const today = new Date();
    const weekEnd = new Date(today.getTime() + 6 * 24 * 60 * 60 * 1000);
    const from = toDateInput(today);
    const to = toDateInput(weekEnd);

    try {
        const data = await FitCoreApi.get(`/api/Classes/browse?fromDate=${from}&toDate=${to}&Page=1&Page_Size=100`);
        weeklyOccurrences = data.data || data.Data || [];
    } catch (error) {
        weeklyOccurrences = [];
        console.warn('Could not load weekly occurrences for capacity stats:', error);
    }
}

function toDateInput(date) { return date.toISOString().substring(0, 10); }

/* ---------------- Table rendering ---------------- */

function getFilteredClasses() {
    return allClasses.filter(c => {
        const trainerId = pick(c, 'trainerID', 'TrainerID');
        const status = pick(c, 'status', 'Status');
        const name = (pick(c, 'className', 'ClassName') || '').toLowerCase();

        if (filters.trainerId && String(trainerId) !== filters.trainerId) return false;
        if (filters.status !== '' && String(status) !== filters.status) return false;
        if (filters.type && !name.includes(filters.type)) return false;
        return true;
    });
}

function renderTable() {
    const tbody = document.getElementById('classesTableBody');
    const filtered = getFilteredClasses();

    if (filtered.length === 0) {
        tbody.innerHTML = `<tr><td colspan="6" class="text-center text-muted py-5">No classes match your filters.</td></tr>`;
        document.getElementById('paginationSummary').textContent = '';
        document.getElementById('paginationControls').innerHTML = '';
        return;
    }

    const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
    if (currentPage > totalPages) currentPage = totalPages;
    const pageItems = filtered.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);
    console.log(totalPages);
    tbody.innerHTML = pageItems.map(renderRow).join('');

    document.getElementById('paginationSummary').textContent =
        `Showing ${(currentPage - 1) * PAGE_SIZE + 1}-${Math.min(currentPage * PAGE_SIZE, filtered.length)} of ${filtered.length} classes`;

    renderPagination(totalPages);
    wireRowActions();
}

function weeklyCapacityFor(classId) {
    const matches = weeklyOccurrences.filter(o => Number(pick(o, 'classID', 'ClassID')) === Number(classId));
    if (matches.length === 0) return null;

    const booked = matches.reduce((sum, o) => sum + (Number(pick(o, 'bookedCount', 'BookedCount')) || 0), 0);
    const capacity = matches.reduce((sum, o) => sum + (Number(pick(o, 'capacity', 'Capacity')) || 0), 0);
    return { booked, capacity, percent: capacity > 0 ? Math.round((booked / capacity) * 100) : 0 };
}

function renderRow(c) {
    const id = pick(c, 'classID', 'ClassID');
    const name = pick(c, 'className', 'ClassName') || 'Untitled Class';
    const description = pick(c, 'description', 'Description') || '';
    const trainerName = pick(c, 'trainerName', 'TrainerName') || '—';
    const trainerId = pick(c, 'trainerID', 'TrainerID');
    const status = Number(pick(c, 'status', 'Status'));
    const NumberOfSessions = Number(pick(c, 'numberOfSessions', 'NumberOfSessions'));
    const schedules = pick(c, 'schedules', 'Schedules') || [];
    const price = Number(pick(c, 'price', 'price'));
    
    const scheduleText = schedules.length ? scheduleSummary(schedules) : { time: 'No time slots', days: '' };
    const capacity = weeklyCapacityFor(id);
    const barVariant = capacity && capacity.percent >= 90 ? 'bg-danger' : (capacity && capacity.percent >= 70 ? 'bg-warning' : 'bg-primary');

    const trainerOptions = allTrainers.map(t => {
        const tId = pick(t, 'trainerID', 'TrainerID');
        const tName = pick(t, 'fullName', 'FullName') || `Trainer #${tId}`;
        return `<option value="${tId}" ${tId === trainerId ? 'selected' : ''}>${escapeHtml(`#${tId} ${tName} `)}</option>`;
    }).join('');

    return `
    <tr data-class-id="${id}">
        <td>
            <div class="d-flex align-items-center gap-2">
                <span class="class-icon-chip"><i class='bx ${classIconFor(name)}'></i></span>
                <div>
                    <div class="fw-bold">${escapeHtml(name)}</div>
                    <div class="text-muted fs-xs">${escapeHtml(description)}</div>
                </div>
            </div>
        </td>
        <td>
            <div class="d-flex align-items-center gap-2">
                <span class="trainer-avatar-chip">${initials(trainerName)}</span>
                ${escapeHtml(trainerName)}
            </div>
        </td>
        <td>
            <div class="fw-semibold">${escapeHtml(scheduleText.time)}</div>
            <div class="text-muted small">${escapeHtml(scheduleText.days)}</div>
        </td>
        <td>
            ${capacity ? `
                <div class="d-flex justify-content-between small fw-semibold mb-1">
                    <span>${capacity.booked} / ${capacity.capacity}</span><span>${capacity.percent}%</span>
                </div>
                <div class="progress" style="height:6px;">
                    <div class="progress-bar ${barVariant}" style="width:${Math.min(capacity.percent, 100)}%"></div>
                </div>
            ` : `<span class="capacity-empty-text">No sessions this week</span>`}
        </td>
        <td><span class="badge rounded-pill text-bg-${status === 1 ? 'success' : 'secondary'}">${status === 1 ? 'Active' : 'Inactive'}</span></td>
         <td><span class=""> ${price} EGP</span></td>
        <td><span class="">${NumberOfSessions === 0 ? 1 : NumberOfSessions} Session</span></td>
        <td class="text-end">
            <div class="dropdown">
                <button class="btn btn-sm btn-outline-secondary" type="button" data-bs-toggle="dropdown" aria-expanded="false">
                    <i class='bx bx-dots-vertical-rounded'></i>
                </button>
                <div class="dropdown-menu dropdown-menu-end p-3" style="min-width:240px;">
                    <label class="form-label small fw-semibold">Reassign trainer</label>
                    <select class="form-select form-select-sm mb-2" data-reassign-select="${id}">${trainerOptions}</select>
                    <button class="btn btn-outline-primary btn-sm w-100 mb-2" data-reassign-btn="${id}">Assign</button>
                    <button class="btn btn-outline-success btn-sm w-100 mb-2" data-bs-toggle="modal" data-bs-target="#editClassModal" data-edit-btn="${id}">edit</button>
                    <button class="btn btn-outline-secondary btn-sm w-100" data-toggle-status="${id}">
                        ${status === 1 ? 'Deactivate class' : 'Activate class'}
                    </button>
                   <button class="btn btn-sm text-danger  w-100 border-danger" onclick="deleteServiceAsset(${id})"><i class='bx bx-trash fs-5'></i></button> 
                </div>
            </div>
        </td>
    </tr>`;
}

function scheduleSummary(schedules) {
    const first = schedules[0];
    const start = (pick(first, 'startTime', 'StartTime') || '').toString().substring(0, 5);
    const end = (pick(first, 'endTime', 'EndTime') || '').toString().substring(0, 5);
    const days = schedules.map(s => DAY_LABELS[Number(pick(s, 'day', 'Day'))]).join(', ');
    return { time: `${start} - ${end}`, days };
}

function wireRowActions() {
    document.querySelectorAll('[data-edit-btn]').forEach(btn => {

        btn.addEventListener('click', () => {

            editClassId = btn.dataset.editBtn;

            const gymClass = allClasses.find(c =>
                Number(pick(c, 'classID', 'ClassID')) === Number(editClassId)
            );

            if (!gymClass) return;


            document.getElementById('editClassName').value =
                pick(gymClass, 'className', 'ClassName') || '';

            document.getElementById('editClassDescription').value =
                pick(gymClass, 'description', 'Description') || '';

            document.getElementById('editClassCapacity').value =
                pick(gymClass, 'capacity', 'Capacity') || 0;

            document.getElementById('editNumSessions').value =
                pick(gymClass, 'numberOfSessions', 'NumberOfSessions') || 1;

            document.getElementById('editClassStatus').value =
                pick(gymClass, 'status', 'Status') ?? 1;

            document.getElementById('editPrice').value =
                pick(gymClass, 'price', 'Price') ?? 1000;

        });

    });

    document.querySelectorAll('[data-reassign-btn]').forEach(btn => {
        btn.addEventListener('click', async () => {
            
            const classId = btn.dataset.reassignBtn;
            const select = document.querySelector(`[data-reassign-select="${classId}"]`);
            console.log(select.value);
            try {
                await FitCoreApi.put(`/api/Trainers/${select.value}/assign-class/${classId}`);
                showMessage('Trainer reassigned.', 'success');
                await loadClasses();
            } catch (error) {
                showMessage(error.message, 'error');
            }
        });
    });

    document.querySelectorAll('[data-toggle-status]').forEach(btn => {
        btn.addEventListener('click', async () => {
            const classId = btn.dataset.toggleStatus;
            const gymClass = allClasses.find(c => Number(pick(c, 'classID', 'ClassID')) === Number(classId));
            if (!gymClass) return;

            const newStatus = Number(pick(gymClass, 'status', 'Status')) === 1 ? 0 : 1;
            try {
                await FitCoreApi.put(`/api/Classes/${classId}`, {
                    className: pick(gymClass, 'className', 'ClassName'),
                    description: pick(gymClass, 'description', 'Description'),
                    capacity: pick(gymClass, 'capacity', 'Capacity'),
                    numberOfSessions: Number(pick(gymClass, 'numberOfSessions', 'NumberOfSessions')) || 1,
                    price: Number(pick(gymClass, 'price', 'Price')) || 0,
                    status: newStatus,
                });
                showMessage('Class status updated.', 'success');
                await loadClasses();
            } catch (error) {
                showMessage(error.message, 'error');
            }
        });
    });
}

function renderPagination(totalPages) {
    const container = document.getElementById('paginationControls');
    container.innerHTML = '';
    for (let i = 1; i <= totalPages; i++) {
        const li = document.createElement('li');
        li.className = `page-item ${i === currentPage ? 'active' : ''}`;
        li.innerHTML = `<button class="page-link">${i}</button>`;
        li.querySelector('button').addEventListener('click', () => { currentPage = i; renderTable(); });
        container.appendChild(li);
    }
}

/* ---------------- Stats ---------------- */

function renderStats() {
    const totalBooked = weeklyOccurrences.reduce((sum, o) => sum + (Number(pick(o, 'bookedCount', 'BookedCount')) || 0), 0);
    const totalCapacity = weeklyOccurrences.reduce((sum, o) => sum + (Number(pick(o, 'capacity', 'Capacity')) || 0), 0);
    const occupancy = totalCapacity > 0 ? ((totalBooked / totalCapacity) * 100).toFixed(1) : '0.0';

    document.getElementById('liveOccupancyValue').textContent = `${occupancy}%`;
    document.getElementById('activeBookingsValue').textContent = totalBooked.toLocaleString();
    document.getElementById('classesThisWeekValue').textContent = weeklyOccurrences.length.toLocaleString();

    renderPeakHourAlert();
}

function renderPeakHourAlert() {
    const groups = {};
    weeklyOccurrences.forEach(o => {
        const key = `${pick(o, 'startTime', 'StartTime')}`;
        if (!groups[key]) groups[key] = { booked: 0, capacity: 0, count: 0 };
        groups[key].booked += Number(pick(o, 'bookedCount', 'BookedCount')) || 0;
        groups[key].capacity += Number(pick(o, 'capacity', 'Capacity')) || 0;
        groups[key].count += 1;
    });

    let peak = null;
    for (const [time, g] of Object.entries(groups)) {
        if (g.capacity === 0) continue;
        const percent = (g.booked / g.capacity) * 100;
        if (!peak || percent > peak.percent) peak = { time, percent, count: g.count };
    }

    const card = document.getElementById('peakHourCard');
    if (peak && peak.percent >= 90) {
        document.getElementById('peakHourText').textContent =
            `${peak.time.substring(0, 5)} classes are averaging ${peak.percent.toFixed(0)}% capacity across ${peak.count} session(s) this week.`;
        card.classList.remove('d-none');
    } else {
        card.classList.add('d-none');
    }
}

/* ---------------- Create class modal ---------------- */

function addScheduleRow(day, start, end) {
    const container = document.getElementById('scheduleRows');
    const row = document.createElement('div');
    row.className = 'row g-2 align-items-end mb-2';
    const dayOptions = DAY_LABELS.map((label, value) => `<option value="${value}" ${value === day ? 'selected' : ''}>${label}</option>`).join('');

    row.innerHTML = `
        <div class="col"><label class="form-label small">Day</label><select class="form-select form-select-sm cs-day">${dayOptions}</select></div>
        <div class="col"><label class="form-label small">Start</label><input type="time" class="form-control form-control-sm cs-start" value="${start || '09:00'}"></div>
        <div class="col"><label class="form-label small">End</label><input type="time" class="form-control form-control-sm cs-end" value="${end || '10:00'}"></div>
        <div class="col-auto"><button class="btn btn-outline-secondary btn-sm remove-row" type="button">Remove</button></div>
    `;
    row.querySelector('.remove-row').addEventListener('click', () => row.remove());
    container.appendChild(row);
}

async function submitCreateClass() {
    const rows = document.querySelectorAll('#scheduleRows .row');
    const schedules = Array.from(rows).map(row => ({
        day: parseInt(row.querySelector('.cs-day').value, 10),
        startTime: row.querySelector('.cs-start').value + ':00',
        endTime: row.querySelector('.cs-end').value + ':00',
    }));

    const dto = {
        className: document.getElementById('className').value,
        description: document.getElementById('classDescription').value,
        capacity: parseInt(document.getElementById('classCapacity').value, 10) || 0,
        numberOfSessions: parseInt(document.getElementById('numSessions').value, 10) || 0, 
        price: parseFloat(document.getElementById('Price').value) || 0,                     
        trainerID: parseInt(document.getElementById('classTrainerSelect').value, 10),
        schedules,
    };

    try {
        await FitCoreApi.post('/api/Classes', dto);
        showMessage('Class created.', 'success');
        createClassModal.hide();
        document.getElementById('className').value = '';
        document.getElementById('classDescription').value = '';
        document.getElementById('scheduleRows').innerHTML = '';
        addScheduleRow();
        await loadClasses();
    } catch (error) {
        showMessage(error.message, 'error');
    }
}

async function submitEditClass() {
    if (!editClassId) {
        showMessage('No class selected', 'error');
        return;
    }

    const dto = {
        className: document.getElementById('editClassName').value,
        description: document.getElementById('editClassDescription').value,
        capacity: parseInt(document.getElementById('editClassCapacity').value, 10) || 0,
        numberOfSessions: parseInt(document.getElementById('editNumSessions').value, 10) || 1,
        status: Number(document.getElementById('editClassStatus').value),
        price: parseFloat(document.getElementById('editPrice').value) || 0                     
    };

    try {
        await FitCoreApi.put(`/api/Classes/${editClassId}`, dto);
        showMessage('Class updated successfully.', 'success');
        editClassModal.hide();
        editClassId = null;
        await loadClasses();
    } catch (error) {
        showMessage(error.message, 'error');
    }
}

function initials(name) {
    return name.split(' ').map(p => p[0]).join('').substring(0, 2).toUpperCase();
}

function escapeHtml(str) {
    const div = document.createElement('div');
    div.textContent = str ?? '';
    return div.innerHTML;
}

async function deleteServiceAsset(id) {

    try {

        await FitCoreApi.delete(`/api/Classes/${id}`);

        await loadClasses();

        showMessage('Class deleted successfully.', 'success');

    } catch (err) {
        showMessage(err.message, "error");
    }
}
