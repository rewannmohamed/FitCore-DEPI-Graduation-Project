// auditlogs.js

let currentPage = 1;
let currentSearch = '';
let currentSortBy = 'Date';
let isDescending = true;

// أول ما الصفحة تحمل
document.addEventListener('DOMContentLoaded', () => {
    requireRole(["Admin"]);
    fetchAuditLogs();
    document.getElementById('applyBtn').addEventListener('click', applyFilters);
    document.getElementById('sortDirBtn').addEventListener('click', toggleSortDirection);
});

async function fetchAuditLogs() {
    showLoadingState();

    try {

        const url = `/api/AuditLogs?page=${currentPage}&pageSize=10&searchTerm=${encodeURIComponent(currentSearch)}&sortBy=${encodeURIComponent(currentSortBy)}&isDescending=${isDescending}`;

        const data = await FitCoreApi.get(url);


        const logsArray = data.data || data.Data || [];
        const totalCount = data.totalCount ?? data.TotalCount ?? 0;
        const page = data.currentPage ?? data.CurrentPage ?? currentPage;
        const pageSize = data.pageSize ?? data.PageSize ?? 10;

        if (logsArray.length === 0) {
            showEmptyState();
        } else {
            renderTable(logsArray);
        }

        renderPagination(totalCount, page, pageSize);
    } catch (error) {
        console.error("Failed to fetch logs", error);
        showErrorState();
        // لو حصل error فعلي، نفضّي الباجيناشن عشان ميفضلش شكل قديم غلط
        document.getElementById('paginationControls').innerHTML = '';
    }
}

function showLoadingState() {
    const tbody = document.getElementById('tableBody');
    tbody.innerHTML = `
        <tr class="state-row">
            <td colspan="5">
                <span class="spinner"></span>
                <span class="state-title">Loading logs…</span>
            </td>
        </tr>`;
}

function showEmptyState() {
    const tbody = document.getElementById('tableBody');
    tbody.innerHTML = `
        <tr class="state-row">
            <td colspan="5">
                <span class="state-icon">🗂️</span>
                <span class="state-title">No logs found</span>
                <span class="state-subtitle">Try adjusting your search or filters.</span>
            </td>
        </tr>`;
}

function showErrorState() {
    const tbody = document.getElementById('tableBody');
    tbody.innerHTML = `
        <tr class="state-row is-error">
            <td colspan="5">
                <span class="state-icon">⚠️</span>
                <span class="state-title">Couldn't load logs</span>
                <span class="state-subtitle">Something went wrong. Please try again.</span>
            </td>
        </tr>`;
}

function renderTable(logs) {
    const tbody = document.getElementById('tableBody');
    tbody.innerHTML = '';

    logs.forEach(log => {
        // 1. تظبيط الحروف (عشان الـ .NET بيبعتها camelCase)
        const entityName = log.entityName || log.EntityName || 'Unknown';
        const action = log.action || log.Action || 'Unknown';
        const userName = log.userName || log.UserName || 'System';
        const primaryKey = log.entityPrimaryKey || log.EntityPrimaryKey || '#';
        const createdAt = log.createdAt || log.CreatedAt;

        // 2. تحديد لون النقطة (Module)
        let dotClass = 'system';
        const entityLower = entityName.toLowerCase();
        if (entityLower.includes('invoice') || entityLower.includes('payment') || entityLower.includes('cart')) dotClass = 'billing';
        else if (entityLower.includes('user') || entityLower.includes('member')) dotClass = 'members';
        else if (entityLower.includes('product') || entityLower.includes('inventory')) dotClass = 'inventory';

        // 3. تنسيق التاريخ زي التصميم بالظبط
        const dateObj = new Date(createdAt);
        const hasValidDate = !isNaN(dateObj.getTime());
        const dateStr = hasValidDate ? dateObj.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' }) : '—';
        const timeStr = hasValidDate ? dateObj.toLocaleTimeString('en-US', { hour12: false }) : '';
        const formattedDate = `${dateStr}${timeStr ? ` <br> <small>${timeStr}</small>` : ''}`;

        // 4. تجهيز الداتا لزرار الـ Details (عشان ميضربش إيرور لو كان null)
        const oldVal = encodeURIComponent(log.oldValue || log.OldValue || '');
        const newVal = encodeURIComponent(log.newValue || log.NewValue || '');

        const row = document.createElement('tr');
        row.innerHTML = `
                <td>${formattedDate}</td>
                <td class="user-cell">
                  
                   ${escapeHtml(userName)}
                </td>
                <td>
                    <div class="action-cell">${escapeHtml(action)} <span class="id-tag">ID: #${escapeHtml(String(primaryKey))}</span></div>
                </td>
                <td class="module-cell">
                    <div class="dot ${dotClass}"></div>
                    ${escapeHtml(entityName)}
                </td>
                <td>
                    <button class="btn-outline" data-old="${oldVal}" data-new="${newVal}">ℹ</button>
                </td>
        `;
        row.querySelector('button').addEventListener('click', (e) => {
            showDetails(e.currentTarget.dataset.old, e.currentTarget.dataset.new);
        });
        tbody.appendChild(row);
    });
}

// حماية بسيطة من الـ HTML injection في النصوص القادمة من السيرفر
function escapeHtml(str) {
    const div = document.createElement('div');
    div.textContent = str ?? '';
    return div.innerHTML;
}

// دالة بسيطة عشان لما تدوسي على زرار الـ (ℹ) تطبع التغييرات في الـ Console مؤقتاً
// أو تقدري مستقبلاً تخليها تفتح Popup (Modal)
function showDetails(encodedOld, encodedNew) {
    const oldVal = decodeURIComponent(encodedOld);
    const newVal = decodeURIComponent(encodedNew);

    console.log("Old Value:", oldVal ? JSON.parse(oldVal) : "None");
    console.log("New Value:", newVal ? JSON.parse(newVal) : "None");

    alert("Check the browser console to see the exact data changes!");
}

function renderPagination(totalCount, currPage, pageSize) {
    const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
    const paginationContainer = document.getElementById('paginationControls');
    paginationContainer.innerHTML = '';

    // لو صفحة واحدة بس ومفيش نتائج أصلاً، مفيش داعي نعرض باجيناشن
    if (totalCount === 0) return;

    for (let i = 1; i <= totalPages; i++) {
        const btn = document.createElement('button');
        btn.innerText = i;
        if (i === currPage) btn.classList.add('active');

        btn.addEventListener('click', () => {
            currentPage = i;
            fetchAuditLogs();
        });

        paginationContainer.appendChild(btn);
    }
}

function applyFilters() {
    currentSearch = document.getElementById('searchInput').value;
    currentSortBy = document.getElementById('sortBySelect').value;
    currentPage = 1; // رجع لأول صفحة لما تفلتر
    fetchAuditLogs();
}

function toggleSortDirection() {
    isDescending = !isDescending;
    const btn = document.getElementById('sortDirBtn');
    btn.innerText = isDescending ? '⬇ Desc' : '⬆ Asc';
    fetchAuditLogs();
}