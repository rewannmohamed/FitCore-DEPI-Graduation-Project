document.addEventListener('DOMContentLoaded', () => {
    const token = getToken();

    if (!token) {
        window.location.href = '/html/Auth/login.html';
        return;
    }

    // الـ Endpoints الخاصة بيكي
    const API_BASE = 'http://localhost:5184/api';

    // العناصر في الـ DOM
    const cartContainer = document.getElementById('cartItemsContainer');
    const bookingsContainer = document.getElementById('bookingItemsContainer');
    const productsTotalEl = document.getElementById('productsTotal');
    const bookingsTotalEl = document.getElementById('bookingsTotal');
    const grandTotalEl = document.getElementById('grandTotal');
    const checkoutBtn = document.getElementById('checkoutBtn');

    let productsSubtotal = 0;
    let bookingsSubtotal = 0;

    // تهيئة الصفحة
    async function initPage() {
        await fetchCartItems();
        await fetchBookings();
        updateGrandTotal();
    }

    // ==========================================
    // 1. إدارة سلة المشتريات (Products)
    // ==========================================
    async function fetchCartItems() {
        try {
            // نداء للـ API بناءً على [HttpGet("cart")] في الكنترولر بتاعك
            const response = await fetch(`${API_BASE}/Shop/cart`, {
                headers: { 'Authorization': `Bearer ${token}` }
            });

            if (response.ok) {
                const cartItems = await response.json();
                renderCartItems(cartItems);
            } else {
                cartContainer.innerHTML = '<p class="text-muted">Failed to load cart items.</p>';
            }
        } catch (error) {
            console.error("Error fetching cart:", error);
        }
    }

    function renderCartItems(items) {
        productsSubtotal = 0;
        cartContainer.innerHTML = '';

        if (!items || items.length === 0) {
            cartContainer.innerHTML = '<p class="text-muted">Your cart is empty.</p>';
            productsTotalEl.textContent = '$0.00';
            return;
        }

        items.forEach(item => {
            const itemTotal = item.unitPrice * item.quantity;
            productsSubtotal += itemTotal;

            const div = document.createElement('div');
            div.className = 'item-card';
            div.innerHTML = `
                <div class="item-info">
                    <img src="${item.imageUrl || '/assets/default-product.png'}" alt="${item.productName}" class="item-image">
                    <div class="item-details">
                        <h4>${item.productName}</h4>
                        <p>$${item.unitPrice.toFixed(2)} each</p>
                    </div>
                </div>
                <div class="item-actions">
                    <div class="qty-controls">
                        <button class="qty-btn minus-btn" data-id="${item.cartItemID}" data-qty="${item.quantity - 1}">-</button>
                        <input type="text" class="qty-input" value="${item.quantity}" readonly>
                        <button class="qty-btn plus-btn" data-id="${item.cartItemID}" data-qty="${item.quantity + 1}">+</button>
                    </div>
                    <div class="item-price">$${itemTotal.toFixed(2)}</div>
                    <button class="remove-btn remove-cart-btn" data-id="${item.cartItemID}" title="Remove">
                        <i class="fa-solid fa-trash-can"></i>
                    </button>
                </div>
            `;
            cartContainer.appendChild(div);
        });

        productsTotalEl.textContent = `$${productsSubtotal.toFixed(2)}`;
        attachCartEvents();
    }

    function attachCartEvents() {
        // تحديث الكمية (Update Quantity)
        document.querySelectorAll('.qty-btn').forEach(btn => {
            btn.addEventListener('click', async (e) => {
                const cartItemId = e.target.dataset.id;
                const newQty = parseInt(e.target.dataset.qty);
                if (newQty > 0) {
                    await updateCartQuantity(cartItemId, newQty);
                }
            });
        });

        // حذف من السلة (Remove from Cart)
        document.querySelectorAll('.remove-cart-btn').forEach(btn => {
            btn.addEventListener('click', async (e) => {
                const cartItemId = e.currentTarget.dataset.id;
                await removeCartItem(cartItemId);
            });
        });
    }

    async function updateCartQuantity(cartItemId, quantity) {
        try {
            await fetch(`${API_BASE}/Shop/cart/${cartItemId}`, {
                method: 'PATCH',
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(quantity)
            });
            await fetchCartItems(); // إعادة تحميل السلة بعد التحديث
            updateGrandTotal();
        } catch (error) { console.error(error); }
    }

    async function removeCartItem(cartItemId) {
        try {
            await fetch(`${API_BASE}/Shop/cart/${cartItemId}`, {
                method: 'DELETE',
                headers: { 'Authorization': `Bearer ${token}` }
            });
            await fetchCartItems();
            updateGrandTotal();
        } catch (error) { console.error(error); }
    }

    // ==========================================
    // 2. إدارة الحجوزات (Bookings)
    // ==========================================
    async function fetchBookings() {
        try {
            // افترضت إن عندك API بيجيب حجوزات اليوزر اللي لسه مادفعش تمنها
            // (عدلي المسار ده لو مختلف عندك في الـ Controller)
            const response = await fetch(`${API_BASE}/GymServices/bookings`, {
                headers: { 'Authorization': `Bearer ${token}` }
            });

            if (response.ok) {
                const bookings = await response.json();
                renderBookings(bookings);
            } else {
                bookingsContainer.innerHTML = '<p class="text-muted">Failed to load bookings.</p>';
            }
        } catch (error) {
            console.error("Error fetching bookings:", error);
        }
    }

    function renderBookings(bookings) {
        bookingsSubtotal = 0;
        bookingsContainer.innerHTML = '';

        if (!bookings || bookings.length === 0) {
            bookingsContainer.innerHTML = '<p class="text-muted">You have no unpaid bookings.</p>';
            bookingsTotalEl.textContent = '$0.00';
            return;
        }

        bookings.forEach(booking => {
            bookingsSubtotal += booking.price;

            // تحديد أيقونة ونوع بناءً على نوع الحجز (Class أو Gym Service)
            const isClass = booking.itemType === "Class";
            const iconClass = isClass ? "fa-dumbbell" : "fa-calendar-days";

            const div = document.createElement('div');
            div.className = 'item-card';
            div.innerHTML = `
                <div class="item-info">
                    <div class="item-icon"><i class="fa-solid ${iconClass}"></i></div>
                    <div class="item-details">
                        <h4>${booking.bookedItemName} <span class="badge">${booking.itemType}</span></h4>
                        <p>Booking ID: #${booking.bookingID} | Trainer: ${booking.trainerName}</p>
                    </div>
                </div>
                <div class="item-actions">
                    <div class="item-price">$${booking.price.toFixed(2)}</div>
                    <button class="remove-btn cancel-booking-btn" data-id="${booking.bookingID}" data-type="${booking.itemType}" title="Cancel Booking">
                        <i class="fa-solid fa-xmark"></i>
                    </button>
                </div>
            `;
            bookingsContainer.appendChild(div);
        });

        bookingsTotalEl.textContent = `$${bookingsSubtotal.toFixed(2)}`;
        attachBookingEvents();
    }

    function attachBookingEvents() {
        document.querySelectorAll('.cancel-booking-btn').forEach(btn => {
            btn.addEventListener('click', async (e) => {
                const bookingId = e.currentTarget.dataset.id;
                //const type = e.currentTarget.dataset.type;
                //const userId = localStorage.getItem('userId');

                // جوه attachBookingEvents
                if (confirm('Are you sure you want to cancel this booking?')) {
                    const response = await fetch(`${API_BASE}/GymServices/bookings/${bookingId}/cancel`, {
                        method: 'DELETE',
                        headers: { 'Authorization': `Bearer ${token}` }
                    });

                    if (response.ok) {
                        await fetchBookings();
                        updateGrandTotal();
                    } else {
                        alert("Failed to cancel this booking.");
                    }
                }
            });
        });
    }

    // ==========================================
    // 3. الحساب النهائي والـ Checkout
    // ==========================================
    function updateGrandTotal() {
        const grandTotal = productsSubtotal + bookingsSubtotal;
        grandTotalEl.textContent = `$${grandTotal.toFixed(2)}`;

        // تعطيل الزر لو مفيش حاجة تندفع
        checkoutBtn.disabled = grandTotal === 0;
        if (grandTotal === 0) {
            checkoutBtn.style.opacity = '0.5';
        } else {
            checkoutBtn.style.opacity = '1';
        }
    }

    checkoutBtn.addEventListener('click', async () => {
        try {
            // 1. تغيير شكل الزرار لـ Loading لمنع التكرار
            checkoutBtn.disabled = true;
            checkoutBtn.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Processing...';

            // ⚠️ ملحوظة: تأكدي من اسم الـ Controller بتاعك هنا، أنا افترضت إنه Checkout
            // 2. مناداة الـ API الأول لإنشاء الفاتورة
            const processResponse = await fetch(`${API_BASE}/Checkout/process`, {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json'
                }
            });

            if (!processResponse.ok) {
                const errorData = await processResponse.json();
                throw new Error(errorData.message || "Failed to process checkout.");
            }

            const processData = await processResponse.json();
            const invoiceId = processData.invoiceId;

            if (!invoiceId) {
                throw new Error("Server did not return an Invoice ID.");
            }

            // 3. مناداة الـ API الثاني لإنشاء رابط الدفع الخاص بـ Stripe
            const sessionResponse = await fetch(`${API_BASE}/Payments/create-checkout-session`, {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ invoiceID: invoiceId }) // بنبعت الـ ID في الـ Body زي ما الـ C# طالب
            });

            if (!sessionResponse.ok) {
                const errorData = await sessionResponse.json();
                throw new Error(errorData.error || "Failed to create payment session.");
            }

            const sessionData = await sessionResponse.json();

            // عشان نتفادى اختلاف حالة الحروف بين الـ C# والـ JS
            const sessionUrl = sessionData.sessionUrl || sessionData.SessionUrl;

            if (!sessionUrl) {
                throw new Error("Stripe Session URL not found.");
            }

            // 4. توجيه اليوزر لصفحة الدفع الخاصة بـ Stripe
            window.location.href = sessionUrl;

        } catch (error) {
            console.error("Checkout Error:", error);
            alert("Checkout Error: " + error.message);

            // إرجاع الزرار لشكله الطبيعي لو حصل إيرور
            checkoutBtn.disabled = false;
            checkoutBtn.innerHTML = 'Proceed to Checkout <i class="fa-solid fa-arrow-right"></i>';
        }
    });

    // Start fetching
    initPage();
});
