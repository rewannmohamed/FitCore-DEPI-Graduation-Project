// trainers-management.js

const DAY_LABELS = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
const DAY_LABELS_FULL = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
const Roles = { 1: 'Trainer', 3: 'Receptionist' };

let allStaff = [];
let createStaffModal;
let trainerWorkingModal;
let trainers;
let receptionists;
let dataRoles;

document.addEventListener('DOMContentLoaded', () => {
    requireRole(["Admin"]);
    
    document.getElementById('adminRolesFilter').addEventListener('change', (e) => {
        loadTrainers();
    });

    loadTrainers();
    createStaffModal = bootstrap.Modal.getOrCreateInstance(document.getElementById('createStaffModal'));
    trainerWorkingModal = bootstrap.Modal.getOrCreateInstance(document.getElementById('trainerWorkingModal'));
    document.getElementById('createStaffBtn').addEventListener('click', createStaff);
    document.getElementById('staffRole').addEventListener('change', toggleTrainerOnlyFields);
    document.getElementById('addWorkingHourRowBtn').addEventListener('click', () => addWorkingHourRow());
    document.getElementById('saveWorkingHoursBtn').addEventListener('click', saveWorkingHours);
    document.getElementById('workingHoursTrainerSelect').addEventListener('change', loadWorkingHoursForSelected);
    document.getElementById('exportPdfBtn').addEventListener('click', () => window.print());
    toggleTrainerOnlyFields();
    addWorkingHourRow();
});

function showMessage(text, type) {
    const banner = document.getElementById('msgBanner');
    banner.textContent = text;
    banner.className = `msg-banner show ${type}`;
    setTimeout(() => banner.classList.remove('show'), 4000);
}

function toggleTrainerOnlyFields() {
    document.getElementById('trainerOnlyFields').style.display = document.getElementById('staffRole').value === '1' ? 'grid' : 'none';
}

async function createStaff() {
    const dto = {
        fullName: document.getElementById('staffFullName').value,
        email: document.getElementById('staffEmail').value,
        phoneNumber: document.getElementById('staffPhone').value,
        password: document.getElementById('staffPassword').value,
        role: parseInt(document.getElementById('staffRole').value, 10),
        specialization: document.getElementById('staffSpecialization').value,
        bio: document.getElementById('staffBio').value,
    };

    try {
        await FitCoreApi.post('/api/Trainers/staff', dto);
        showMessage('Profile created.', 'success');
        createStaffModal.hide();
        ['staffFullName', 'staffEmail', 'staffPhone', 'staffPassword', 'staffSpecialization', 'staffBio'].forEach(id => document.getElementById(id).value = '');
        await loadTrainers();
    } catch (error) {
        showMessage(error.message, 'error');
    }
}

async function loadTrainers() {
    const tbody = document.getElementById('trainersTableBody');
    tbody.innerHTML = `<tr class="state-row"><td colspan="5">Loading trainers…</td></tr>`;

    try {
        const data = await FitCoreApi.get('/api/Trainers/staff?Page_Size=50&Page=1');
        allStaff = data.data || data.Data || [];

        trainers = allStaff.filter(user => user.role === 1);
        receptionists = allStaff.filter(user => user.role === 3);

        const filterValue = document.getElementById('adminRolesFilter')?.value || '';

        if (filterValue === "1") {
            dataRoles = trainers;
        } else if (filterValue === "3") {
            dataRoles = receptionists;
        } else {
            dataRoles = allStaff; // "all" أو لو مفيش اختيار
        }
        
        populateTrainerSelect();
        renderTrainersTable();
    } catch (error) {
        tbody.innerHTML = `<tr class="state-row is-error"><td colspan="5">Couldn't load trainers: ${escapeHtml(error.message)}</td></tr>`;
    }
}

function populateTrainerSelect() {
    const select = document.getElementById('workingHoursTrainerSelect');
    const previous = select.value;
    select.innerHTML = trainers.map(t => {
        const id = pick(t, 'trainerID', 'TrainerID');
        const name = pick(t, 'fullName', 'FullName') || `Trainer #${id}`;
        return `<option value="${id}">${escapeHtml(`${id} ${name}`)}</option>`;
    }).join('');
    if (previous) select.value = previous;
    if (select.value) loadWorkingHoursForSelected();
}

