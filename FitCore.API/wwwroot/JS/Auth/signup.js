document.addEventListener('DOMContentLoaded', () => {
    const signupForm = document.getElementById('signupForm');
    const errorMessage = document.getElementById('errorMessage');
    const submitBtn = document.getElementById('submitBtn');
    const btnText = document.getElementById('btnText');
    const btnSpinner = document.getElementById('btnSpinner');

    const API_BASE_URL = 'http://localhost:5184/api/Auth';

    signupForm.addEventListener('submit', async (e) => {
        e.preventDefault();


        const fullName = document.getElementById('fullName').value.trim();
        const email = document.getElementById('email').value.trim();
        const phoneNumber = document.getElementById('phoneNumber').value.trim();
        const password = document.getElementById('password').value;


        errorMessage.classList.add('hidden');
        errorMessage.textContent = '';
        setLoadingState(true);

        try {

            const response = await fetch(`${API_BASE_URL}/register-member`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    fullName: fullName,
                    email: email,
                    phoneNumber: phoneNumber,
                    password: password
                })
            });

            const data = await response.json();


            if (response.ok) {

                localStorage.setItem('token', data.token);
                localStorage.setItem('userId', data.userID);
                localStorage.setItem('userName', data.fullName);
                localStorage.setItem('userRoles', JSON.stringify(data.roles));


                window.location.href = '../user/MemberDashboard/member-dashboard.html';
            } else {

                if (data.errors && Array.isArray(data.errors)) {
                    showError(data.errors.join('<br>'));
                } else if (data.message) {
                    showError(data.message);
                } else {
                    showError('Registration failed. Please try again.');
                }
            }
        } catch (error) {
            console.error('Error during registration:', error);
            showError('A network error occurred. Please check your connection and try again.');
        } finally {

            setLoadingState(false);
        }
    });


    function showError(message) {
        errorMessage.innerHTML = message;
        errorMessage.classList.remove('hidden');
    }

    function setLoadingState(isLoading) {
        if (isLoading) {
            btnText.classList.add('hidden');
            btnSpinner.classList.remove('hidden');
            submitBtn.disabled = true;
            submitBtn.style.opacity = '0.7';
        } else {
            btnText.classList.remove('hidden');
            btnSpinner.classList.add('hidden');
            submitBtn.disabled = false;
            submitBtn.style.opacity = '1';
        }
    }
});