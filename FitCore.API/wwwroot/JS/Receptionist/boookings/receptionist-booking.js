// receptionist-booking.js

const STATUS_LABELS = ['booked', 'cancelled', 'attended', 'noshow'];
const STATUS_BADGE = { booked: 'primary', cancelled: 'secondary', attended: 'success', noshow: 'danger' };
const STATUS_SERVICE = { Booked: 'primary', Cancelled: 'secondary', Attended: 'success', Noshow: 'danger' };
let currentMemberUserId = null;
let allClasses = [];
let allServices = [];

document.addEventListener('DOMContentLoaded', () => {
    requireRole(["Receptionist"]);
    document.getElementById('loadMemberBtn').addEventListener('click', loadMember);

    document.querySelectorAll('#bookingTabs button').forEach(btn => {
        btn.addEventListener('click', () => {
            document.querySelectorAll('#bookingTabs button').forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            document.getElementById('classesTab').style.display = btn.dataset.tab === 'classes' ? 'block' : 'none';
            document.getElementById('servicesTab').style.display = btn.dataset.tab === 'services' ? 'block' : 'none';
            document.getElementById('historyTab').style.display = btn.dataset.tab === 'history' ? 'block' : 'none';
            if (btn.dataset.tab === 'history' && currentMemberUserId) loadMemberHistory();
        });
    });
    init();
    loadClasses();
    loadServices();
});


async function init() {
    try {
        const allMembers = await FitCoreApi.get('/api/Auth/users');
        const membersOnly = allMembers.filter(user => user.roles.includes('Member'));


        const Memberoptions = membersOnly.map(t => {
            const id = pick(t, 'userID', 'userID');
            const name = pick(t, 'fullName', 'FullName') || `Member #${id}`;
            return { id, name };
        });

        document.getElementById('memberSelect').innerHTML = Memberoptions.map(o => `<option value="${o.id}">${escapeHtml(o.name)}</option>`).join('');

    } catch (error) {
        showMessage(`Couldn't load trainers: ${error.message}`, 'error');
    }
}

function showMessage(text, type) {
    const banner = document.getElementById('msgBanner');
    banner.textContent = text;
    banner.className = `alert alert-${type === 'success' ? 'success' : 'danger'}`;
}

function loadMember() {
    const value = parseInt(document.getElementById('memberSelect').value, 10);
    if (!value) {
        showMessage('Enter a valid Member User ID first.', 'error');
        return;
    }
    currentMemberUserId = value;
    document.getElementById('memberSummary').textContent = `Booking for Member #${value}`;
    renderClassesTable();
    renderServicesTable();
    loadMemberHistory();
}

async function loadClasses() {
    try {
        const data = await FitCoreApi.get('/api/Classes?Page=1&Page_Size=200');
        allClasses = (data.data || data.Data || []).filter(c => Number(pick(c, 'status', 'Status')) === 1);
        renderClassesTable();
    } catch (error) {
        document.getElementById('classesTableBody').innerHTML = `<tr><td colspan="5" class="text-center text-danger py-4">Couldn't load classes: ${escapeHtml(error.message)}</td></tr>`;
    }
}

async function loadServices() {
    try {
        const data = await FitCoreApi.get('/api/GymServices?page=1&pageSize=7');
        allServices = Array.isArray(data) ? data : (data.data || data.Data || []);
        renderServicesTable();
    } catch (error) {
        document.getElementById('servicesTableBody').innerHTML = `<tr><td colspan="5" class="text-center text-danger py-4">Couldn't load gym services: ${escapeHtml(error.message)}</td></tr>`;
    }
}

function renderClassesTable() {
    const tbody = document.getElementById('classesTableBody');
    if (allClasses.length === 0) {
        tbody.innerHTML = `<tr><td colspan="5" class="text-center text-muted py-4">No active classes found.</td></tr>`;
        return;
    }

    tbody.innerHTML = allClasses.map(c => {
        const id = pick(c, 'classID', 'ClassID');
        const name = pick(c, 'className', 'ClassName');
        const trainerName = pick(c, 'trainerName', 'TrainerName') || '—';
        const capacity = pick(c, 'capacity', 'Capacity');
        return `
        <tr>
            <td class="fw-semibold">${escapeHtml(name)}</td>
            <td>${escapeHtml(trainerName)}</td>
            <td>${capacity}</td>
            <td><span class="badge text-bg-success">Active</span></td>
            <td class="text-end"><button class="btn btn-primary btn-sm" data-book-class="${id}" ${currentMemberUserId ? '' : 'disabled'}>Book</button></td>
        </tr>`;
    }).join('');

    tbody.querySelectorAll('[data-book-class]').forEach(btn => btn.addEventListener('click', () => bookClass(btn.dataset.bookClass, btn)));
}

function renderServicesTable() {
    const tbody = document.getElementById('servicesTableBody');
    if (allServices.length === 0) {
        tbody.innerHTML = `<tr><td colspan="5" class="text-center text-muted py-4">No gym services found.</td></tr>`;
        return;
    }

    tbody.innerHTML = allServices.map(s => {
        const id = pick(s, 'serviceID', 'ServiceID');
        const name = pick(s, 'name', 'Name');
        const category = pick(s, 'category', 'Category');
        const price = pick(s, 'price', 'Price');
        const duration = pick(s, 'durationInDays', 'DurationInDays');
        return `
        <tr>
            <td class="fw-semibold">${escapeHtml(name)}</td>
            <td>${escapeHtml(String(category))}</td>
            <td>$${price}</td>
            <td>${duration} days</td>
            <td class="text-end"><button class="btn btn-primary btn-sm" data-book-service="${id}" ${currentMemberUserId ? '' : 'disabled'}>Book</button></td>
        </tr>`;
    }).join('');

    tbody.querySelectorAll('[data-book-service]').forEach(btn => btn.addEventListener('click', () => bookService(btn.dataset.bookService, btn)));
}

