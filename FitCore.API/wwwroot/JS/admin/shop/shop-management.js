const SHOP_API = '/api/Shop';

let currentPage = 1;
const pageSize = 8;
let searchTerm = '';
let selectedCategory = '';

let categories = [];
let suppliers = [];
let allProducts = [];

let createProductModal, editProductModal, addStockModal;

document.addEventListener('DOMContentLoaded', () => {
    requireRole(["Admin"]);

    createProductModal = new bootstrap.Modal(document.getElementById('createProductModal'));
    editProductModal = new bootstrap.Modal(document.getElementById('editProductModal'));
    addStockModal = new bootstrap.Modal(document.getElementById('addStockModal'));

    init();

    document.getElementById('productSearchInput').addEventListener('input', (e) => {
        searchTerm = e.target.value.trim();
        currentPage = 1;
        loadProducts();
    });

    document.getElementById('productCategoryFilter').addEventListener('change', (e) => {
        selectedCategory = e.target.value;
        currentPage = 1;
        loadProducts();
    });

    document.getElementById('exportPdfBtn').addEventListener('click', () => window.print());

    document.getElementById('submitCreateProductBtn').addEventListener('click', submitCreateProduct);
    document.getElementById('submitEditProductBtn').addEventListener('click', submitEditProduct);
    document.getElementById('submitAddStockBtn').addEventListener('click', submitAddStock);

    document.getElementById('tab-inventory-btn').addEventListener('click', loadInventory);
});

async function init() {
    await Promise.all([loadCategories(), loadSuppliers()]);
    await loadProducts();
}

function showMessage(text, type) {
    const banner = document.getElementById('msgBanner');
    banner.textContent = text;
    banner.className = `alert alert-${type === 'success' ? 'success' : 'danger'}`;
    setTimeout(() => banner.classList.add('d-none'), 4000);
}

// =========================================================
// Lookups
// =========================================================
async function loadCategories() {
    try {
        categories = await FitCoreApi.get(`${SHOP_API}/categories`);
     
        const filterSelect = document.getElementById('productCategoryFilter');
        const createSelect = document.getElementById('createCategory');
        const editSelect = document.getElementById('editCategory');

        filterSelect.innerHTML = '<option value="">All Categories</option>';
        createSelect.innerHTML = '';
        editSelect.innerHTML = '';

        categories.forEach(cat => {
            const id = pick(cat, 'id', 'Id');
            const name = pick(cat, 'name', 'Name');
            filterSelect.innerHTML += `<option value="${id}">${name}</option>`;
            createSelect.innerHTML += `<option value="${id}">${name}</option>`;
            editSelect.innerHTML += `<option value="${id}">${name}</option>`;
        });

        document.getElementById('statTotalCategories').textContent = categories.length;
    } catch (err) {
        showMessage(err.message, 'error');
    }
}

async function loadSuppliers() {
    try {
        suppliers = await FitCoreApi.get(`${SHOP_API}/suppliers`);

        const createSelect = document.getElementById('createSupplier');
        const editSelect = document.getElementById('editSupplier');
        const stockSelect = document.getElementById('stockProduct');

        createSelect.innerHTML = '<option value="">No supplier</option>';
        editSelect.innerHTML = '<option value="">No supplier</option>';

        suppliers.forEach(sup => {
            const id = pick(sup, 'supplierID', 'SupplierID');
            const name = pick(sup, 'companyName', 'CompanyName');
            createSelect.innerHTML += `<option value="${id}">${name}</option>`;
            editSelect.innerHTML += `<option value="${id}">${name}</option>`;
        });
    } catch (err) {
        showMessage(err.message, 'error');
    }
}

