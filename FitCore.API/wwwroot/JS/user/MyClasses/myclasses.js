// My Classes page — shows only bookings (classes + gym services) whose
// status is "Attended" or "Paid".
//
// APIs used (already existing in the backend):
//   GET /api/Classes/my-bookings   -> ClassBookingDto[]      (Status = BookingStatus enum -> serialized as a NUMBER by default)
//   GET /api/GymServices/my-services -> BookingGymServiceDto[] (Status = string, e.g. "Attended" / "Paid")
//
// BookingStatus enum (FitCore.Shared.Enums.BookingStatus):
//   0 = Booked, 1 = Cancelled, 2 = Attended, 3 = NoShow, 4 = Paid

const CLASSES_ENDPOINT = '/api/Classes/my-bookings';
const GYM_SERVICES_ENDPOINT = '/api/GymServices/my-services';

const STATUS_NUMBER_TO_NAME = {
    0: 'Booked',
    1: 'Cancelled',
    2: 'Attended',
    3: 'NoShow',
    4: 'Paid',
};

const TARGET_STATUSES = ['attended', 'paid']; // what this page cares about

let allClassBookings = [];
let allServiceBookings = [];

document.addEventListener('DOMContentLoaded', () => {
    if (typeof requireRole === 'function') requireRole(['Member']);

    loadData();
    document.getElementById('statusFilter')?.addEventListener('change', renderAll);
});

// ---------------------------------------------------------------
// Data loading
// ---------------------------------------------------------------
async function loadData() {
    const loadingState = document.getElementById('loadingState');
    loadingState.style.display = 'block';

    try {
        const [classData, serviceData] = await Promise.all([
            FitCoreApi.get(CLASSES_ENDPOINT),
            FitCoreApi.get(GYM_SERVICES_ENDPOINT),
        ]);

        allClassBookings = Array.isArray(classData) ? classData : (classData?.data || classData?.Data || []);
        allServiceBookings = Array.isArray(serviceData) ? serviceData : (serviceData?.data || serviceData?.Data || []);

        renderAll();
    } catch (error) {
        console.error('Error loading my classes:', error);
        showBanner(error.message || 'Could not load your classes and services.');
    } finally {
        loadingState.style.display = 'none';
    }
}

// ---------------------------------------------------------------
// Status helpers
// ---------------------------------------------------------------
function normalizeStatus(rawStatus) {
    // Handles both numeric enum values (Classes API) and string values (GymServices API)
    if (typeof rawStatus === 'number') {
        return (STATUS_NUMBER_TO_NAME[rawStatus] || '').toLowerCase();
    }
    if (typeof rawStatus === 'string' && /^\d+$/.test(rawStatus)) {
        return (STATUS_NUMBER_TO_NAME[Number(rawStatus)] || '').toLowerCase();
    }
    return String(rawStatus || '').toLowerCase();
}

function matchesFilter(statusLower) {
    const filter = document.getElementById('statusFilter')?.value || 'all';
    if (filter === 'all') return TARGET_STATUSES.includes(statusLower);
    return statusLower === filter;
}

// ---------------------------------------------------------------
// Render
// ---------------------------------------------------------------
function renderAll() {
    const filteredClasses = allClassBookings.filter(b => matchesFilter(normalizeStatus(pick(b, 'status', 'Status'))));
    const filteredServices = allServiceBookings.filter(s => matchesFilter(normalizeStatus(pick(s, 'status', 'Status'))));

    renderClasses(filteredClasses);
    renderServices(filteredServices);
    renderStats();
}

function renderClasses(items) {
    const grid = document.getElementById('classesGrid');
    const emptyState = document.getElementById('classesEmptyState');
    grid.innerHTML = '';

    if (!items || items.length === 0) {
        emptyState.style.display = 'block';
        return;
    }
    emptyState.style.display = 'none';

    items.forEach(item => grid.appendChild(buildClassCard(item)));
}

