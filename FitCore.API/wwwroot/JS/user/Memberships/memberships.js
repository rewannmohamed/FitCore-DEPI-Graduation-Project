const token = getToken();

document.addEventListener("DOMContentLoaded", () => {
    requireRole(["Member"]);
    const tableBody = document.getElementById("membershipsTableBody");
    const freezeModal = document.getElementById("freezeModal");
    const freezeDaysInput = document.getElementById("freezeDaysInput");
    const confirmFreezeBtn = document.getElementById("confirmFreezeBtn");
    const freezeError = document.getElementById("freezeError");

    let currentFreezeMembershipId = null;

    async function loadMemberships() {
        try {
            const response = await fetch('/api/Memberships/my-memberships', {
                method: 'GET',
                headers: {
                    "Accept": "application/json",
                    "Authorization": `Bearer ${token}`
                }
            });

            if (response.ok) {
                const memberships = await response.json();
                renderTable(memberships);
            } else {
                tableBody.innerHTML = `<tr><td colspan="7" class="text-center" style="color: var(--status-red);">Failed to load memberships.</td></tr>`;
            }
        } catch (error) {
            console.error("Error fetching memberships:", error);
            tableBody.innerHTML = `<tr><td colspan="7" class="text-center" style="color: var(--status-red);">Network error occurred.</td></tr>`;
        }
    }

    function renderTable(memberships) {
        tableBody.innerHTML = '';

        if (memberships.length === 0) {
            tableBody.innerHTML = `<tr><td colspan="7" class="text-center loading-text">You have no active memberships.</td></tr>`;
            return;
        }

        memberships.forEach(m => {
            const startDate = new Date(m.startDate).toLocaleDateString();
            const endDate = new Date(m.endDate).toLocaleDateString();

            let statusBadgeClass = 'badge-active';
            let statusText = 'Active';

            if (m.status === 2 || String(m.status).toLowerCase() === 'freezed') {
                statusBadgeClass = 'badge-frozen';
                statusText = 'Frozen';
            } else if (m.status === 3 || String(m.status).toLowerCase() === 'expired') {
                statusBadgeClass = 'badge-expired';
                statusText = 'Expired';
            } else {
                statusBadgeClass = 'badge-active';
                statusText = 'Active';
            }

            const remaining = m.remainingSessions !== null ? m.remainingSessions : 'Unlimited';

            let actionBtnHtml = '';
            if (statusText === 'Active') {
                actionBtnHtml = `<button class="btn-outline btn-freeze btn-small" onclick="openFreezeModal(${m.membershipID})">Freeze</button>`;
            } else if (statusText === 'Frozen') {
                actionBtnHtml = `<button class="btn-outline btn-unfreeze btn-small" onclick="unfreezeMembership(${m.membershipID})">Unfreeze</button>`;
            } else {
                actionBtnHtml = `<button class="btn-outline btn-small" disabled>Freeze</button>`;
            }

            const tr = document.createElement('tr');
            tr.innerHTML = `
                <td><strong>${m.name}</strong></td>
                <td><span class="badge">${m.membershipType}</span></td>
                <td>${startDate}</td>
                <td>${endDate}</td>
                <td><span class="badge ${statusBadgeClass}">${statusText}</span></td>
                <td>${remaining}</td>
                <td class="action-buttons">
                    <a href="/html/user/Memberships/membership-details.html?id=${m.membershipID}" class="btn-outline btn-small">View</a>
                    ${actionBtnHtml}
                </td>
            `;
            tableBody.appendChild(tr);
        });
    }

    window.openFreezeModal = function (membershipId) {
        currentFreezeMembershipId = membershipId;
        freezeDaysInput.value = '';
        freezeError.classList.add('hidden');
        freezeModal.classList.remove('hidden');
    };


    window.unfreezeMembership = async function (membershipId) {
        if (!confirm("Are you sure you want to unfreeze this membership now? The end date will be adjusted accordingly.")) {
            return;
        }

        try {
            const response = await fetch(`/api/Memberships/${membershipId}/unfreeze`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                    // 'Authorization': `Bearer ${localStorage.getItem('token')}`
                }
            });

            if (response.ok) {
                alert("Membership unfrozen successfully!");
                loadMemberships();
            } else {
                const errData = await response.json();
                alert(errData.message || "Failed to unfreeze membership.");
            }
        } catch (error) {
            console.error("Error unfreezing membership:", error);
            alert("A network error occurred.");
        }
    };

    function closeModal() {
        freezeModal.classList.add('hidden');
        currentFreezeMembershipId = null;
    }

    if (freezeModal && confirmFreezeBtn) {

        document.getElementById("closeModalBtn").addEventListener("click", closeModal);
        document.getElementById("closeModalBtnTop").addEventListener("click", closeModal);

        confirmFreezeBtn.addEventListener("click", async () => {
            const days = parseInt(freezeDaysInput.value);

            if (!days || days <= 0) {
                freezeError.textContent = "Please enter a valid number of days.";
                freezeError.classList.remove('hidden');
                return;
            }

            const originalBtnText = confirmFreezeBtn.innerHTML;
            confirmFreezeBtn.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Freezing...';
            confirmFreezeBtn.disabled = true;
            freezeError.classList.add('hidden');

            try {
                const response = await fetch(`/api/Memberships/${currentFreezeMembershipId}/freeze`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                        // 'Authorization': `Bearer ${localStorage.getItem('token')}`
                    },
                    body: JSON.stringify(days)
                });

                if (response.ok) {
                    alert("Membership frozen successfully!");
                    closeModal();
                    loadMemberships(); 
                } else {
                    const errData = await response.json();
                    freezeError.textContent = errData.message || "Failed to freeze membership.";
                    freezeError.classList.remove('hidden');
                }
            } catch (error) {
                console.error("Error freezing membership:", error);
                freezeError.textContent = "A network error occurred.";
                freezeError.classList.remove('hidden');
            } finally {
                confirmFreezeBtn.innerHTML = originalBtnText;
                confirmFreezeBtn.disabled = false;
            }
        });
    }


    if (tableBody) {
        loadMemberships();
    }


    const detailsContainer = document.getElementById("membershipDetailsContainer");

    if (detailsContainer) {
        const urlParams = new URLSearchParams(window.location.search);
        const membershipId = urlParams.get('id');

        const loadingDiv = document.getElementById("detailsLoading");
        const contentDiv = document.getElementById("detailsContent");
        const errorDiv = document.getElementById("detailsError");

        if (membershipId) {
            loadMembershipDetails(membershipId);
        } else {
            showDetailsError("Invalid membership ID.");
        }

        async function loadMembershipDetails(id) {
            try {
                const response = await fetch(`/api/Memberships/${id}`, {
                    method: 'GET',
                    headers: {
                        'Content-Type': 'application/json'
                        // 'Authorization': `Bearer ${localStorage.getItem('token')}`
                    }
                });

                if (response.ok) {
                    const data = await response.json();
                    populateDetails(data);
                } else {
                    showDetailsError("Membership not found or you don't have access.");
                }
            } catch (error) {
                console.error("Error fetching details:", error);
                showDetailsError("Network error occurred.");
            }
        }

        function populateDetails(m) {
            document.getElementById("detailName").textContent = m.name;
            document.getElementById("detailType").textContent = m.membershipType;
            document.getElementById("detailStart").textContent = new Date(m.startDate).toLocaleDateString();
            document.getElementById("detailEnd").textContent = new Date(m.endDate).toLocaleDateString();
            document.getElementById("detailRemaining").textContent = m.remainingSessions !== null ? m.remainingSessions : 'Unlimited';

            const statusSpan = document.getElementById("detailStatus");
            let statusText = 'Active';
            statusSpan.className = 'badge badge-active';

            if (m.status === 2 || String(m.status).toLowerCase() === 'freezed') {
                statusText = 'Frozen';
                statusSpan.className = 'badge badge-frozen';
            } else if (m.status === 3 || String(m.status).toLowerCase() === 'expired') {
                statusText = 'Expired';
                statusSpan.className = 'badge badge-expired';
            }

            statusSpan.textContent = statusText;

            loadingDiv.classList.add("hidden");
            contentDiv.classList.remove("hidden");
        }

        function showDetailsError(message) {
            loadingDiv.classList.add("hidden");
            errorDiv.textContent = message;
            errorDiv.classList.remove("hidden");
        }
    }
});