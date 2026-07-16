
const FALLBACK_PLANS = [
    {
        name: 'Essential', price: 149, highlight: false,
        features: ['Up to 500 members', 'Automated billing', 'Basic app access', 'Email support'],
        cta: 'Start Trial', ctaHref: '/html/Auth/register.html?plan=essential',
    },
    {
        name: 'Pro', price: 299, highlight: true, ribbon: 'Most Popular',
        features: ['Unlimited members', 'Custom branded app', 'Advanced marketing tools', 'Inventory management', 'Priority support'],
        cta: 'Get Started', ctaHref: '/html/Auth/register.html?plan=pro',
    },
    {
        name: 'Enterprise', price: 599, highlight: false,
        features: ['Multi-location support', 'API access', 'Custom integrations', 'Dedicated success manager'],
        cta: 'Contact Sales', ctaHref: '/html/Landing/contact.html',
    },
];

document.addEventListener('DOMContentLoaded', () => {
    CheckToken();
    renderPlans(FALLBACK_PLANS);
});

const CheckToken = () => {
    const token = getToken();
    const tokenHead = document.getElementById("tokenHead");
    if (!token) {
        tokenHead.innerHTML = `
           <a href="/html/Auth/login.html" class="btn btn-primary" role="button">Login</a>
            <a href="/html/Auth/signup.html" class="btn btn-outline-primary" role="button">Sign up</a>
        `;
    }
    else { 
        tokenHead.innerHTML = `
            <a href="/html/Profile/profile.html" class="profile-chip bg-info-subtle py-2 px-3 rounded-circle" id="profileChip">
                <div class="avatar" id="headerAvatar"><i class="fa-solid fa-user"></i></div>
            </a>
        `;
    }
}

function renderPlans(plans) {
    const grid = document.getElementById('plansGrid');
    if (!grid) return;

    grid.innerHTML = plans.map(plan => `
     <div class="p-3 rounded-5 col-md-4 col-12">
         <div class="card ${plan.highlight ? 'bg-blue bg-gradient text-white' : 'bg-body-secondary'} border-0 shadow position-relative p-4">
                ${plan.ribbon ? `<span class="badge w-25 position-absolute p-1 badge-position">${escapeHtml(plan.ribbon)}</span>` : ''}
                <div class="fs-6 fw-bold">${escapeHtml(plan.name)}</div>
                <div class="fs-3 fw-bold">$${plan.price}<span class="fs-6 ${plan.highlight ? 'text-white-light' : 'text-body-secondary'}  fw-normal">/month</span></div>
                <ul class="list-group py-3">
                    ${plan.features.map(f => `
                    <li class="list-group-item bg-transparent border-0 ${plan.highlight ? 'text-white-light' : 'text-body-secondary'}  ">
                        <i class='${plan.highlight ? 'text-white-light' : 'text-blue'} bx bx-check-circle '></i> ${escapeHtml(f)}
                    </li>
                    `
                    ).join('')}
                </ul>
                <a href="${plan.ctaHref}" class="  ${plan.highlight ? 'btn btn-light' : 'btn btn-outline-primary '}  " role="button">${escapeHtml(plan.cta)}</a>
            </div>
        </div>
    `).join('');
}

function escapeHtml(str) {
    const div = document.createElement('div');
    div.textContent = str ?? '';
    return div.innerHTML;
}
