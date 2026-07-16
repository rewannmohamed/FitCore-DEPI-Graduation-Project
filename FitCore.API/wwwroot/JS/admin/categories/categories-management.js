const CATEGORY_API = '/api/Category';

let allCategories = [];
let searchTerm = '';

let createCategoryModal, editCategoryModal;

document.addEventListener('DOMContentLoaded', () => {
    requireRole(["Admin"]);

    createCategoryModal = new bootstrap.Modal(document.getElementById('createCategoryModal'));
    editCategoryModal = new bootstrap.Modal(document.getElementById('editCategoryModal'));

    loadCategories();

    document.getElementById('categorySearchInput').addEventListener('input', (e) => {
        searchTerm = e.target.value.trim().toLowerCase();
        renderCategoriesTable(filterCategories());
    });

    document.getElementById('submitCreateCategoryBtn').addEventListener('click', submitCreateCategory);
    document.getElementById('submitEditCategoryBtn').addEventListener('click', submitEditCategory);
});

function showMessage(text, type) {
    const banner = document.getElementById('msgBanner');
    banner.textContent = text;
    banner.className = `alert alert-${type === 'success' ? 'success' : 'danger'}`;
    setTimeout(() => banner.classList.add('d-none'), 4000);
}

// =========================================================
// Load & filter
// =========================================================
async function loadCategories() {
    const tbody = document.getElementById('categoriesTableBody');
    tbody.innerHTML = `<tr><td colspan="3" class="text-center text-muted py-4">Loading categories...</td></tr>`;

    try {
        allCategories = await FitCoreApi.get(CATEGORY_API);
        renderCategoriesTable(filterCategories());
        updateStats(allCategories);
    } catch (err) {
        tbody.innerHTML = `<tr><td colspan="3" class="text-center text-danger py-4">Failed to load categories.</td></tr>`;
        showMessage(err.message, 'error');
    }
}

function filterCategories() {
    if (!searchTerm) return allCategories;
    return allCategories.filter(c => (pick(c, 'name', 'Name') || '').toLowerCase().includes(searchTerm));
}

function updateStats(categories) {
    const total = categories.length;
    const linkedProducts = categories.reduce((sum, c) => sum + (pick(c, 'productsCount', 'ProductsCount') || 0), 0);
    const empty = categories.filter(c => (pick(c, 'productsCount', 'ProductsCount') || 0) === 0).length;

    document.getElementById('statTotalCategories').textContent = total;
    document.getElementById('statTotalLinkedProducts').textContent = linkedProducts;
    document.getElementById('statEmptyCategories').textContent = empty;
}

// =========================================================
// Render
// =========================================================
function renderCategoriesTable(categories) {
    const tbody = document.getElementById('categoriesTableBody');
    tbody.innerHTML = '';

    if (!categories || categories.length === 0) {
        tbody.innerHTML = `<tr><td colspan="3" class="text-center text-muted py-5">No categories found.</td></tr>`;
        return;
    }

    categories.forEach(category => {
        const id = pick(category, 'id', 'Id');
        const name = pick(category, 'name', 'Name');
        const productsCount = pick(category, 'productsCount', 'ProductsCount') || 0;

        const badgeClass = productsCount === 0 ? 'products-count-badge empty' : 'products-count-badge';
        const badgeText = productsCount === 0 ? 'No products' : `${productsCount} product${productsCount === 1 ? '' : 's'}`;

        tbody.innerHTML += `
            <tr data-category-id="${id}">
                <td>
                    <div class="d-flex align-items-center gap-2">
                        <div class="category-icon"><i class='bx bx-category'></i></div>
                        <div class="fw-bold">${escapeHtml(name)}</div>
                    </div>
                </td>
                <td><span class="${badgeClass}">${badgeText}</span></td>
                <td class="text-end pe-4">
                    <button class="btn btn-sm text-primary me-2 border-0" data-edit-btn="${id}"><i class='bx bx-edit fs-5'></i></button>
                    <button class="btn btn-sm text-danger border-0" data-delete-btn="${id}"><i class='bx bx-trash fs-5'></i></button>
                </td>
            </tr>
        `;
    });

    wireCategoryRowActions();
}

function wireCategoryRowActions() {
    document.querySelectorAll('[data-edit-btn]').forEach(btn => {
        btn.addEventListener('click', () => openEditModal(btn.dataset.editBtn));
    });
    document.querySelectorAll('[data-delete-btn]').forEach(btn => {
        btn.addEventListener('click', () => deleteCategory(btn.dataset.deleteBtn));
    });
}

function escapeHtml(str) {
    const div = document.createElement('div');
    div.textContent = str ?? '';
    return div.innerHTML;
}

// =========================================================
// Create
// =========================================================
async function submitCreateCategory() {
    const name = document.getElementById('createCategoryName').value.trim();

    if (!name) {
        showMessage('Please enter a category name.', 'error');
        return;
    }

    try {
        await FitCoreApi.post(CATEGORY_API, { name });
        showMessage('Category created successfully.', 'success');
        createCategoryModal.hide();
        document.getElementById('createCategoryForm').reset();
        await loadCategories();
    } catch (err) {
        showMessage(err.message, 'error');
    }
}

// =========================================================
// Edit
// =========================================================
function openEditModal(categoryId) {
    const category = allCategories.find(c => Number(pick(c, 'id', 'Id')) === Number(categoryId));
    if (!category) return;

    document.getElementById('editCategoryId').value = pick(category, 'id', 'Id');
    document.getElementById('editCategoryName').value = pick(category, 'name', 'Name') || '';

    editCategoryModal.show();
}

async function submitEditCategory() {
    const id = document.getElementById('editCategoryId').value;
    const name = document.getElementById('editCategoryName').value.trim();

    if (!id) {
        showMessage('No category selected.', 'error');
        return;
    }
    if (!name) {
        showMessage('Please enter a category name.', 'error');
        return;
    }

    try {
        await FitCoreApi.put(`${CATEGORY_API}/${id}`, { name });
        showMessage('Category updated successfully.', 'success');
        editCategoryModal.hide();
        await loadCategories();
    } catch (err) {
        showMessage(err.message, 'error');
    }
}

// =========================================================
// Delete
// =========================================================
async function deleteCategory(id) {
    if (!confirm('Are you sure you want to delete this category? This cannot be undone.')) return;

    try {
        await FitCoreApi.delete(`${CATEGORY_API}/${id}`);
        showMessage('Category deleted successfully.', 'success');
        await loadCategories();
    } catch (err) {
        showMessage(err.message, 'error');
    }
}
