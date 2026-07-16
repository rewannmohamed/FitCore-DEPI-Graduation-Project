// Shop page (user area) — wired to the real ShopController endpoints.
// GET    /api/Shop/products
// POST   /api/Shop/cart                body: { productID, quantity }
// GET    /api/Shop/cart
// PATCH  /api/Shop/cart/{cartItemId}   body: quantity (raw number)
// DELETE /api/Shop/cart/{cartItemId}
// POST   /api/Shop/checkout            body: { description }

const SHOP_ENDPOINTS = {
    products: '/api/Shop/products',
    cart: '/api/Shop/cart',
    cartItem: (id) => `/api/Shop/cart/${id}`,
    checkout: '/api/Shop/checkout',
};

let allProducts = [];
const pendingQuantities = {}; // productId -> chosen quantity before "Add to Cart"

document.addEventListener('DOMContentLoaded', () => {
    requireRole(["Member"]);
    loadProducts();
    refreshCartBadge();
    wireCart();
    wireSearch();
});

// ---------------------------------------------------------------
// Products
// ---------------------------------------------------------------
async function loadProducts() {
    const grid = document.getElementById('productsGrid');
    const emptyState = document.getElementById('productsEmptyState');
    const loadingState = document.getElementById('productsLoadingState');
    if (!grid) return;

    loadingState.style.display = 'block';
    emptyState.style.display = 'none';

    try {
        const products = await FitCoreApi.get(SHOP_ENDPOINTS.products);
        console.log(products);
        allProducts = products || [];
        renderProducts(allProducts);
    } catch (error) {
        console.error('Error loading products:', error);
        showBanner('shopMsgBanner', error.message || 'Could not load products. Please refresh the page.');
    } finally {
        loadingState.style.display = 'none';
    }
}

function renderProducts(products) {
    const grid = document.getElementById('productsGrid');
    const emptyState = document.getElementById('productsEmptyState');

    grid.innerHTML = '';

    if (!products || products.length === 0) {
        emptyState.style.display = 'block';
        return;
    }
    emptyState.style.display = 'none';

    products.forEach(product => grid.appendChild(buildProductCard(product)));
}

function buildProductCard(product) {
    const id = pick(product, 'productID', 'productId', 'ProductID');
    const name = pick(product, 'name', 'Name') ?? 'Unnamed product';
    const description = pick(product, 'description', 'Description') ?? '';
    const price = Number(pick(product, 'currentSellPrice', 'CurrentSellPrice') ?? 0);
    const imageUrl = pick(product, 'imageUrl', 'ImageUrl') ?? '';
    const totalStock = Number(pick(product, 'totalStock', 'TotalStock') ?? 0);
    const supplierName = pick(product, 'supplierName', 'SupplierName') ?? 'Unnamed supplier';

    if (pendingQuantities[id] === undefined) pendingQuantities[id] = 1;

    const col = document.createElement('div');
    col.className = 'col';

    const imgFile = imageUrl ? escapeAttr(imageUrl.split('/').pop()) : '';
    const outOfStock = totalStock <= 0;

    col.innerHTML = `
        <div class="product-card ${outOfStock ? 'out-of-stock' : ''}">
            <div class="product-card-img-wrap">
                ${imgFile
            ? `<img class="product-card-img" src="/images/${imgFile}" alt="${escapeAttr(name)}" onerror="this.closest('.product-card-img-wrap').innerHTML='<i class=\\'fa-solid fa-box-open fallback-icon\\'></i>'">`
            : `<i class="fa-solid fa-box-open fallback-icon"></i>`}
                ${outOfStock ? `<span class="out-of-stock-badge">Out of Stock</span>` : ''}
            </div>
            <h3 class="product-name" title="${escapeAttr(name)}">${escapeHtml(name)}</h3>
            <p class="product-desc">${escapeHtml(description)}</p>
            <p class="product-supplier"><i class="fa-solid fa-truck"></i> ${escapeHtml(supplierName)}</p>
            <span class="product-price">${formatCurrency(price)}</span>
            <div class="product-actions">
                <div class="qty-stepper">
                    <button type="button" class="qty-minus" aria-label="Decrease quantity" ${outOfStock ? 'disabled' : ''}>-</button>
                    <span class="qty-value">${pendingQuantities[id]}</span>
                    <button type="button" class="qty-plus" aria-label="Increase quantity" ${outOfStock ? 'disabled' : ''}>+</button>
                </div>
                <button type="button" class="btn btn-shop-primary btn-sm flex-grow-1 add-to-cart-btn" ${outOfStock ? 'disabled' : ''}>
                    ${outOfStock ? 'Out of Stock' : 'Add to Cart'}
                </button>
            </div>
        </div>
    `;

    const qtyValueEl = col.querySelector('.qty-value');
    col.querySelector('.qty-minus').addEventListener('click', () => {
        pendingQuantities[id] = Math.max(1, pendingQuantities[id] - 1);
        qtyValueEl.innerText = pendingQuantities[id];
    });
    col.querySelector('.qty-plus').addEventListener('click', () => {
        pendingQuantities[id] = pendingQuantities[id] + 1;
        qtyValueEl.innerText = pendingQuantities[id];
    });

    const addBtn = col.querySelector('.add-to-cart-btn');
    if (!outOfStock) {
        addBtn.addEventListener('click', () => addToCart(id, totalStock, pendingQuantities[id], addBtn));
    }

    return col;
}

