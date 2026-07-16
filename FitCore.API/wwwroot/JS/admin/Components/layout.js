async function loadComponent(elementId, componentPath) {
    try {
        const response = await fetch(componentPath);
        const html = await response.text();
        document.getElementById(elementId).innerHTML = html;
    } catch (error) {
        console.error(`Error loading ${componentPath}:`, error);
    }
}

function setActiveSidebarLink() {
    const currentPage = document.body.dataset.page;
    if (!currentPage) return;

    const currentPageLower = currentPage.toLowerCase();

    document.querySelectorAll('.sidebar-nav ul li').forEach(item => {
        if (item.dataset.page) {
            const itemPageLower = item.dataset.page.toLowerCase();

            const isMatch = itemPageLower === currentPageLower;
            item.classList.toggle('active', isMatch);

        }
    });
}

function initTrainerLayout() {
    console.log("🎬 Trainer layout started initializing...");

    fetch('/HTML/admin/Components/sidebar.html')
        .then(response => response.text())
        .then(html => {
            const sidebarContainer = document.getElementById('sidebar-container');
            if (sidebarContainer) {
                sidebarContainer.innerHTML = html;
            }

            if (typeof setActiveSidebarLink === 'function') {
                setActiveSidebarLink();
            }
        })
        .catch(error => console.error('Error loading sidebar:', error));


    loadComponent('header-container', '/HTML/admin/Components/header.html').then(() => {


        if (typeof initNotificationSystem === 'function') {
            initNotificationSystem();
        }


        const toggleBtn = document.querySelector('.sidebar-toggle-btn');
        const sidebar = document.getElementById('sidebar-container');


        if (toggleBtn && sidebar) {
            toggleBtn.addEventListener('click', () => {
                sidebar.classList.toggle('open');
            });
        }


        document.addEventListener('click', (event) => {
            if (sidebar && sidebar.classList.contains('open') && toggleBtn) {
                if (!sidebar.contains(event.target) && !toggleBtn.contains(event.target)) {
                    sidebar.classList.remove('open');
                }
            }
        });


        if (typeof loadName === 'function') {
            loadName();
        }
    });
}

if (document.readyState === "complete" || document.readyState === "interactive") {
    initTrainerLayout();
} else {
    document.addEventListener("DOMContentLoaded", initTrainerLayout);
}



// المتغيرات الأساسية للـ Pagination
let notifPage = 1;
const notifPageSize = 10;

// استدعاء الدالة دي بعد ما الـ Header HTML يترسم في الشاشة
function initNotificationSystem() {
    const bellBtn = document.getElementById('notificationBellBtn');
    const panel = document.getElementById('notificationPanel');
    const closeBtn = document.getElementById('closeNotificationBtn');
    const loadMoreBtn = document.getElementById('loadMoreNotifsBtn');
    const markAllReadBtn = document.getElementById('markAllReadBtn');

    // 1. فتح وقفل البانل
    bellBtn.addEventListener('click', () => {
        panel.classList.add('open');
        // لو أول مرة يفتح، نحمل الداتا
        if (notifPage === 1 && document.getElementById('notificationList').innerHTML.trim() === '') {
            fetchNotifications(notifPage);
        }
    });

    closeBtn.addEventListener('click', () => {
        panel.classList.remove('open');
    });

    // 2. زرار Load More
    loadMoreBtn.addEventListener('click', () => {
        notifPage++;
        fetchNotifications(notifPage, true);
    });

    // 3. Mark All as Read
    markAllReadBtn.addEventListener('click', async () => {
        try {
            await authFetch('/api/Notification/mark-all-read', { method: 'PATCH' });
            // await fetch('/api/Notification/mark-all-read', { method: 'PATCH' });

            // نخلي كل الإشعارات اللي في الشاشة مقروءة
            document.querySelectorAll('.notification-item.unread').forEach(item => {
                item.classList.remove('unread');
            });
            updateBadge(0); // نخفي النقطة الحمرا
        } catch (error) {
            console.error("Error marking all as read", error);
        }
    });

    // نحمل أول صفحة في الخلفية عشان نعرف في إشعارات جديدة ولا لأ
    fetchNotifications(1);
    setInterval(pollUnreadCount, 30000);
}

// دالة جلب الإشعارات من الـ API
async function fetchNotifications(page, append = false) {
    try {
        const response = await authFetch(`/api/Notification?Page=${page}&Page_Size=${notifPageSize}`);
        // const response = await fetch(`/api/Notification?Page=${page}&Page_Size=${notifPageSize}`);

        const data = response;
        // افتراض إن الـ API بيرجع { data: [...], totalCount: 50, unreadCount: 5 }
        const notifications = data.data || data.Data || [];

        renderNotifications(notifications, append);

        // تحديث زرار Load More لو الداتا خلصت
        const totalCount = data.totalCount ?? data.TotalCount ?? 0;
        const footer = document.getElementById('notificationFooter');
        if (page * notifPageSize >= totalCount) {
            footer.style.display = 'none';
        } else {
            footer.style.display = 'block';
        }

        // لو الـ API بيبعت عدد الـ unread في الـ Response، نحدث البادج (النقطة)
        // أو نقدر نحسبهم من اللستة كحل مؤقت لو مش مبعوت
        const unreadCount = data.unreadCount ?? notifications.filter(n => !n.isRead && !n.IsRead).length;
        if (page === 1) updateBadge(unreadCount);

    } catch (error) {
        console.error("Error fetching notifications", error);
    }
}

