// 1. تحديث الحالات لتطابق الـ Enum في السي شارب (0=Booked, 1=Cancelled, 2=Attended, 3=NoShow, 4=Paid)
const STATUS_LABELS = ['booked', 'cancelled', 'attended', 'noshow', 'paid'];
const bookingStatuses = {
    0: 'Booked',
    1: 'Cancelled',
    2: 'Attended',
    3: 'NoShow',
    4: 'Paid'
};
const categories = { 0: 'Memberships', 1: 'Personal Training', 2: 'Spa & Recovery', 3: 'Special Workshops' };
const CLASS_ICONS = [
    { match: /hiit|sprint|inferno/i, icon: 'bx-bolt' },
    { match: /yoga|flow|yin|zen/i, icon: 'bx-leaf' },
    { match: /spin|cycle|cycling/i, icon: 'bx-cycling' },
    { match: /lift|strength|power|core/i, icon: 'bx-transfer-alt' },
];

let allBookings = [];
let allServices = [];

let user = getCurrentUser();

document.addEventListener('DOMContentLoaded', () => {
    requireRole(["Member"]);
    loadData();
    document.querySelectorAll('.tab-btn').forEach(btn => {
        btn.addEventListener('click', () => {
            document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            document.getElementById('upcomingTab').style.display = btn.dataset.tab === 'upcoming' ? 'block' : 'none';
            document.getElementById('pastTab').style.display = btn.dataset.tab === 'past' ? 'block' : 'none';
        });
    });

    document.getElementById('pastRangeFilter').addEventListener('change', renderPast);
});

function showMessage(text, type) {
    const banner = document.getElementById('msgBanner');
    banner.textContent = text;
    banner.className = `msg-banner show ${type}`;
    setTimeout(() => banner.classList.remove('show'), 4000);
}

function classIconFor(name) {
    const found = CLASS_ICONS.find(c => c.match.test(name || ''));
    return found ? found.icon : 'bx-body';
}


function pick(obj, lowerKey, upperKey) {
    return obj[lowerKey] !== undefined ? obj[lowerKey] : obj[upperKey];
}


async function loadBookings() {
    const memberUserId = user?.userId;

    try {
        const data = await FitCoreApi.get(`/api/Classes/my-bookings`);
        allBookings = Array.isArray(data) ? data : (data.data || data.Data || []);
        console.log("Retrieved Bookings:", allBookings);

    } catch (error) {
        showMessage(`Couldn't load your bookings: ${error.message}`, 'error');
    }
}

async function loadServices() {
    const memberUserId = user?.userId;

    try {
        const data = await FitCoreApi.get(`/api/GymServices/my-services?memberUserId=${memberUserId}`);
        allServices = Array.isArray(data) ? data : (data.data || data.Data || []);
        console.log("Retrieved Services:", allServices);

    } catch (error) {
        showMessage(`Couldn't load your bookings: ${error.message}`, 'error');
    }
}

async function loadData() {
    try {
        await Promise.all([loadBookings(), loadServices()]);
        renderStats();
        renderUpcoming();
        renderServiceUpcoming();
        renderPast();
    } catch (error) {
        showMessage(`Couldn't load your Data: ${error.message}`, 'error');
    }
}


function isUpcoming(b) {
    const rawStatus = pick(b, 'status', 'Status');
    return rawStatus === 0 ||
        rawStatus === '0' ||
        (typeof rawStatus === 'string' && rawStatus.toLowerCase() === 'booked');
}

function renderStats() {
    const totalBookings = allBookings.length;
    const totalServices = allServices.length;

    console.log(allServices.filter(b => isUpcoming(b)).length, allServices);

    const upcomingCount = (allBookings.filter(b => isUpcoming(b)).length + allServices.filter(b => isUpcoming(b)).length);

    document.getElementById('totalBookingsValue').textContent = totalBookings;
    document.getElementById('totalServicesValue').textContent = totalServices;
    document.getElementById('trainingHoursValue').textContent = (allBookings.filter(b => !isUpcoming(b)).length * 1).toFixed(1);
    document.getElementById('upcomingCountValue').textContent = upcomingCount;
}