async function addToCart(productId, totalStock ,quantity, buttonEl) {
    if (buttonEl) {
        buttonEl.disabled = true;
        buttonEl.innerText = 'Adding…';
    }
    if (quantity > totalStock) {
        showToast(`Requested quantity exceeds available stock. Only ${totalStock} available.`, isError = true);
        return null;
    }
    try {
        await FitCoreApi.post(SHOP_ENDPOINTS.cart, { productID: productId, quantity });
        showToast('Added to cart');  
        await refreshCartBadge();
    } catch (error) {
        console.error('Error adding to cart:', error);
        showToast(error.message || 'Could not add product to cart', true);
    } finally {
        if (buttonEl) {
            buttonEl.disabled = false;
            buttonEl.innerText = 'Add to Cart';
        }
    }
}

// ---------------------------------------------------------------
// Search
// ---------------------------------------------------------------
function wireSearch() {
    const input = document.getElementById('productSearchInput');
    if (!input) return;

    input.addEventListener('input', () => {
        const term = input.value.trim().toLowerCase();
        if (!term) {
            renderProducts(allProducts);
            return;
        }
        const filtered = allProducts.filter(p => {
            const name = (pick(p, 'name', 'Name') ?? '').toLowerCase();
            const description = (pick(p, 'description', 'Description') ?? '').toLowerCase();
            return name.includes(term) || description.includes(term);
        });
        renderProducts(filtered);
    });
}

// ---------------------------------------------------------------
// Cart (Bootstrap offcanvas)
// ---------------------------------------------------------------
function wireCart() {
    const cartOffcanvasEl = document.getElementById('cartOffcanvas');
    const checkoutBtn = document.getElementById('checkoutBtn');

    cartOffcanvasEl?.addEventListener('show.bs.offcanvas', loadCart);
    checkoutBtn?.addEventListener('click', checkout);
}

async function loadCart() {
    const container = document.getElementById('cartItems');
    const emptyState = document.getElementById('cartEmptyState');
    if (!container) return;

    try {
        const items = await FitCoreApi.get(SHOP_ENDPOINTS.cart);
        renderCartItems(items || []);
    } catch (error) {
        console.error('Error loading cart:', error);
        container.innerHTML = '';
        if (emptyState) {
            emptyState.style.display = 'block';
            emptyState.innerText = 'Could not load your cart.';
        }
    }
}

function renderCartItems(items) {
    const container = document.getElementById('cartItems');
    const emptyState = document.getElementById('cartEmptyState');

    container.innerHTML = '';

    if (!items || items.length === 0) {
        if (emptyState) {
            emptyState.style.display = 'block';
            emptyState.innerText = 'Your cart is empty.';
        }
        updateCartTotal(items);
        updateCartBadgeCount(0);
        return;
    }
    if (emptyState) emptyState.style.display = 'none';

    items.forEach(item => container.appendChild(buildCartItemRow(item)));

    updateCartTotal(items);
    updateCartBadgeCount(items.reduce((sum, i) => sum + Number(pick(i, 'quantity', 'Quantity') ?? 0), 0));
}

function buildCartItemRow(item) {
    const cartItemId = pick(item, 'cartItemID', 'cartItemId', 'CartItemID');
    const name = pick(item, 'productName', 'ProductName') ?? 'Item';
    const quantity = Number(pick(item, 'quantity', 'Quantity') ?? 1);
    const unitPrice = Number(pick(item, 'unitPrice', 'UnitPrice') ?? 0);
    const imageUrl = pick(item, 'imageUrl', 'ImageUrl') ?? '';
    const imgFile = imageUrl ? escapeAttr(imageUrl.split('/').pop()) : '';

    const row = document.createElement('div');
    row.className = 'cart-item';
    row.innerHTML = `
        <div class="cart-item-img-wrap">
            ${imgFile
            ? `<img src="/images/${imgFile}" alt="${escapeAttr(name)}" onerror="this.parentElement.innerHTML='<i class=\\'fa-solid fa-box-open fallback-icon\\'></i>'">`
            : `<i class="fa-solid fa-box-open fallback-icon"></i>`}
        </div>
        <div class="cart-item-info">
            <span class="cart-item-name">${escapeHtml(name)}</span>
            <span class="cart-item-price">${formatCurrency(unitPrice)} each</span>
            <div class="cart-item-controls">
                <div class="qty-stepper">
                    <button type="button" class="qty-minus" aria-label="Decrease quantity">-</button>
                    <span class="qty-value">${quantity}</span>
                    <button type="button" class="qty-plus" aria-label="Increase quantity">+</button>
                </div>
                <button type="button" class="cart-item-remove">Remove</button>
            </div>
        </div>
    `;

    const qtyValueEl = row.querySelector('.qty-value');
    row.querySelector('.qty-minus').addEventListener('click', () => {
        const next = quantity - 1;
        if (next <= 0) {
            removeCartItem(cartItemId);
        } else {
            updateCartItemQuantity(cartItemId, next);
        }
    });
    row.querySelector('.qty-plus').addEventListener('click', () => {
        updateCartItemQuantity(cartItemId, quantity + 1);
    });
    row.querySelector('.cart-item-remove').addEventListener('click', () => removeCartItem(cartItemId));

    return row;
}