function buildClassCard(booking) {
    const className = pick(booking, 'className', 'ClassName') ?? 'Class';
    const trainerName = pick(booking, 'trainerName', 'TrainerName') ?? '—';
    const price = Number(pick(booking, 'price', 'Price') ?? 0);
    const schedules = pick(booking, 'scheduleDetails', 'ScheduleDetails') ?? [];
    const statusLower = normalizeStatus(pick(booking, 'status', 'Status'));

    const col = document.createElement('div');
    col.className = 'col';
    col.innerHTML = `
        <div class="item-card">
            <div class="item-card-top">
                <div class="d-flex gap-3">
                    <div class="item-icon"><i class="fa-solid fa-dumbbell"></i></div>
                    <div>
                        <div class="item-title">${escapeHtml(className)}</div>
                        <div class="item-subtitle">Trainer: ${escapeHtml(trainerName)}</div>
                    </div>
                </div>
                ${statusBadge(statusLower)}
            </div>
            <div class="item-meta">
                <i class="bx bx-time-five"></i>
                <span>${schedules.length ? escapeHtml(schedules.join(', ')) : 'No schedule info'}</span>
            </div>
            <div class="item-meta item-price">${formatCurrency(price)}</div>
        </div>
    `;
    return col;
}

function renderServices(items) {
    const grid = document.getElementById('servicesGrid');
    const emptyState = document.getElementById('servicesEmptyState');
    grid.innerHTML = '';

    if (!items || items.length === 0) {
        emptyState.style.display = 'block';
        return;
    }
    emptyState.style.display = 'none';

    items.forEach(item => grid.appendChild(buildServiceCard(item)));
}

const SERVICE_CATEGORY_LABELS = { 0: 'Membership', 1: 'Personal Training', 2: 'Spa & Recovery', 3: 'Special Workshop' };

function buildServiceCard(service) {
    const serviceName = pick(service, 'serviceName', 'ServiceName') ?? 'Gym Service';
    const price = Number(pick(service, 'price', 'Price') ?? 0);
    const category = pick(service, 'category', 'Category');
    const durationInDays = pick(service, 'durationInDays', 'DurationInDays') ?? 0;
    const allowedSessions = pick(service, 'allowedSessionsCount', 'AllowedSessionsCount') ?? 0;
    const statusLower = normalizeStatus(pick(service, 'status', 'Status'));

    const col = document.createElement('div');
    col.className = 'col';
    col.innerHTML = `
        <div class="item-card">
            <div class="item-card-top">
                <div class="d-flex gap-3">
                    <div class="item-icon"><i class="fa-solid fa-spa"></i></div>
                    <div>
                        <div class="item-title">${escapeHtml(serviceName)}</div>
                        <div class="item-subtitle">${escapeHtml(SERVICE_CATEGORY_LABELS[category] ?? 'Service')}</div>
                    </div>
                </div>
                ${statusBadge(statusLower)}
            </div>
            <div class="item-meta">
                <i class="bx bx-calendar"></i>
                <span>${durationInDays} day(s) · ${allowedSessions} session(s)</span>
            </div>
            <div class="item-meta item-price">${formatCurrency(price)}</div>
        </div>
    `;
    return col;
}

function statusBadge(statusLower) {
    if (statusLower === 'attended') {
        return `<span class="status-badge status-badge--attended"><i class="fa-solid fa-check"></i> Attended</span>`;
    }
    if (statusLower === 'paid') {
        return `<span class="status-badge status-badge--paid"><i class="fa-solid fa-coins"></i> Paid</span>`;
    }
    return `<span class="status-badge">${escapeHtml(statusLower)}</span>`;
}

// ---------------------------------------------------------------
// Stats
// ---------------------------------------------------------------
function renderStats() {
    const classStatuses = allClassBookings.map(b => normalizeStatus(pick(b, 'status', 'Status')));
    const serviceStatuses = allServiceBookings.map(s => normalizeStatus(pick(s, 'status', 'Status')));

    setCount('attendedClassesCount', classStatuses.filter(s => s === 'attended').length);
    setCount('paidClassesCount', classStatuses.filter(s => s === 'paid').length);
    setCount('attendedServicesCount', serviceStatuses.filter(s => s === 'attended').length);
    setCount('paidServicesCount', serviceStatuses.filter(s => s === 'paid').length);
}

function setCount(elId, value) {
    const el = document.getElementById(elId);
    if (el) el.innerText = value;
}

// ---------------------------------------------------------------
// Small helpers
// ---------------------------------------------------------------
function pick(obj, ...keys) {
    for (const key of keys) {
        if (obj && obj[key] !== undefined && obj[key] !== null) return obj[key];
    }
    return undefined;
}

function formatCurrency(value) {
    const num = Number(value) || 0;
    return num.toLocaleString(undefined, { style: 'currency', currency: 'USD' });
}

function escapeHtml(str) {
    return String(str).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}

function showBanner(message) {
    const banner = document.getElementById('msgBanner');
    if (!banner) return;
    banner.innerText = message;
    banner.style.display = 'block';
}