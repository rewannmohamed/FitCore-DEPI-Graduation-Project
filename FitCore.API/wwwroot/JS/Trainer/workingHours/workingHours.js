// working-hours.js

const DAY_LABELS = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
const user = getCurrentUser();
document.addEventListener('DOMContentLoaded', () => {
    requireRole(["Trainer"]);
    loadHours();
    document.getElementById('addRowBtn').addEventListener('click', () => addRow());
    document.getElementById('saveBtn').addEventListener('click', save);
});

function showMessage(text, type) {
    const banner = document.getElementById('msgBanner');
    banner.textContent = text;
    banner.className = `msg-banner show ${type}`;
    setTimeout(() => banner.classList.remove('show'), 4000);
}

async function loadHours() {
    try {
        console.log(user.userId);
        const hours = await FitCoreApi.get(`/api/Trainers/${user.userId}/working-hours`);
        document.getElementById('hoursRows').innerHTML = '';
        if (!hours || hours.length === 0) {
            addRow();
        } else {
            hours.forEach(h => addRow(
                Number(pick(h, 'day', 'Day')),
                (pick(h, 'startTime', 'StartTime') || '').toString().substring(0, 5),
                (pick(h, 'endTime', 'EndTime') || '').toString().substring(0, 5),
            ));
        }
    } catch (error) {
        showMessage(`Couldn't load working hours: ${error.message}`, 'error');
        addRow();
    }
}

function addRow(day, start, end) {
    const container = document.getElementById('hoursRows');
    const row = document.createElement('div');
    row.className = 'schedule-row';
    const dayOptions = DAY_LABELS.map((label, value) => `<option value="${value}" ${value === day ? 'selected' : ''}>${label}</option>`).join('');

    row.innerHTML = `
        <div class="form-group"><label>Day</label><select class="wh-day">${dayOptions}</select></div>
        <div class="form-group"><label>Start Time</label><input type="time" class="text-input wh-start" value="${start || '09:00'}"></div>
        <div class="form-group"><label>End Time</label><input type="time" class="text-input wh-end" value="${end || '17:00'}"></div>
        <button class="btn-outline btn-sm remove-row">Remove</button>
    `;
    row.querySelector('.remove-row').addEventListener('click', () => row.remove());
    container.appendChild(row);
}

async function save() {
    const rows = document.querySelectorAll('#hoursRows .schedule-row');
    const workingHours = Array.from(rows).map(row => ({
        day: parseInt(row.querySelector('.wh-day').value, 10),
        startTime: row.querySelector('.wh-start').value + ':00',
        endTime: row.querySelector('.wh-end').value + ':00',
    }));

    try {

        await FitCoreApi.put(`/api/Trainers/${user.userId}/working-hours`, { workingHours });
        showMessage('Working hours saved.', 'success');
    } catch (error) {
        showMessage(error.message, 'error');
    }
}
