const API_URL = '/api/GymServices';
let currentPage = 1;
const pageSize = 5;
let searchTerm = '';
let category = '';
let modalInst = null;

let createServiceModal;
let allClasses = [];

const token = getToken();

let editServiceId = null;
let editServiceModal;
const categories = { 0: 'Memberships', 1: 'Personal Training'};

document.addEventListener('DOMContentLoaded', () => {
    requireRole(["Admin"]);
    createServiceModal = new bootstrap.Modal(document.getElementById('createServiceModal'));
    loadAdminTable();

    editServiceModal = new bootstrap.Modal(
        document.getElementById('editServiceModal')
    );

    document.getElementById('adminSearchInput').addEventListener('input', (e) => {
        searchTerm = e.target.value.trim();
        page = 1;
        loadAdminTable();
    });
    document.getElementById('exportPdfBtn').addEventListener('click', () => window.print());
    document.getElementById('adminCategoryFilter').addEventListener('change', (e) => {
        category = e.target.value;
        page = 1;
        loadAdminTable();
    });
    document.getElementById('submitCreateServiceBtn').addEventListener('click', submitCreateService);
    document.getElementById('submitEditServiceBtn').addEventListener('click', submitEditService);
});

function showMessage(text, type) {
    const banner = document.getElementById('msgBanner');
    banner.textContent = text;
    banner.className = `alert alert-${type === 'success' ? 'success' : 'danger'}`;
    setTimeout(() => banner.classList.add('d-none'), 4000);
}

async function loadAdminTable() {
    console.log(category);
    let url = `${API_URL}?page=${currentPage}&pageSize=${pageSize}`;
    if (searchTerm) url += `&searchTerm=${encodeURIComponent(searchTerm)}`;
    if (category !== '') url += `&category=${category}`;
    const tbody = document.getElementById('adminTableBody');
    try {

        const result = await authFetch(url);
        const data = result.data || result;
        console.log(result);
        allServices = result.data || result;
        if (data.length === 0) {
            tbody.innerHTML = `<tr><td colspan="6" class="text-center text-muted py-5">No gym sevices match your filters.</td></tr>`;
            document.getElementById('paginationSummary').textContent = '';
            document.getElementById('paginationControls').innerHTML = '';
            return;
        }

        const totalCount = result.totalCount || data.length;
        const totalPages = result.totalPages || Math.max(1, Math.ceil(data.length / pageSize));;
        if (currentPage > totalPages) currentPage = totalPages;
        const pageItems = data.slice((currentPage - 1) * pageSize, currentPage * pageSize);


        document.getElementById('adminTotalCount').textContent = totalCount;
        renderTableRows(data);

        document.getElementById('paginationSummary').textContent =
            `Showing ${(currentPage - 1) * pageSize + 1}-${Math.min(currentPage * pageSize, data.length)} of ${data.length} classes`;

        renderPagination(totalPages);
        wireRowActions();
    } catch (err) {
        console.error("Failed to fetch admin registry:", err);
    }
}

function renderTableRows(services) {
    const tbody = document.getElementById('adminTableBody');
    tbody.innerHTML = '';

    if (!services || services.length === 0) {
        tbody.innerHTML = `<tr><td colspan="7" class="text-center py-4 text-muted">No records matched the system parameters.</td></tr>`;
        return;
    }

    services.forEach(service => {
        tbody.innerHTML += `
            <tr data-service-id="${service.serviceID}">
                <td class="ps-4 text-muted fw-bold">#${service.serviceID}</td>
                <td class="fw-bold">${service.name}</td>
                <td><span class="badge bg-light text-dark border px-2.5 py-1.5">${categories[service.category]}</span></td>
                <td class="fw-bold text-primary">${parseFloat(service.price).toFixed(2)} EGP</td>
                <td>${service.durationInDays} Days</td>
                <td>${service.allowedSessionsCount} Sessions</td>
                <td class="text-end pe-4">
                    <button class="btn btn-sm text-primary me-2 border-0" data-bs-toggle="modal" data-bs-target="#editServiceModal" data-edit-btn="${service.serviceID}"><i class='bx bx-edit fs-5'></i></button>
                    <button class="btn btn-sm text-danger border-0" onclick="deleteServiceAsset(${service.serviceID})"><i class='bx bx-trash fs-5'></i></button>
                </td>
            </tr>
        `;

    });
}

function wireRowActions() {
    document.querySelectorAll('[data-edit-btn]').forEach(btn => {

        btn.addEventListener('click', () => {

            editServiceId = btn.dataset.editBtn;

            const gymService = allServices.find(c =>
                Number(pick(c, 'serviceID', 'ServiceID')) === Number(editServiceId)
            );

            if (!gymService) return;


            document.getElementById('editServiceName').value =
                pick(gymService, 'name', 'Name') || '';

            document.getElementById('editServiceCategory').value =
                pick(gymService, 'category', 'Category') ?? 1;

            document.getElementById('editServicePrice').value =
                pick(gymService, 'price', 'Price') || 0;

            document.getElementById('editServiceSessions').value =
                pick(gymService, 'allowedSessionsCount', 'AllowedSessionsCount') || 1;

            document.getElementById('editServiceDuration').value =
                pick(gymService, 'durationInDays', 'DurationInDays');

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


async function submitCreateService() {
   
    const dto = {
        name: document.getElementById('serviceName').value.trim(),
        price: parseFloat(document.getElementById('formPrice').value),
        category: parseInt(document.getElementById('formCategory').value),
        durationInDays: parseInt(document.getElementById('formDuration').value),
        allowedSessionsCount: parseInt(document.getElementById('formSessions').value)
    };

    try {
        await FitCoreApi.post(API_URL, dto);
        showMessage('Service created.', 'success');
        createServiceModal.hide();
        document.getElementById('serviceName').value = '';
        document.getElementById('formCategory').value = '';
        document.getElementById('formDuration').value = '';
        document.getElementById('formSessions').value = '';

        await loadAdminTable();
    } catch (error) {
        showMessage(error.message, 'error');
    }
}

async function submitEditService() {

    if (!editServiceId) {
        showMessage('No class selected', 'error');
        return;
    }


    const dto = {
        name: document.getElementById('editServiceName').value,
        category: Number(
            document.getElementById('editServiceCategory').value
        ) || 0,
        price: parseInt(
            document.getElementById('editServicePrice').value
        ) || 2000,

        allowedSessionsCount: parseInt(
            document.getElementById('editServiceDuration').value
        ) || 1,
        durationInDays: parseInt(
            document.getElementById('editServiceDuration').value
        )
    };


    try {

        await FitCoreApi.put(
            `/api/GymServices/${editServiceId}`,
            dto
        );


        showMessage(
            'Service updated successfully.',
            'success'
        );


        editServiceModal.hide();

        editServiceId = null;

        await loadAdminTable();


    } catch (error) {

        showMessage(
            error.message,
            'error'
        );

    }
}

async function deleteServiceAsset(id) {
   
    try {

        await FitCoreApi.delete(`${API_URL}/${id}`);

        await loadAdminTable();

        showMessage('Service deleted successfully.', 'success');

    } catch (err) {
        showMessage(err.message, "error");
    }
}