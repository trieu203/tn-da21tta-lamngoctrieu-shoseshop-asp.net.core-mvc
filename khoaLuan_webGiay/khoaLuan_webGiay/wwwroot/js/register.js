// register.js - Xử lý luồng đăng ký 3 bước: Email -> OTP -> Thông tin cá nhân

// Chờ khi DOM đã sẵn sàng
document.addEventListener("DOMContentLoaded", function () {
    const form = document.getElementById("registerForm");
    const emailInput = document.getElementById("emailInput");
    const otpInput = document.getElementById("otpInput");
    const emailHidden = document.getElementById("emailHidden");
    const btnSendOtp = document.getElementById("btnSendOtp");
    const msgEmail = document.getElementById("message-email");
    const msgOtp = document.getElementById("message-otp");
    const msgRegister = document.getElementById("message-register");

    const stepEmail = document.getElementById("step-email");
    const stepOtp = document.getElementById("step-otp");
    const stepInfo = document.getElementById("step-info");

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/;

    function isValidEmail(email) {
        return emailRegex.test(email);
    }

    // Bước 1: Gửi OTP
    btnSendOtp?.addEventListener("click", function () {
        const email = emailInput.value.trim();
        msgEmail.innerHTML = "";

        if (!email) {
            msgEmail.innerHTML = `<div class="text-danger">Vui lòng nhập email.</div>`;
            return;
        }

        if (!isValidEmail(email)) {
            msgEmail.innerHTML = `<div class="text-danger">Email không hợp lệ.</div>`;
            return;
        }

        fetch("/Users/SendOtp", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ email })
        })
            .then(res => {
                if (res.ok) {
                    stepEmail.classList.add("d-none");
                    stepOtp.classList.remove("d-none");
                    msgOtp.innerHTML = `<div class='text-success'>OTP đã gửi. Vui lòng kiểm tra email.</div>`;
                } else {
                    return res.text().then(text => {
                        msgEmail.innerHTML = `<div class='text-danger'>${text}</div>`;
                    });
                }
            })
            .catch(err => {
                msgEmail.innerHTML = `<div class='text-danger'>Lỗi gửi OTP.</div>`;
                console.error(err);
            });
    });

    // Bước 2: Xác nhận OTP
    window.verifyOtp = function () {
        const email = emailInput.value.trim();
        const otp = otpInput.value.trim();
        msgOtp.innerHTML = "";

        if (!otp || otp.length !== 6 || !/^\d{6}$/.test(otp)) {
            msgOtp.innerHTML = `<div class="text-danger">Mã OTP phải gồm 6 chữ số.</div>`;
            return;
        }

        fetch("/Users/VerifyOtp", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ email, otp })
        })
            .then(res => {
                if (res.ok) {
                    stepOtp.classList.add("d-none");
                    stepInfo.classList.remove("d-none");
                    emailHidden.value = email;
                } else {
                    return res.text().then(text => {
                        msgOtp.innerHTML = `<div class="text-danger">${text}</div>`;
                    });
                }
            })
            .catch(err => {
                msgOtp.innerHTML = `<div class="text-danger">Lỗi xác thực OTP.</div>`;
                console.error(err);
            });
    };

    // Bước 3: Gửi form đăng ký
    form?.addEventListener("submit", function (e) {
        e.preventDefault();
        msgRegister.innerHTML = "";

        const username = form.querySelector('[name="UserName"]').value.trim();
        const password = form.querySelector('[name="Password"]').value.trim();
        const email = emailHidden.value.trim();

        if (!username || !password || !email) {
            msgRegister.innerHTML = `<div class='text-danger'>Vui lòng điền đầy đủ thông tin bắt buộc.</div>`;
            return;
        }

        const formData = new FormData(form);

        fetch("/Users/Register", {
            method: "POST",
            body: formData
        })
            .then(res => {
                if (res.ok) {
                    alert("Đăng ký thành công!");
                    window.location.href = "/Users/Login";
                } else {
                    return res.text().then(text => {
                        msgRegister.innerHTML = `<div class='text-danger'>Đăng ký thất bại: ${text}</div>`;
                    });
                }
            })
            .catch(err => {
                msgRegister.innerHTML = `<div class='text-danger'>Có lỗi xảy ra khi đăng ký.</div>`;
                console.error("Lỗi đăng ký:", err);
            });
    });
});