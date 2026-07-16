// classes-schedule.js

const DAY_LABELS = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
const CLASS_ICONS = [
    { match: /hiit|sprint|inferno/i, icon: 'bx-bolt', color: 'orange', category: 'HIIT' },
    { match: /yoga|flow|yin|zen/i, icon: 'bx-leaf', color: 'indigo', category: 'Yoga' },
    { match: /spin|cycle|cycling/i, icon: 'bx-cycling', color: 'orange', category: 'Spin' },
    { match: /lift|strength|power|core/i, icon: 'bx-transfer-alt', color: 'indigo', category: 'Strength' },
];

const FEATURED_SVG = `<svg viewBox="0 0 260 230" xmlns="http://www.w3.org/2000/svg">
    <ellipse cx="130" cy="212" rx="90" ry="10" fill="rgba(0,0,0,0.12)"/>
    <g fill="none" stroke="rgba(20,22,31,0.8)" stroke-width="6" stroke-linecap="round" stroke-linejoin="round">
        <path d="M130 60 L130 120"/><path d="M130 120 L95 200"/><path d="M130 120 L165 200"/>
        <path d="M130 85 L75 55"/><path d="M130 85 L185 55"/><path d="M75 55 L60 20"/><path d="M185 55 L200 20"/>
    </g>
    <circle cx="130" cy="42" r="18" fill="rgba(20,22,31,0.8)"/>
</svg>`;

let occurrences = [];
let currentPage = 1;
let totalCount = 0;
const PAGE_SIZE = 9;
let user = [];
let activeCategory = '';
let activeTrainer = '';


function pick(obj, ...props) {
    if (!obj) return undefined;
    for (const prop of props) {
        if (prop in obj) return obj[prop];
    }
    return undefined;
}

document.addEventListener('DOMContentLoaded', () => {
    requireRole(["Member"]);
    user = getCurrentUser();
    loadOccurrences(true);
    
    document.getElementById('rangeFilter').addEventListener('change', () => loadOccurrences(true));
    document.getElementById('trainerFilter').addEventListener('change', (e) => { activeTrainer = e.target.value; renderGrid(); });
    document.getElementById('loadMoreBtn').addEventListener('click', () => loadOccurrences(false));

    document.querySelectorAll('.view-btn').forEach(btn => {
        btn.addEventListener('click', () => {
            document.querySelectorAll('.view-btn').forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            document.getElementById('classesGrid').classList.toggle('list-view', btn.dataset.view === 'list');
        });
    });
});

function showMessage(text, type) {
    const banner = document.getElementById('msgBanner');
    if (!banner) return;
    banner.textContent = text;
    banner.className = `msg-banner show ${type}`;
    setTimeout(() => banner.classList.remove('show'), 4000);
}

function showToast(message) {
    const toast = document.getElementById('toast');
    if (!toast) return;
    toast.textContent = message;
    toast.classList.add('show');
    clearTimeout(showToast._t);
    showToast._t = setTimeout(() => toast.classList.remove('show'), 2800);
}

function categoryFor(name) {
    const found = CLASS_ICONS.find(c => c.match.test(name || ''));
    return found || { icon: 'bx-body', color: 'indigo', category: 'General' };
}

async function loadOccurrences(reset) {
    if (reset) { currentPage = 1; occurrences = []; }

    const rangeFilter = document.getElementById('rangeFilter');
    const rangeDays = rangeFilter ? parseInt(rangeFilter.value, 10) : 7;
    const from = new Date();
    const to = new Date(from.getTime() + rangeDays * 24 * 60 * 60 * 1000);

    const grid = document.getElementById('classesGrid');
    if (!grid) return;
    if (reset) grid.innerHTML = `<div class="state-empty">Loading classes…</div>`;

    try {
        const url = `/api/Classes/browse?fromDate=${toDateInput(from)}&toDate=${toDateInput(to)}&Page=${currentPage}&Page_Size=${PAGE_SIZE}`;
        const data = await FitCoreApi.get(url);
        const pageItems = data.data || data.Data || [];
        totalCount = data.totalCount ?? data.TotalCount ?? pageItems.length;

        console.log(data);

        occurrences = reset ? pageItems : occurrences.concat(pageItems);

        populateFilterOptions();
        renderGrid();

        const loadMoreBtn = document.getElementById('loadMoreBtn');
        if (loadMoreBtn) {
            loadMoreBtn.style.display = occurrences.length < totalCount ? 'inline-flex' : 'none';
        }
        if (occurrences.length < totalCount) currentPage++;
    } catch (error) {
        grid.innerHTML = `<div class="state-empty">Couldn't load classes: ${escapeHtml(error.message)}</div>`;
    }
}