// =========================================================
// Products
// =========================================================
async function loadProducts() {
    const tbody = document.getElementById('productsTableBody');
    let url = `${SHOP_API}/admin/products?page=${currentPage}&pageSize=${pageSize}`;
    if (searchTerm) url += `&searchTerm=${encodeURIComponent(searchTerm)}`;
    if (selectedCategory) url += `&categoryId=${selectedCategory}`;

    try {
        const result = await FitCoreApi.get(url);
        const data = pick(result, 'data', 'Data') || [];
        allProducts = data;
        console.log(result);
        const totalCount = pick(result, 'totalCount', 'TotalCount') || 0;
        const totalPages = pick(result, 'totalPages', 'TotalPages') || Math.max(1, Math.ceil(totalCount / pageSize));

        document.getElementById('statTotalProducts').textContent = totalCount;

        if (data.length === 0) {
            tbody.innerHTML = `<tr><td colspan="7" class="text-center text-muted py-5">No products match your filters.</td></tr>`;
            document.getElementById('productsPaginationSummary').textContent = '';
            document.getElementById('productsPaginationControls').innerHTML = '';
            updateStockStats([]);
            return;
        }

        renderProductsTable(data);
        wireProductRowActions();

        document.getElementById('productsPaginationSummary').textContent =
            `Showing ${(currentPage - 1) * pageSize + 1}-${Math.min(currentPage * pageSize, totalCount)} of ${totalCount} products`;

        renderPagination(totalPages);
        updateStockStats(data);
    } catch (err) {
        showMessage(err.message, 'error');
    }
}

function updateStockStats(pageData) {
    // Best-effort stats based on the loaded page; refined further once inventory tab is opened.
    const totalStock = pageData.reduce((sum, p) => sum + (pick(p, 'totalStock', 'TotalStock') || 0), 0);
    const lowStock = pageData.filter(p => (pick(p, 'totalStock', 'TotalStock') || 0) <= (pick(p, 'reorderLevel', 'ReorderLevel') || 0)).length;
    document.getElementById('statTotalStock').textContent = totalStock;
    document.getElementById('statLowStock').textContent = lowStock;
}

function stockBadge(stock, reorderLevel) {
    if (stock <= 0) return `<span class="stock-badge stock-out">Out of stock</span>`;
    if (stock <= reorderLevel) return `<span class="stock-badge stock-low">${stock} left — Low</span>`;
    return `<span class="stock-badge stock-ok">${stock} in stock</span>`;
}

function renderProductsTable(products) {
    const tbody = document.getElementById('productsTableBody');
    tbody.innerHTML = '';

    products.forEach(product => {
        const id = pick(product, 'productID', 'ProductID');
        const name = pick(product, 'name', 'Name');
        const categoryName = pick(product, 'categoryName', 'CategoryName') || '—';
        const barcode = pick(product, 'barcode', 'Barcode') || '—';
        const price = pick(product, 'currentSellPrice', 'CurrentSellPrice') || 0;
        const stock = pick(product, 'totalStock', 'TotalStock') || 0;
        const reorderLevel = pick(product, 'reorderLevel', 'ReorderLevel') || 0;
        const supplierName = pick(product, 'supplierName', 'SupplierName') || '—';
        const imageUrl = pick(product, 'imageUrl', 'ImageUrl');

        const thumb = imageUrl
            ? `<img src="${imageUrl}" class="product-thumb" alt="${name}" onerror="this.replaceWith(Object.assign(document.createElement('div'), {className:'product-thumb-fallback', innerHTML:'<i class=\\'bx bx-package\\'></i>'}))">`
            : `<div class="product-thumb-fallback"><i class='bx bx-package'></i></div>`;

        tbody.innerHTML += `
            <tr data-product-id="${id}">
                <td>
                    <div class="d-flex align-items-center gap-2">
                        ${thumb}
                        <div>
                            <div class="fw-bold">${name}</div>
                            <div class="text-muted small">${price ? parseFloat(price).toFixed(2) + ' EGP' : ''}</div>
                        </div>
                    </div>
                </td>
                <td><span class="badge bg-light text-dark border">${categoryName}</span></td>
                <td class="text-muted small">${barcode}</td>
                <td class="fw-bold text-primary">${parseFloat(price).toFixed(2)} EGP</td>
                <td>${stockBadge(stock, reorderLevel)}</td>
                <td class="text-muted small">${supplierName}</td>
                <td class="text-end pe-4">
                    <button class="btn btn-sm text-primary me-2 border-0" data-edit-btn="${id}"><i class='bx bx-edit fs-5'></i></button>
                    <button class="btn btn-sm text-danger border-0" data-delete-btn="${id}"><i class='bx bx-trash fs-5'></i></button>
                </td>
            </tr>
        `;
    });
}

