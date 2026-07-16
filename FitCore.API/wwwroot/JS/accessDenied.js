
const user = getCurrentUser();
const Token = getToken();

document.addEventListener('DOMContentLoaded', () => {
  
    document.getElementById('DashboardBtn').addEventListener('click', redict);
});

const redict= () => {
    
    if (!Token) {
        window.location.href = "/html/Auth/login.html";
    }

    roles = user?.roles || [];

    function redirectForRoles(roles) {
        roles = roles || [];
        if (roles.includes("Admin")) {
            window.location.href = "/html/admin/dashboard/dashboard.html";
        } else if (roles.includes("Receptionist")) {
            window.location.href = "/html/Receptionist/Dashboard/receptionist-dashboard.html";
        } else if (roles.includes("Trainer")) {
            window.location.href = "/html/Trainer/Dashboard/trainer-dashboard.html";
        } else {
            window.location.href = "/html/user/MemberDashboard/member-dashboard.html";
        }
    }
}