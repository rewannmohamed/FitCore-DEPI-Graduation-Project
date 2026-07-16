const userRole = getCurrentUser();
const token = getToken();

document.addEventListener("DOMContentLoaded", () => {
    dynamicLoadLayout(userRole.roles);
    const form = document.getElementById("editProfileForm");
    const saveBtn = document.getElementById("saveBtn");
    const goBackBtn = document.getElementById("goBackBtn");
    const errorContainer = document.getElementById("errorContainer");
    const trainerSection = document.getElementById("trainerSection");

    
    let isUserTrainer = false;

    if (userRole.roles[0] === "Trainer") {
        isUserTrainer = true
    }

    async function loadCurrentProfileData() {
        try {
            const response = await fetch("/api/Profile",
                {
                    method: "GET",
                    headers: {
                        "Accept": "application/json",
                        "Authorization": `Bearer ${token}`
                    }
                }
            );

            if (response.ok) {
                const data = await response.json();

                // ملء البيانات الأساسية
                document.getElementById("fullName").value = data.fullName || data.FullName || '';
                document.getElementById("email").value = data.email || data.Email || '';
                document.getElementById("phoneNumber").value = data.phoneNumber || data.PhoneNumber || '';

                // هنا السيستم بيتحقق: لو الباك إند باعت TrainerDto، يبقى ده مدرب
                const trainerData = data.trainerDto || data.TrainerDto;

                if (trainerData != null) {
                    isUserTrainer = true; // نحدث المتغير
                    trainerSection.classList.remove("hidden"); // نظهر خانات المدرب

                    document.getElementById("specialization").value = trainerData.specialization || trainerData.Specialization || '';
                    document.getElementById("workingHours").value = trainerData.workingHours || trainerData.WorkingHours || '';
                    document.getElementById("bio").value = trainerData.bio || trainerData.Bio || '';
                }
            } else {
                console.error("Failed to load profile data.");
            }
        } catch (error) {
            console.error("Error loading profile:", error);
        }
    }

    loadCurrentProfileData();

    goBackBtn.addEventListener("click", () => {
        window.history.back();
    });

    form.addEventListener("submit", async (e) => {
        e.preventDefault();

        errorContainer.classList.add("hidden");
        errorContainer.innerHTML = '';

        const payload = {
            fullName: document.getElementById("fullName").value.trim(),
            email: document.getElementById("email").value.trim(),
            phoneNumber: document.getElementById("phoneNumber").value.trim(),
            trainerDto: null
        };

        // لو المتغير ده بـ true (يعني هو أصلاً مدرب)، نبعت بياناته الجديدة
        if (isUserTrainer) {
            payload.trainerDto = {
                specialization: document.getElementById("specialization").value.trim(),
                workingHours: document.getElementById("workingHours").value.trim(),
                bio: document.getElementById("bio").value.trim()
            };
        }

        const originalBtnText = saveBtn.innerHTML;
        saveBtn.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Saving...';
        saveBtn.disabled = true;

        try {
            const response = await fetch("/api/Profile", {
                method: "PUT",
                headers: {
                    "Accept": "application/json",
                    "Content-Type": "application/json",   
                    "Authorization": `Bearer ${token}`
                },
                body: JSON.stringify(payload)
            });
            console.log(response.message);
            if (response.ok) {
                alert("Profile updated successfully!");
                window.location.href = "/HTML/Profile/profile.html";
            } else {
                const errorData = await response.json();

                if (errorData.errors && Array.isArray(errorData.errors)) {
                    showErrors(errorData.errors);
                }
                else if (errorData.errors && typeof errorData.errors === 'object') {
                    const errorMessages = [];
                    for (const key in errorData.errors) {
                        errorMessages.push(...errorData.errors[key]);
                    }
                    showErrors(errorMessages);
                }
                else {
                    showErrors([errorData.message || "Failed to update profile."]);
                }
            }
        } catch (error) {
            console.error("Error saving profile:", error);
            showErrors(["A network error occurred. Please check your connection."]);
        } finally {
            saveBtn.innerHTML = originalBtnText;
            saveBtn.disabled = false;
        }
    });

    function showErrors(errorsArray) {
        errorContainer.innerHTML = `<strong>Please fix the following errors:</strong>`;
        const ul = document.createElement('ul');
        errorsArray.forEach(err => {
            const li = document.createElement('li');
            li.textContent = err;
            ul.appendChild(li);
        });
        errorContainer.appendChild(ul);
        errorContainer.classList.remove("hidden");
        errorContainer.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
});

function dynamicLoadLayout(userRoles) {
    if (!userRole.roles || userRole.roles.length === 0) return;


    const primaryRole = userRole.roles[0];
    let scriptSrc = "";


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