function wireProductRowActions() {
    document.querySelectorAll('[data-edit-btn]').forEach(btn => {
        btn.addEventListener('click', () => openEditModal(btn.dataset.editBtn));
    });
    document.querySelectorAll('[data-delete-btn]').forEach(btn => {
        btn.addEventListener('click', () => deleteProduct(btn.dataset.deleteBtn));
    });
}

function openEditModal(productId) {
    const product = allProducts.find(p => Number(pick(p, 'productID', 'ProductID')) === Number(productId));
    if (!product) return;

    document.getElementById('editProductId').value = pick(product, 'productID', 'ProductID');
    document.getElementById('editName').value = pick(product, 'name', 'Name') || '';
    document.getElementById('editCategory').value = pick(product, 'categoryId', 'CategoryId') ?? '';
    document.getElementById('editPrice').value = pick(product, 'currentSellPrice', 'CurrentSellPrice') || 0;
    document.getElementById('editBarcode').value = pick(product, 'barcode', 'Barcode') || '';
    document.getElementById('editReorderLevel').value = pick(product, 'reorderLevel', 'ReorderLevel') || 0;
    document.getElementById('editSupplier').value = pick(product, 'supplierID', 'SupplierID') ?? '';
    document.getElementById('editImageUrl').value = pick(product, 'imageUrl', 'ImageUrl') || '';
    document.getElementById('editDescription').value = pick(product, 'description', 'Description') || '';

    editProductModal.show();
}

function readProductForm(prefix) {
    const supplierValue = document.getElementById(`${prefix}Supplier`).value;
    return {
        name: document.getElementById(`${prefix}Name`).value.trim(),
        barcode: document.getElementById(`${prefix}Barcode`).value.trim(),
        description: document.getElementById(`${prefix}Description`).value.trim(),
        currentSellPrice: parseFloat(document.getElementById(`${prefix}Price`).value) || 0,
        reorderLevel: parseInt(document.getElementById(`${prefix}ReorderLevel`).value) || 0,
        imageUrl: document.getElementById(`${prefix}ImageUrl`).value.trim() || null,
        categoryId: parseInt(document.getElementById(`${prefix}Category`).value),
        supplierID: supplierValue ? parseInt(supplierValue) : null
    };
}

async function submitCreateProduct() {
    const dto = readProductForm('create');

    if (!dto.name || !dto.categoryId) {
        showMessage('Please fill in the required fields.', 'error');
        return;
    }

    try {
        await FitCoreApi.post(`${SHOP_API}/products`, dto);
        showMessage('Product created successfully.', 'success');
        createProductModal.hide();
        document.getElementById('createProductForm').reset();
        await loadProducts();
    } catch (err) {
        showMessage(err.message, 'error');
    }
}

async function submitEditProduct() {
    const id = document.getElementById('editProductId').value;
    if (!id) {
        showMessage('No product selected.', 'error');
        return;
    }

    const dto = readProductForm('edit');

    try {
        await FitCoreApi.put(`${SHOP_API}/products/${id}`, dto);
        showMessage('Product updated successfully.', 'success');
        editProductModal.hide();
        await loadProducts();
    } catch (err) {
        showMessage(err.message, 'error');
    }
}