// دالة رسم الإشعارات
function renderNotifications(notifications, append) {
    const list = document.getElementById('notificationList');
    if (!append) list.innerHTML = ''; // لو مش بنعمل Load More، امسح القديم

    if (notifications.length === 0 && !append) {
        list.innerHTML = '<p style="text-align:center; color: var(--text-muted); margin-top: 20px;">No notifications yet.</p>';
        return;
    }

    notifications.forEach(notif => {
        // تظبيط حالة الحروف حسب الـ JSON
        const id = notif.id || notif.Id || notif.notificationId || notif.NotificationId;
        const title = notif.title || notif.Title;
        const message = notif.message || notif.Message;
        const isRead = notif.isRead || notif.IsRead;
        const createdAt = notif.createdAt || notif.CreatedAt;

        // تنسيق الوقت
        const type = notif.type ?? notif.Type;
        const notifStyle = getNotificationStyle(type);

        const dateObj = new Date(createdAt);
        const timeString = dateObj.toLocaleDateString() + ' ' + dateObj.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

        const item = document.createElement('div');
        item.className = `notification-item ${isRead ? '' : 'unread'}`;
        item.innerHTML = `
            <div class="notif-icon">
                <i class="fa-solid fa-info"></i>
            </div>
            <div class="notif-content">
                <div class="notif-title">${title}</div>
                <div class="notif-desc">${message}</div>
                <div class="notif-time">${timeString}</div>
            </div>
        `;

        item.addEventListener('click', async () => {
            if (item.classList.contains('unread')) {
                try {
                    const response = await fetch(`/api/Notification/mark-as-read/${id}`, { method: 'PATCH' });

                    if (response.ok) {
                        item.classList.remove('unread'); // 1. نشيل اللون 

                        // 👇 2. السطرين الجداد: نعد الإشعارات اللي لسه زرقاء ونحدث النقطة فوراً
                        const remainingUnread = document.querySelectorAll('.notification-item.unread').length;
                        updateBadge(remainingUnread);

                    } else {
                        console.error("Error from backend:", await response.text());
                        alert("Failed to mark as read! Check console.");
                    }
                } catch (err) { console.error(err); }
            }
        });

        list.appendChild(item);
    });
}

// دالة لإظهار/إخفاء النقطة الحمرا
function updateBadge(unreadCount) {
    const badge = document.getElementById('unreadBadge');
    if (unreadCount > 0) {
        badge.style.display = 'block';
    } else {
        badge.style.display = 'none';
    }
}

// دالة بتحدد الأيقونة واللون بناءً على الـ Enum
function getNotificationStyle(type) {
    // بنحول النوع لـ String عشان لو الباك إند بعته كرقم (0,1,2,3) أو كنص
    const typeStr = String(type).toLowerCase();

    if (typeStr === '0' || typeStr === 'membershipexpiration') {
        // اشتراك هينتهي -> أيقونة كارت أحمر
        return { icon: 'fa-solid fa-id-card-clip', colorClass: 'icon-danger' };
    }
    else if (typeStr === '1' || typeStr === 'productexpiry') {
        // منتج هتنتهي صلاحيته -> أيقونة نتيجة حمراء
        return { icon: 'fa-solid fa-calendar-xmark', colorClass: 'icon-danger' };
    }
    else if (typeStr === '2' || typeStr === 'announcement') {
        // إعلان عام -> أيقونة ميكروفون أزرق
        return { icon: 'fa-solid fa-bullhorn', colorClass: 'icon-info' };
    }
    else if (typeStr === '3' || typeStr === 'lowstock') {
        // مخزون قليل -> أيقونة صندوق مفتوح أصفر/برتقالي
        return { icon: 'fa-solid fa-box-open', colorClass: 'icon-warning' };
    }

    // الأيقونة الافتراضية لو جيه نوع غريب
    return { icon: 'fa-solid fa-bell', colorClass: 'icon-info' };
}


// دالة بتسأل السيرفر كل فترة في إشعارات جديدة ولا لأ (بعد التحديث)
async function pollUnreadCount() {
    try {
        
        const response = await authFetch(`/api/Notification/UnRead-Count`);

        if (response.ok) {
            const data = await response.json();

            // هياخد الـ Value سواء الباك إند بعتها سمول أو كابيتال
            const count = data.unreadCount ?? data.UnreadCount ?? 0;
            updateBadge(count);
        }
    } catch (error) {
        console.error("Error polling unread count:", error);
    }
}