function renderTrainersTable() {
    const tbody = document.getElementById('trainersTableBody');
    if (allStaff.length === 0) {
        tbody.innerHTML = `<tr class="state-row"><td colspan="5">No trainers yet.</td></tr>`;
        return;
    }
    console.log(dataRoles)
    tbody.innerHTML = dataRoles.map(t => {
        const id = pick(t, 'trainerID', 'userID');
        const userID = pick(t, 'userID', 'userID');
        const name = pick(t, 'fullName', 'FullName') || '—';
        const email = pick(t, 'email', 'Email') || '—';
        const spec = pick(t, 'specialization', 'Specialization') || '—';
        const role = pick(t, 'role', 'role') || '—';
        const hours = pick(t, 'workingHours', 'WorkingHours') || [];
        const hoursText = hours.length
            ? hours.map(h => `${DAY_LABELS[Number(pick(h, 'day', 'Day'))]} ${(pick(h, 'startTime', 'StartTime') || '').toString().substring(0, 5)}-${(pick(h, 'endTime', 'EndTime') || '').toString().substring(0, 5)}`).join(', ')
            : 'Not set';

        return `
        <tr>
            <td>#${id}</td>
            <td>${escapeHtml(name)}</td>
            <td>${escapeHtml(email)}</td>
            <td>${escapeHtml(Roles[role])}</td>
            <td>${escapeHtml(spec)}</td>
            <td>${escapeHtml(hoursText)}</td>
            <td class="text-end pe-4">
                 <button class="btn btn-sm text-danger border-0" onclick="deleteServiceAsset(${userID})"><i class='bx bx-trash fs-5'></i></button>
             </td>
        </tr>`;
    }).join('');
}

async function loadWorkingHoursForSelected() {
    const trainerId = document.getElementById('workingHoursTrainerSelect').value;
    if (!trainerId) return;

    document.getElementById('workingHoursRows').innerHTML = '';
    try {
        const hours = await FitCoreApi.get(`/api/Trainers/${trainerId}/working-hours`);
        if (!hours || hours.length === 0) {
            addWorkingHourRow();
        } else {
            hours.forEach(h => addWorkingHourRow(
                Number(pick(h, 'day', 'Day')),
                (pick(h, 'startTime', 'StartTime') || '').toString().substring(0, 5),
                (pick(h, 'endTime', 'EndTime') || '').toString().substring(0, 5),
            ));
        }
    } catch (error) {
        showMessage(error.message, 'error');
    }
}

function addWorkingHourRow(day, start, end) {
    const container = document.getElementById('workingHoursRows');
    const row = document.createElement('div');
    row.className = 'schedule-row';
    const dayOptions = DAY_LABELS_FULL.map((label, value) => `<option value="${value}" ${value === day ? 'selected' : ''}>${label}</option>`).join('');

    row.innerHTML = `
        <div class="form-group"><label>Day</label><select class="wh-day">${dayOptions}</select></div>
        <div class="form-group"><label>Start</label><input type="time" class="text-input wh-start" value="${start || '09:00'}"></div>
        <div class="form-group"><label>End</label><input type="time" class="text-input wh-end" value="${end || '17:00'}"></div>
        <button class="btn-outline btn-sm remove-row">Remove</button>
    `;
    row.querySelector('.remove-row').addEventListener('click', () => row.remove());
    container.appendChild(row);
}

async function saveWorkingHours() {
    const trainerId = document.getElementById('workingHoursTrainerSelect').value;
    if (!trainerId) { showMessage('Select a trainer first.', 'error'); return; }

    const rows = document.querySelectorAll('#workingHoursRows .schedule-row');
    const workingHours = Array.from(rows).map(row => ({
        day: parseInt(row.querySelector('.wh-day').value, 10),
        startTime: row.querySelector('.wh-start').value + ':00',
        endTime: row.querySelector('.wh-end').value + ':00',
    }));

    try {
        console.log(workingHours);
        const res = await FitCoreApi.put(`/api/Trainers/${trainerId}/working-hours`, { workingHours });
        showMessage('Working hours saved.', 'success');
        console.log(res);
        trainerWorkingModal.hide();
        await loadTrainers();
    } catch (error) {
        showMessage(error.message, 'error');
    }
}

function escapeHtml(str) {
    const div = document.createElement('div');
    div.textContent = str ?? '';
    return div.innerHTML;
}

async function deleteServiceAsset(userID) {

    try {

        await FitCoreApi.delete(`/api/Trainers/staff/${userID}`);

        await loadTrainers();

        showMessage('Staff deleted successfully.', 'success');

    } catch (err) {
        showMessage(err.message, "error");
    }
}