function renderUpcoming() {

    const upcoming = allBookings.filter(b => isUpcoming(b));

    const nextCard = document.getElementById('nextClassCard');

    if (upcoming.length === 0) {

        nextCard.innerHTML = `<div class="state-empty">
        No upcoming classes booked yet.
        <br>
        <a href="/html/user/classes/classes-schedule.html">Browse classes →</a>
        </div>`;
        return;
    }

    nextCard.innerHTML = upcoming.map((up) => {
        const currentClassName = pick(up, 'className', 'ClassName');
        const currentSchedules = pick(up, 'scheduleDetails', 'ScheduleDetails') || [];
        const currentBookingID = pick(up, 'bookingID', 'BookingID');
        const status =  pick(up, 'status', 'status');
        const trainerName = pick(up, 'trainerName', 'trainerName');
        const price = pick(up, 'price', 'price');

        return `
        <div class="col">
           <div class="card p-3 rounded-4 shadow">
            <div class="next-class-media rounded-4">
                <i class='bx ${classIconFor(currentClassName)}'></i>
            </div>
            <div class="next-class-body">
                <div class="next-class-title-row">
                    <div class="next-class-title">${escapeHtml(currentClassName)}</div>
                    <div class="badge ${bookingStatuses[status] === 'Paid' ? 'bg-light-success text-success' : 'bg-light-warning text-warning'}" 
                             style="padding: 6px 10px; font-size: 11px; border-radius: 20px;">
                            <span class="day">${escapeHtml(bookingStatuses[status])}</span>
                     </div>
                </div>
                <div class="next-class-meta">
                    
                </div>
                <div class="next-class-meta d-flex flex-column gap-1 text-muted mb-3" style="font-size: 13px;">
                            ${currentSchedules.map((sch) => `
                                <span> <i class='bx bx-time-five'></i> schedule: ${escapeHtml(sch)}</span>
                            `).join('')}
                        <span><i class='bx bx-dumbbell text-primary me-1'></i> trainerName: <strong>${trainerName}</strong></span>
                        <span><i class='bx bx-money text-primary me-1'></i> Price: <strong class="text-dark">${parseFloat(price).toFixed(0)} EGP</strong></span>
                 </div>
                <div class=" d-flex flex-md-row flex-column justify-content-between gap-2">
                    <button class="btn btn-primary fw-semibold"
                        onclick="checkoutService(${currentBookingID})">
                        Proceed to Checkout
                    </button>          
                    <button class="icon-btn-danger border border-danger text-danger" title="Cancel booking" data-cancel="${currentBookingID}"><i class='bx bx-x'></i></button>
                </div>
            </div>
           </div>
        </div>`;
    }).join('');


    document.querySelectorAll('[data-cancel]').forEach(btn => {
        btn.addEventListener('click', () => cancelBooking(parseInt(btn.dataset.cancel)));
    });
}