function updateCartTotal(items) {
    const totalEl = document.getElementById('cartTotal');
    if (!totalEl) return;
    const total = (items || []).reduce((sum, i) => {
        const qty = Number(pick(i, 'quantity', 'Quantity') ?? 0);
        const price = Number(pick(i, 'unitPrice', 'UnitPrice') ?? 0);
        return sum + qty * price;
    }, 0);
    totalEl.innerText = formatCurrency(total);
}

async function updateCartItemQuantity(cartItemId, newQuantity) {
    try {
        await FitCoreApi.request('PATCH', SHOP_ENDPOINTS.cartItem(cartItemId), newQuantity);
        loadCart();
    } catch (error) {
        console.error('Error updating quantity:', error);
        showToast(error.message || 'Could not update quantity', true);
    }
}

async function removeCartItem(cartItemId) {
    try {
        await FitCoreApi.delete(SHOP_ENDPOINTS.cartItem(cartItemId));
        loadCart();
        showToast('Item removed');
    } catch (error) {
        console.error('Error removing item:', error);
        showToast(error.message || 'Could not remove item', true);
    }
}

async function refreshCartBadge() {
    try {
        const items = await FitCoreApi.get(SHOP_ENDPOINTS.cart);
        updateCartBadgeCount((items || []).reduce((sum, i) => sum + Number(pick(i, 'quantity', 'Quantity') ?? 0), 0));
    } catch (error) {
        console.error('Error refreshing cart badge:', error);
    }
}

function updateCartBadgeCount(count) {
    const badge = document.getElementById('cartCount');
    if (!badge) return;
    badge.innerText = count;
    badge.style.display = count > 0 ? 'inline-block' : 'none';
}

// ---------------------------------------------------------------
// Checkout
// ---------------------------------------------------------------
async function checkout() {
    const checkoutBtn = document.getElementById('checkoutBtn');
    const description = document.getElementById('checkoutDescription')?.value || '';

    // Capture the cart total *before* checkout clears it, so we can hand it
    // off to the Invoice/payment page (the API has no "get invoice by id"
    // endpoint yet, so this is the only way that page can know the amount).
    const totalText = document.getElementById('cartTotal')?.innerText || '';
    const totalAmount = Number(totalText.replace(/[^0-9.]/g, '')) || 0;

    checkoutBtn.disabled = true;
    checkoutBtn.innerText = 'Processing…';

    try {
        const result = await FitCoreApi.post(SHOP_ENDPOINTS.checkout, { description });
        const invoiceId = pick(result, 'invoiceId', 'InvoiceId');

        // Hand the invoice off to the payment page and navigate there.
        sessionStorage.setItem('fitcore_pending_invoice', JSON.stringify({
            invoiceId: invoiceId,
            amount: totalAmount,
            description: description || 'Shop order',
        }));

        window.location.href = '/html/Invoice/Payment.html';
    } catch (error) {
        console.error('Error during checkout:', error);
        showToast(error.message || 'Checkout failed', true);
        checkoutBtn.disabled = false;
        checkoutBtn.innerText = 'Checkout';
    }
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

function escapeAttr(str) {
    return String(str).replace(/"/g, '&quot;');
}

function showBanner(elId, message) {
    const banner = document.getElementById(elId);
    if (!banner) return;
    banner.innerText = message;
    banner.style.display = 'block';
}

let shopToastInstance;
function showToast(message, isError = false) {
    const toastEl = document.getElementById('shopToast');
    const bodyEl = document.getElementById('shopToastBody');
    if (!toastEl || !bodyEl) return;

    bodyEl.innerText = message;
    toastEl.classList.toggle('text-bg-danger', isError);
    toastEl.classList.toggle('text-bg-dark', !isError);

    if (!shopToastInstance) {
        shopToastInstance = new bootstrap.Toast(toastEl, { delay: 2500 });
    }
    shopToastInstance.show();
}