function populateFilterOptions() {
    const categories = new Set();
    const trainers = new Set();
    occurrences.forEach(o => {
        categories.add(categoryFor(pick(o, 'className', 'ClassName')).category);
        trainers.add(pick(o, 'trainerName', 'TrainerName'));
    });

    const pillsContainer = document.getElementById('disciplinePills');
    if (pillsContainer) {
        const existingButtons = new Set(Array.from(pillsContainer.querySelectorAll('.pill')).map(b => b.dataset.filter));
        categories.forEach(cat => {
            if (!existingButtons.has(cat)) {
                const btn = document.createElement('button');
                btn.className = 'pill';
                btn.dataset.filter = cat;
                btn.textContent = cat;
                btn.addEventListener('click', () => {
                    document.querySelectorAll('.discipline-pills .pill').forEach(p => p.classList.remove('active'));
                    btn.classList.add('active');
                    activeCategory = cat;
                    renderGrid();
                });
                pillsContainer.appendChild(btn);
            }
        });

        const allPill = pillsContainer.querySelector('[data-filter=""]');
        if (allPill) {
            allPill.onclick = () => {
                document.querySelectorAll('.discipline-pills .pill').forEach(p => p.classList.remove('active'));
                allPill.classList.add('active');
                activeCategory = '';
                renderGrid();
            };
        }
    }

    const trainerSelect = document.getElementById('trainerFilter');
    if (trainerSelect) {
        const currentValue = trainerSelect.value;
        trainerSelect.innerHTML = '<option value="">Filter by Trainer</option>' +
            Array.from(trainers).sort().map(t => `<option value="${escapeHtml(t)}">${escapeHtml(t)}</option>`).join('');
        trainerSelect.value = currentValue;
    }
}

function getFilteredOccurrences() {
    return occurrences.filter(o => {
        if (activeCategory && categoryFor(pick(o, 'className', 'ClassName')).category !== activeCategory) return false;
        if (activeTrainer && pick(o, 'trainerName', 'TrainerName') !== activeTrainer) return false;
        return true;
    });
}

function renderGrid() {
    const grid = document.getElementById('classesGrid');
    if (!grid) return;

    const filtered = getFilteredOccurrences();

    if (filtered.length === 0) {
        grid.innerHTML = `<div class="state-empty"><i class='bx bx-calendar-x' style="font-size:28px;display:block;margin-bottom:8px;"></i>No sessions match your filters in this range.</div>`;
        return;
    }

    grid.innerHTML = filtered.map((o, index) => renderCard(o, index === 0)).join('');

    grid.querySelectorAll('[data-book]').forEach(btn => {
        btn.addEventListener('click', () => bookOccurrence(btn.dataset.classId, btn.dataset.sessionDate, btn));
    });
}