async function deleteProduct(id) {
    if (!confirm('Are you sure you want to delete this product? This cannot be undone.')) return;

    try {
        await FitCoreApi.delete(`${SHOP_API}/products/${id}`);
        showMessage('Product deleted successfully.', 'success');
        await loadProducts();
    } catch (err) {
        showMessage(err.message, 'error');
    }
}

function renderPagination(totalPages) {
    const container = document.getElementById('productsPaginationControls');
    container.innerHTML = '';
    for (let i = 1; i <= totalPages; i++) {
        const li = document.createElement('li');
        li.className = `page-item ${i === currentPage ? 'active' : ''}`;
        li.innerHTML = `<button class="page-link">${i}</button>`;
        li.querySelector('button').addEventListener('click', () => {
            currentPage = i;
            loadProducts();
        });
        container.appendChild(li);
    }
}

// =========================================================
// Inventory
// =========================================================
async function loadInventory() {
    const tbody = document.getElementById('inventoryTableBody');
    tbody.innerHTML = `<tr><td colspan="5" class="text-center text-muted py-4">Loading inventory...</td></tr>`;

    try {
        const [data, allProductsFull] = await Promise.all([
            FitCoreApi.get(`${SHOP_API}/inventory`),
            FitCoreApi.get(`${SHOP_API}/products`)
        ]);
        console.log(allProductsFull);
        populateStockProductSelect(allProductsFull);

        if (!data || data.length === 0) {
            tbody.innerHTML = `<tr><td colspan="5" class="text-center text-muted py-5">No stock batches recorded yet.</td></tr>`;
            return;
        }

        tbody.innerHTML = '';
        data.forEach(item => {
            const productName = pick(item, 'productName', 'ProductName');
            const quantity = pick(item, 'quantity', 'Quantity');
            const costPrice = pick(item, 'costPrice', 'CostPrice');
            const dateAdded = pick(item, 'dateAdded', 'DateAdded');
            const expiryDate = pick(item, 'expiryDate', 'ExpiryDate');

            tbody.innerHTML += `
                <tr>
                    <td class="fw-bold">${productName}</td>
                    <td>${quantity}</td>
                    <td>${parseFloat(costPrice).toFixed(2)} EGP</td>
                    <td class="text-muted small">${dateAdded ? new Date(dateAdded).toLocaleDateString() : '—'}</td>
                    <td class="text-muted small">${expiryDate ? new Date(expiryDate).toLocaleDateString() : '—'}</td>
                </tr>
            `;
        });
    } catch (err) {
        tbody.innerHTML = `<tr><td colspan="5" class="text-center text-danger py-4">Failed to load inventory.</td></tr>`;
        showMessage(err.message, 'error');
    }
}

function populateStockProductSelect(productsList) {
    const select = document.getElementById('stockProduct');
    const previousValue = select.value;

    select.innerHTML = '';
    (productsList || []).forEach(p => {
        const id = pick(p, 'productID', 'ProductID');
        const name = pick(p, 'name', 'Name');
        select.innerHTML += `<option value="${id}">${name}</option>`;
    });

    if (previousValue) select.value = previousValue;
}

async function submitAddStock() {
    const productId = parseInt(document.getElementById('stockProduct').value);
    const quantity = parseInt(document.getElementById('stockQuantity').value);
    const costPrice = parseFloat(document.getElementById('stockCostPrice').value);
    const expiryDate = document.getElementById('stockExpiryDate').value || null;

    if (!productId || !quantity) {
        showMessage('Please select a product and enter a valid quantity.', 'error');
        return;
    }

    const dto = { productId, quantity, costPrice: costPrice || 0, expiryDate };

    try {
        await FitCoreApi.post(`${SHOP_API}/inventory`, dto);
        showMessage('Stock received successfully.', 'success');
        addStockModal.hide();
        document.getElementById('addStockForm').reset();
        await loadInventory();
        await loadProducts();
    } catch (err) {
        showMessage(err.message, 'error');
    }
}
