document.addEventListener("DOMContentLoaded", function () {
    const form = document.getElementById("registerForm");
    const emailInput = document.getElementById("emailInput");
    const otpInput = document.getElementById("otpInput");
    const emailHidden = document.getElementById("emailHidden");

    const msgEmail = document.getElementById("message-email");
    const msgOtp = document.getElementById("message-otp");
    const msgRegister = document.getElementById("message-register");

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/;

    function isValidEmail(email) {
        return emailRegex.test(email);
    }

    // Gửi OTP
    window.sendOtp = function () {
        const email = emailInput.value.trim();
        msgEmail.innerHTML = "";

        console.log("Email nhập:", email);
        console.log("Hợp lệ?", isValidEmail(email));

        if (!email) {
            msgEmail.innerHTML = `<div class="text-danger">Vui lòng nhập email.</div>`;
            return;
        }

        if (!isValidEmail(email)) {
            msgEmail.innerHTML = `<div class="text-danger">Email không hợp lệ. Vui lòng nhập đúng định dạng (ví dụ: ten@example.com).</div>`;
            return;
        }

        fetch("/Users/SendOtp", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ email: email })
        })
            .then(res => {
                if (res.ok) {
                    document.getElementById("step-email").classList.add("d-none");
                    document.getElementById("step-otp").classList.remove("d-none");
                    msgOtp.innerHTML = `
                        <div class="text-success">OTP đã gửi, kiểm tra email.</div>
                        <div class="alert alert-warning mt-2">
                            <strong>Lưu ý:</strong> Nếu không thấy email, kiểm tra hộp thư <b>Spam</b> và đánh dấu là <b>Không phải spam</b>.
                        </div>`;
                } else {
                    return res.text().then(text => {
                        msgEmail.innerHTML = `<div class="text-danger">${text}</div>`;
                    });
                }
            })
            .catch(err => {
                msgEmail.innerHTML = `<div class="text-danger">Đã xảy ra lỗi khi gửi OTP.</div>`;
                console.error("Lỗi gửi OTP:", err);
            });
    };

    // Xác nhận OTP
    window.verifyOtp = function () {
        const email = emailInput.value.trim();
        const otp = otpInput.value.trim();
        msgOtp.innerHTML = "";

        if (!otp || otp.length !== 6 || !/^\d+$/.test(otp)) {
            msgOtp.innerHTML = `<div class="text-danger">Mã OTP phải gồm 6 chữ số.</div>`;
            return;
        }

        fetch("/Users/VerifyOtp", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ email: email, otp: otp })
        })
            .then(res => {
                if (res.ok) {
                    document.getElementById("step-otp").classList.add("d-none");
                    document.getElementById("step-info").classList.remove("d-none");
                    emailHidden.value = email;
                } else {
                    return res.text().then(text => {
                        msgOtp.innerHTML = `<div class="text-danger">${text}</div>`;
                    });
                }
            })
            .catch(err => {
                msgOtp.innerHTML = `<div class="text-danger">Đã xảy ra lỗi xác thực OTP.</div>`;
                console.error("Lỗi xác thực OTP:", err);
            });
    };

    // Đăng ký tài khoản
    if (form) {
        form.addEventListener("submit", function (e) {
            e.preventDefault();
            msgRegister.innerHTML = "";

            const formData = new FormData(form);

            fetch("/Users/Register", {
                method: "POST",
                body: formData
            })
                .then(res => {
                    if (res.ok) {
                        alert("Đăng ký thành công!");
                        window.location.href = "/";
                    } else {
                        msgRegister.innerHTML = `<div class="text-danger">Đăng ký thất bại.</div>`;
                    }
                })
                .catch(err => {
                    msgRegister.innerHTML = `<div class="text-danger">Có lỗi xảy ra khi đăng ký.</div>`;
                    console.error("Lỗi đăng ký:", err);
                });
        });
    }
});