async function bookClass(classId, btn) {
    if (!currentMemberUserId) { showMessage('Load a member first.', 'error'); return; }
    btn.disabled = true;
    try {
        await FitCoreApi.post(`/api/Classes/admin/book?memberUserId=${currentMemberUserId}&classId=${classId}`);
        showMessage('Class booked for member.', 'success');
    } catch (error) {
        showMessage(error.message, 'error');
    } finally {
        btn.disabled = false;
    }
}

async function bookService(serviceId, btn) {
    if (!currentMemberUserId) { showMessage('Load a member first.', 'error'); return; }
    btn.disabled = true;
    try {
        await FitCoreApi.post(`/api/GymServices/admin/book?userId=${currentMemberUserId}&gymServiceId=${serviceId}`);
        showMessage('Gym service booked for member.', 'success');
    } catch (error) {
        showMessage(error.message, 'error');
    } finally {
        btn.disabled = false;
    }
}

async function loadMemberHistory() {
    if (!currentMemberUserId) return;

    const classContainer = document.getElementById('memberClassBookings');
    const serviceContainer = document.getElementById('memberServiceBookings');
    classContainer.innerHTML = `<div class="text-muted small">Loading…</div>`;
    serviceContainer.innerHTML = `<div class="text-muted small">Loading…</div>`;

    try {
        const bookings = await FitCoreApi.get(`/api/Classes/admin/users/${currentMemberUserId}/bookings`);
        const list = Array.isArray(bookings) ? bookings : (bookings.data || bookings.Data || []);

        classContainer.innerHTML = list.length === 0
            ? `<div class="text-muted small">No class bookings yet.</div>`
            : list.map(b => renderBookingRow(
                pick(b, 'className', 'ClassName'),
                pick(b, 'scheduleDetails', 'ScheduleDetails') || [],
                Number(pick(b, 'status', 'Status')),
                pick(b, 'bookingID', 'BookingID')
            )).join('');

        wireCancelButtons(classContainer);
    } catch (error) {
        classContainer.innerHTML = `<div class="text-danger small">Couldn't load class bookings.</div>`;
    }

    try {
        const serviceBookings = await FitCoreApi.get(`/api/GymServices/admin/users/${currentMemberUserId}/bookings`);
        const list = Array.isArray(serviceBookings) ? serviceBookings : (serviceBookings.data || serviceBookings.Data || []);
        console.log(list);
        serviceContainer.innerHTML = list.length === 0
            ? `<div class="text-muted small">No service bookings yet.</div>`
            : list.map(b => renderBookingServiceRow(
                pick(b, 'serviceName', 'ServiceName'),
                [],
                pick(b, 'status', 'status'),
                pick(b, 'bookingId', 'bookingId')
            )).join('');

        wireCancelButtons(serviceContainer);
    } catch (error) {
        console.log(error)
        serviceContainer.innerHTML = `<div class="text-danger small">Couldn't load service bookings.</div>`;
    }
}

function renderBookingRow(name, scheduleDetails, status, bookingId) {
    const statusLabel = STATUS_LABELS[status] || 'unknown';
    return `
    <div class="d-flex justify-content-between align-items-center border-bottom py-2">
        <div>
            <div class="fw-semibold small">${escapeHtml(name)}</div>
            ${scheduleDetails.length ? `<div class="text-muted" style="font-size:11px;">${scheduleDetails.map(escapeHtml).join(' • ')}</div>` : ''}
        </div>
        <div class="d-flex align-items-center gap-2">
            <span class="badge text-bg-${STATUS_BADGE[statusLabel] || 'secondary'}">${statusLabel}</span>
            ${status === 0 ? `<button class="btn btn-outline-danger btn-sm" data-cancel-booking="${bookingId}">Cancel</button>` : ''}
        </div>
    </div>`;
}

function renderBookingServiceRow(name, scheduleDetails, status, bookingId) {
    return `
    <div class="d-flex justify-content-between align-items-center border-bottom py-2">
        <div>
            <div class="fw-semibold small">${escapeHtml(name)}</div>
            ${scheduleDetails.length ? `<div class="text-muted" style="font-size:11px;">${scheduleDetails.map(escapeHtml).join(' • ')}</div>` : ''}
        </div>
        <div class="d-flex align-items-center gap-2">
            <span class="badge text-bg-${STATUS_SERVICE[status] || 'secondary'}">${status}</span>
            ${status === "Booked" ? `<button class="btn btn-outline-danger btn-sm" data-cancel-booking="${bookingId}">Cancel</button>` : ''}
        </div>
    </div>`;
}

function wireCancelButtons(container) {
    container.querySelectorAll('[data-cancel-booking]').forEach(btn => {
        btn.addEventListener('click', () => cancelBooking(btn.dataset.cancelBooking));
    });
}

async function cancelBooking(bookingId) {
    try {
        await FitCoreApi.patch(`/api/Classes/admin/bookings/${bookingId}/cancel/${currentMemberUserId}`);
        showMessage('Booking cancelled.', 'success');
        loadMemberHistory();
    } catch (error) {
        showMessage(error.message, 'error');
    }
}

function escapeHtml(str) {
    const div = document.createElement('div');
    div.textContent = str ?? '';
    return div.innerHTML;
}
