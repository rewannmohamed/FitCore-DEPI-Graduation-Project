const API_URL = '/api/GymServices';
let page = 1;
const pageSize = 5;
let searchTerm = '';
let category = '';
const user = getCurrentUser();
const categories = { 0: 'Memberships', 1: 'Personal Training', 2: 'Spa & Recovery', 3: 'Special Workshops' };
const token = getToken();

let myBookedServiceIds = new Map(); 

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

document.addEventListener('DOMContentLoaded', () => {
    if (!token) {
        window.location.href = "/html/Auth/login.html";
        return null;
    }
    requireRole(["Member"]);
    loadMyServices().then(loadUserServices);

    document.getElementById('userSearchInput').addEventListener('input', (e) => {
        searchTerm = e.target.value.trim();
        page = 1;
        loadUserServices();
    });

    document.getElementById('userCategoryFilter').addEventListener('change', (e) => {
        category = e.target.value;
        page = 1;
        loadUserServices();
    });

    document.getElementById('userBtnPrev').addEventListener('click', () => { if (page > 1) { page--; loadUserServices(); } });
    document.getElementById('userBtnNext').addEventListener('click', () => { page++; loadUserServices(); });
});


async function loadMyServices() {
    const memberUserId = user?.userId;
    if (!memberUserId) return;

    try {
        const data = await FitCoreApi.get(`${API_URL}/my-services`);
        const list = Array.isArray(data) ? data : (data.data || data.Data || []);

        myBookedServiceIds = new Map();
        list.forEach(b => {
            const serviceId = b.gymServiceId ?? b.GymServiceId;
            const status = (b.status ?? b.Status ?? '').toString();
            if (serviceId != null && status.toLowerCase() !== 'cancelled') {
                myBookedServiceIds.set(serviceId, status);
            }
        });
    } catch (err) {
        console.error("Failed to load member's existing services:", err);
    }
}

async function loadUserServices() {
    let url = `${API_URL}?page=${page}&pageSize=${pageSize}`;
    if (searchTerm) url += `&searchTerm=${encodeURIComponent(searchTerm)}`;
    if (category !== '') url += `&category=${category}`;

    try {
        const result = await FitCoreApi.get(url);
        // const result = await response.json();

        const data = result.data || result;
        const totalCount = result.totalCount || data.length;
        const totalPages = result.totalPages || Math.ceil(totalCount / pageSize);
        renderCards(data);

        document.getElementById('userPaginationText').textContent = `Page ${page} of ${totalPages} (${totalCount} Available Options)`;
        document.getElementById('userBtnPrev').disabled = (page === 1);
        document.getElementById('userBtnNext').disabled = (page >= totalPages || totalPages === 0);
    } catch (err) {
        console.error("Failed to stream user workspace:", err);
    }
}

function renderCards(services) {
    const container = document.getElementById('userCardsContainer');
    container.innerHTML = '';

    if (!services || services.length === 0) {
        container.innerHTML = `<div class="text-center text-muted py-5 w-100"><i class='bx bx-layer-minus fs-2 d-block mb-2'></i>No service tiers available under this scope.</div>`;
        return;
    }

    services.forEach((service, index) => {
        const col = document.createElement('div');
        const isFirst = index === 0;
        col.className = `${isFirst ? "col-12 col-md-6 col-lg-7" : "col-12 col-md-6 col-lg-4"} `;

        const serviceId = service.serviceID;
        const alreadyBooked = myBookedServiceIds.has(serviceId);
        const actionButton = renderActionButton(serviceId, isFirst, alreadyBooked);

        col.innerHTML = `
        ${isFirst ? `
            <div class="d-flex flex-lg-row flex-column gap-4 border p-2 shadow-lg rounded border-primary">
                <div class="position-relative">
                    <img src="/Images/vip.png" alt="Gym Image" class="img-fluid w-100 rounded" style="max-height:350px;"/>
                </div>
                <div class=" ${isFirst ? 'featured' : ''}">
                    <span class="card-badge">${categories[service.category] || 'General'}</span>
                    <h3 class="fw-bold m-0 text-dark">${service.name}</h3>
                    <div class="card-price">
                        ${parseFloat(service.price).toFixed(0)} <span class="text-muted">EGP</span>
                    </div>
                    <ul class="features-list">
                        <li><i class='bx bx-check-circle'></i> Membership cycle valid for <strong>${service.durationInDays} days</strong></li>
                        <li><i class='bx bx-check-circle'></i> Grants access to <strong>${service.allowedSessionsCount} sessions</strong></li>
                        <li><i class='bx bx-check-circle'></i> Instant check-in activation pipeline</li>
                    </ul>
                    ${actionButton}
                </div>
            </div>
        `: `<div class="border px-4 pt-4 pb-2 rounded shadow">
                <span class="card-badge">${categories[service.category] || 'General'}</span>
                <h3 class="fw-bold m-0 text-dark">${service.name}</h3>
                <div class="card-price">
                    ${parseFloat(service.price).toFixed(0)} <span class="text-muted">EGP</span>
                </div>
                <ul class="features-list">
                    <li><i class='bx bx-check-circle'></i> Membership cycle valid for <strong>${service.durationInDays} days</strong></li>
                    <li><i class='bx bx-check-circle'></i> Grants access to <strong>${service.allowedSessionsCount} sessions</strong></li>
                    <li><i class='bx bx-check-circle'></i> Instant check-in activation pipeline</li>
                </ul>
                ${actionButton}
            </div>`}
          
        `;
        container.appendChild(col);
    });
}


function renderActionButton(serviceId, isFirst, alreadyBooked) {
    if (alreadyBooked) {
        return `
        <button class="btn btn-success w-100 rounded-3 py-2 fw-semibold" disabled>
            <i class='bx bx-check-circle'></i> Already Purchased
        </button>`;
    }

    return `
    <button class="btn ${isFirst ? 'btn-primary' : 'btn-outline-primary'} w-100 rounded-3 py-2 fw-semibold"
            onclick="purchaseService(${serviceId}, this)">
        Purchase Plan
    </button>`;
}

async function purchaseService(gymServiceId, btn) {
    const memberUserId = user?.userId;

    if (!memberUserId) {
        showMessage("Please log in to purchase a plan.", "error");
        return;
    }

    const originalText = btn.textContent;
    btn.disabled = true;
    btn.textContent = 'Purchasing…';

    try {
        const data = await FitCoreApi.post(`${API_URL}/book?memberUserId=${memberUserId}&gymServiceId=${gymServiceId}`);
        showMessage('Plan purchased successfully! Check your profile for details.', 'success');

        await loadMyServices();
        loadUserServices();
    } catch (error) {
        showMessage(`Purchase failed: ${error.message}`, 'error');
        btn.disabled = false;
        btn.textContent = originalText;
    }
}
