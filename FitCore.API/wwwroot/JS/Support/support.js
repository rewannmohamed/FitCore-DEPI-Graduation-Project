// support.js
const token = getToken();
const userRole = getCurrentUser();
document.addEventListener('DOMContentLoaded', () => {
    dynamicLoadLayout(userRole.roles);
    const input = document.getElementById('faqSearchInput');
    
    input.addEventListener('input', () => {
        const term = input.value.trim().toLowerCase();
        const items = document.querySelectorAll('.faq-item');
        let visibleCount = 0;
        
        items.forEach(item => {
            const keywords = item.dataset.keywords.toLowerCase();
            const text = item.textContent.toLowerCase();
            const matches = !term || keywords.includes(term) || text.includes(term);
            item.style.display = matches ? '' : 'none';
            if (matches) visibleCount++;
        });

        document.getElementById('noResultsMsg').classList.toggle('d-none', visibleCount > 0);
    });
});
function dynamicLoadLayout(userRoles) {
    if (!userRoles || userRoles.length === 0) return;

    const primaryRole = userRoles[0];
    let scriptSrc = "";
    console.log(primaryRole);

    switch (primaryRole) {
        case "Admin":
        case 0:
            scriptSrc = "/JS/admin/Components/layout.js";
            break;
        case "Trainer":
        case 1:
            scriptSrc = "/JS/Trainer/Components/layout.js";
            break;
        case "Receptionist":
        case 3:
            scriptSrc = "/JS/Receptionist/Components/layout.js";
            break;
        default:
            scriptSrc = "/JS/user/Components/layout.js";
            break;
    }


    const script = document.createElement("script");
    script.src = scriptSrc;
    script.defer = true;

    document.body.appendChild(script);

}