function renderServiceUpcoming() {

    const upcoming = allServices.filter(b => isUpcoming(b));

    const nextCard = document.getElementById('nextServiceCard');

    if (upcoming.length === 0) {

        nextCard.innerHTML = `
        <div class="state-empty">
            No upcoming Services booked yet.
            <br>
            <a href="/html/user/gymServices/gym-services.html">Browse Services →</a>
        </div>`;
        return;
    }

    nextCard.innerHTML = upcoming.map((up) => {
        const currentServiceName = pick(up, 'serviceName', 'serviceName');
        const allowedSessionsCount = pick(up, 'allowedSessionsCount', 'allowedSessionsCount');
        const price = pick(up, 'price', 'price');
        const status = pick(up, 'status', 'status');
        // const category = pick(up, `${categories[category]}`, 'category');
        const durationInDays = pick(up, 'durationInDays', 'durationInDays') || [];
        const currentBookingID = pick(up, 'bookingId', 'BookingID');
        

        return `
        <div class=" col">
          <div class="card p-3 rounded-4 shadow h-100">
                 <div class="next-class-media rounded-4">
                    <i class='bx ${classIconFor(currentServiceName)}'></i>
                </div>
        
                <div class="next-class-body mt-3">
                    <div class="next-class-title-row d-flex justify-content-between align-items-start mb-2">
                        <div class="next-class-title fw-bold text-dark fs-5">${escapeHtml(currentServiceName)}</div>
                
                        <div class="badge ${status === 'Paid' ? 'bg-light-success text-success' : 'bg-light-warning text-warning'}" 
                             style="padding: 6px 10px; font-size: 11px; border-radius: 20px;">
                            <span class="day">${escapeHtml(status)}</span>
                        </div>
                    </div>
            
                    <div class="next-class-meta d-flex flex-column gap-1 text-muted mb-3" style="font-size: 13px;">
                        <span><i class='bx bx-time-five text-primary me-1'></i> Duration: <strong>${durationInDays} Days</strong></span>
                        <span><i class='bx bx-dumbbell text-primary me-1'></i> Sessions: <strong>${allowedSessionsCount === 0 ? 'Unlimited' : allowedSessionsCount + ' Sessions'}</strong></span>
                        <span><i class='bx bx-money text-primary me-1'></i> Price: <strong class="text-dark">${parseFloat(price).toFixed(0)} EGP</strong></span>
                    </div>
                    <div class="next-class-actions">
                        ${status === 'Booked' ? `
                            <button class="btn btn-primary fw-semibold"
                                    onclick="checkoutService(${currentBookingID})">
                                Proceed to Checkout
                            </button>
                        ` : `
                            <button class="btn btn-outline-secondary fw-semibold  " disabled>
                                <i class='bx bx-check'></i> Active Plan
                            </button>
                        `}
                         <button
                             class="icon-btn-danger border border-danger text-danger"
                             title="Cancel booking"
                             data-service="${currentBookingID}"
                         >          
                            <i class='bx bx-x'></i>
                         </button>
                       
                    </div>
                </div>
            </div>
        </div>`;
    }).join('');


    document.querySelectorAll('[data-service]').forEach(btn => {
        btn.addEventListener('click', () => cancelServiceBooking(btn.dataset.service));
    });
}

function renderPast() {

    const past = allBookings.filter(b => !isUpcoming(b));

    const tbody = document.getElementById('pastTableBody');
    if (past.length === 0) {
        tbody.innerHTML = `<tr><td colspan="5" class="state-empty">No past Class sessions found.</td></tr>`;
        return;
    }

    tbody.innerHTML = past.map(b => {
        const className = pick(b, 'className', 'ClassName');
        const status = Number(pick(b, 'status', 'Status'));
        const statusLabel = STATUS_LABELS[status] || 'unknown';
        const bSchedules = pick(b, 'scheduleDetails', 'ScheduleDetails') || [];
        const bMainSchedule = bSchedules[0] || "—";
        const trainerName = pick(b, 'trainerName', 'TrainerName') || [];
        const price = pick(b, 'price', 'price');

        return `
        <tr>
            <td>
                <div class="class-details-cell">
                    <div class="class-details-icon"><i class='bx ${classIconFor(className)}'></i></div>
                    <div>
                        <div class="class-details-name">${escapeHtml(className)}</div>
                        <div class="class-details-date">${escapeHtml(bMainSchedule)}</div>
                    </div>
                </div>
            </td>
            <td>${escapeHtml(trainerName)}</td>
            <td>
                ${bSchedules.map((sch) => `
                      <div class="class-details-date">${escapeHtml(sch)}</div>
                 `).join('')}
            </td>
            <td>${escapeHtml(price)}</td>
            <td><span class="status-dot ${statusLabel}">${statusLabel.charAt(0).toUpperCase() + statusLabel.slice(1)}</span></td>
        </tr>`;
    }).join('');
}

async function cancelBooking(bookingId) {
    const memberUserId = user?.userId;
    console.log(memberUserId, bookingId)

    try {
        await FitCoreApi.patch(`/api/Classes/bookings/${bookingId}/cancel?memberUserId=${memberUserId}`);
        showMessage('Booking cancelled.', 'success');
        loadData();
    } catch (error) {
        showMessage(error.message, 'error');
    }
}

async function cancelServiceBooking(bookingId) {
    const memberUserId = user?.userId;
    try {
        
        await FitCoreApi.delete(`/api/GymServices/bookings/${bookingId}/cancel?memberUserId=${memberUserId}`);
        showMessage('Booking cancelled.', 'success');
        loadData();
    } catch (error) {
        showMessage(error.message, 'error');
    }
}

function escapeHtml(str) {
    const div = document.createElement('div');
    div.textContent = str ?? '';
    return div.innerHTML;
}
