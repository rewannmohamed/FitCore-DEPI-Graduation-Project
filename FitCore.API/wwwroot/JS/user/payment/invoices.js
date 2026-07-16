document.addEventListener('DOMContentLoaded', () => {
    const token = getToken();
    if (!token) {
        window.location.href = '/html/Auth/login.html';
        return;
    }

    const API_BASE = 'http://localhost:5184/api';
    const tbody = document.getElementById('invoicesTableBody');
    const prevBtn = document.getElementById('prevPageBtn');
    const nextBtn = document.getElementById('nextPageBtn');
    const pageIndicator = document.getElementById('pageIndicator');

    let currentPage = 1;
    const pageSize = 10;

    async function loadInvoices() {
        try {
            const response = await fetch(`${API_BASE}/Invoices?page=${currentPage}&pageSize=${pageSize}`, {
                headers: { 'Authorization': `Bearer ${token}` }
            });

            if (!response.ok) throw new Error("Failed to fetch invoices");

            const data = await response.json();
            // Assuming API returns { data: [...], totalPages: X } or similar. Adjust if it returns a direct array.
            const invoices = Array.isArray(data) ? data : (data.data || data.Data || []);

            renderTable(invoices);

            // Basic pagination logic (Adjust based on your actual API pagination response)
            pageIndicator.textContent = `Page ${currentPage}`;
            prevBtn.disabled = currentPage === 1;
            nextBtn.disabled = invoices.length < pageSize;

        } catch (error) {
            console.error(error);
            tbody.innerHTML = `<tr><td colspan="6" class="text-center" style="color: var(--status-red);">Error loading invoices.</td></tr>`;
        }
    }

    function renderTable(invoices) {
        tbody.innerHTML = '';

        if (invoices.length === 0) {
            tbody.innerHTML = `<tr><td colspan="6" style="text-align: center; color: var(--text-muted);">No invoices found.</td></tr>`;
            return;
        }

        invoices.forEach(inv => {
            // Mapping status: 0=Pending, 1=Completed, 2=Cancelled (Change based on your exact Enums)
            let statusText = "Unknown";
            let statusClass = "status-pending";

            const statusCode = inv.invoiceStatus || inv.InvoiceStatus;
            if (statusCode === 1 || statusCode === "Pending") { statusText = "Pending"; statusClass = "status-pending"; }
            if (statusCode === 2 || statusCode === "Completed") { statusText = "Completed"; statusClass = "status-completed"; }
            //if (statusCode === 2 || statusCode === "Cancelled") { statusText = "Cancelled"; statusClass = "status-cancelled"; }

            const date = new Date(inv.issueDate || inv.IssueDate).toLocaleDateString();
            const total = (inv.totalAmount || inv.TotalAmount || 0).toFixed(2);
            const desc = inv.description || inv.Description || "—";

            const tr = document.createElement('tr');
            tr.innerHTML = `
                <td><strong>#INV-${inv.invoiceId || inv.InvoiceId}</strong></td>
                <td>${date}</td>
                <td>${desc}</td>
                <td><strong>$${total}</strong></td>
                <td><span class="status-pill ${statusClass}">${statusText}</span></td>
                <td>
                    <button class="btn-outline view-btn" data-id="${inv.invoiceId || inv.InvoiceId}" style="padding: 4px 10px; font-size: var(--fs-xs);">
                        <i class="fa-solid fa-eye"></i> View
                    </button>
                </td>
            `;
            tbody.appendChild(tr);
        });

        document.querySelectorAll('.view-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                const id = e.currentTarget.dataset.id;
                window.location.href = `/html/user/payment/invoice-details.html?id=${id}`;
            });
        });
    }

    prevBtn.addEventListener('click', () => { if (currentPage > 1) { currentPage--; loadInvoices(); } });
    nextBtn.addEventListener('click', () => { currentPage++; loadInvoices(); });

    loadInvoices();
});