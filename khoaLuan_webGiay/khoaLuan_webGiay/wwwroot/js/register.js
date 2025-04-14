document.addEventListener("DOMContentLoaded", function () {
    const form = document.getElementById("registerForm");
    const emailInput = document.getElementById("emailInput");
    const otpInput = document.getElementById("otpInput");
    const emailHidden = document.getElementById("emailHidden");
    const messageBox = document.getElementById("message");

    // Gửi OTP
    window.sendOtp = function () {
        const email = emailInput.value.trim();
        if (!email || !email.includes("@")) {
            messageBox.innerText = "Vui lòng nhập email hợp lệ.";
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
                    messageBox.innerText = "OTP đã gửi, kiểm tra email.";
                } else {
                    return res.text().then(text => {
                        messageBox.innerText = text;
                    });
                }
            });
    };

    // Xác thực OTP
    window.verifyOtp = function () {
        const email = emailInput.value.trim();
        const otp = otpInput.value.trim();

        if (!otp || otp.length !== 6 || !/^\d+$/.test(otp)) {
            messageBox.innerText = "Mã OTP phải gồm 6 chữ số.";
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
                    messageBox.innerText = "";
                } else {
                    return res.text().then(text => {
                        messageBox.innerText = text;
                    });
                }
            });
    };

    // Đăng ký tài khoản
    form.addEventListener("submit", function (e) {
        e.preventDefault();

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
                    messageBox.innerText = "Đăng ký thất bại";
                }
            });
    });
});

document.getElementById("message").innerHTML = `
    <div class="text-success">OTP đã gửi, kiểm tra email.</div>
    <div class="alert alert-warning mt-2">
        <strong>Lưu ý:</strong> Nếu không thấy email, kiểm tra hộp thư <b>Spam</b> và đánh dấu là <b>Không phải spam</b>.
    </div>
`;