function renderCard(o, featured) {
    const classID = pick(o, 'classID', 'ClassID');
    const className = pick(o, 'className', 'ClassName');
    const description = pick(o, 'description', 'Description') || '';
    const trainerName = pick(o, 'trainerName', 'TrainerName') || '—';
    const price = pick(o, 'price', 'price') ;
    const capacity = Number(pick(o, 'capacity', 'Capacity') || 0);
    const bookedCount = Number(pick(o, 'bookedCount', 'BookedCount') || 0);
    const available = capacity - bookedCount;
    const isFull = available <= 0;

    const cat = categoryFor(className);
    const schedules = pick(o, 'schedules', 'Schedules') || [];

    let schedulesHtml = '';
    if (schedules.length === 0) {
        schedulesHtml = `<div class="no-schedules"><i class='bx bx-info-circle'></i> No upcoming schedules</div>`;
    } else {
        schedulesHtml = `
            <div class="mt-3 d-flex flex-column gap-2">
                <div class="small text-uppercase fw-bold text-muted" style="font-size: 11px; letter-spacing: 0.5px;">
                    Class Days & Times:
                </div>
        `;

        schedules.forEach(s => {
            const dayName = pick(s, 'dayName', 'DayName');
            const calcDate = (pick(s, 'calculatedDate', 'CalculatedDate') || '').toString().substring(0, 10);
            const start = (pick(s, 'startTime', 'StartTime') || '').toString().substring(0, 5);
            const end = (pick(s, 'endTime', 'EndTime') || '').toString().substring(0, 5);

            schedulesHtml += `
            <div class="p-2 rounded-3 bg-light shadow-sm d-flex flex-column justify-content-center">
                <div class="d-flex align-items-center justify-content-between">
                    <span class="fw-bold text-dark" style="font-size: 13.5px;">
                        ${dayName} 
                        <small class="text-muted font-monospace fw-normal ms-1">(${calcDate})</small>
                    </span>
                </div>
                <div class="text-muted mt-1" style="font-size: 12px;">
                    <i class='bx bx-time-five text-primary me-1'></i> ${start} - ${end}
                </div>
            </div>
        `;
        });

        schedulesHtml += `</div>`;
    }

    if (featured) {
        return `
            <article class="card featured">
                <div class="featured-media">
                    <div class="featured-badges">
                        <span class="tag-badge">${escapeHtml(cat.category)}</span>
                        <span class="tag-badge blue">Featured</span>
                    </div>
                    ${FEATURED_SVG}
                </div>
                <div class="featured-body">
                    <div class="d-flex justify-content-between align-items-center">
                        <div class="class-name">${escapeHtml(className)}</div>
                        <span class="badge text-primary">${available}/${capacity} open</span>
                    </div>
                    <div class="tagline">${escapeHtml(description)}</div>
                    <div class="py-1">
                        <div class="meta-row"><i class='bx bx-user'></i> ${escapeHtml(trainerName)}</div>
                    </div>
                
                    ${schedulesHtml}
                    <div class="d-flex align-items-center justify-content-between mt-3 pt-2 border-top border-light">
                        <span class="text-muted small fw-bold text-uppercase" style="font-size: 11px; letter-spacing: 0.5px;">
                            <i class='bx bx-credit-card-front me-1'></i> Total Price:
                        </span>
                        <span class="fw-extrabold fs-4 text-dark">
                            ${price} <span class="text-primary fw-bold" style="font-size: 13px;">EGP</span>
                        </span>
                    </div>
                    <button class="btn-primary" data-book data-class-id="${classID}" ${isFull ? 'disabled' : ''} style="margin-top: 15px; width: 100%;">
                        ${isFull ? 'Full Booked' : 'Book Full Course'}
                    </button>
                </div>
            </article>
        `;
    }


    return `
    <article class="card regular">
        <div class="card-top py-2">
            <div class="icon-chip ${cat.color} "><i class='bx ${cat.icon}'></i></div>
            <span class="badge text-primary">${available}/${capacity} open</span>
        </div>
        <div class="class-name">${escapeHtml(className)}</div>
        <div class="tagline fs-sm">${escapeHtml(description)}</div>
        <div class="meta-row py-1"><i class='bx bx-user'></i> ${escapeHtml(trainerName)}</div>
        
        ${schedulesHtml}
        <div class="d-flex align-items-center justify-content-between mt-3 pt-2 border-top border-light">
            <span class="text-muted small fw-bold text-uppercase" style="font-size: 11px; letter-spacing: 0.5px;">
                <i class='bx bx-credit-card-front me-1'></i> Total Price:
            </span>
            <span class="fw-extrabold fs-4 text-dark">
                ${price} <span class="text-primary fw-bold" style="font-size: 13px;">EGP</span>
            </span>
        </div>
        <button class="btn btn-outline-primary" data-book data-class-id="${classID}" ${isFull ? 'disabled' : ''} style="margin-top: 15px; width: 100%;">
            ${isFull ? 'Full' : 'Book Spot'}
        </button>
    </article>`;
}

function dayLabelFor(isoDate) {
    if (!isoDate) return 'Unknown Date';
    const today = new Date(); today.setHours(0, 0, 0, 0);
    const tomorrow = new Date(today.getTime() + 86400000);
    const target = new Date(isoDate + 'T00:00:00');

    if (isNaN(target.getTime())) return isoDate;
    if (target.getTime() === today.getTime()) return 'Today';
    if (target.getTime() === tomorrow.getTime()) return 'Tomorrow';
    return DAY_LABELS[target.getDay()] + ' ' + target.toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
}

async function bookOccurrence(classID, sessionDate, btn) {
    const memberUserId = user?.userId ;
    const originalText = btn.textContent;
    btn.disabled = true;
    btn.textContent = 'Booking…';

    try {
        
        console.log(parseInt(classID, 10));
        await FitCoreApi.post(`/api/Classes/book?classID=${parseInt(classID, 10)}`);
        showToast('You are booked in! Check My Bookings for details.');
        await loadOccurrences(true);
    } catch (error) {
        showMessage(error.message, 'error');
        btn.disabled = false;
        btn.textContent = originalText;
    }
}

function toDateInput(date) { return date.toISOString().substring(0, 10); }

function initials(name) {
    return (name || '').split(' ').map(p => p[0]).join('').substring(0, 2).toUpperCase();
}

function escapeHtml(str) {
    const div = document.createElement('div');
    div.textContent = str ?? '';
    return div.innerHTML;
}