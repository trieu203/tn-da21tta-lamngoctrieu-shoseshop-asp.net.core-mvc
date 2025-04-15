document.addEventListener("DOMContentLoaded", function () {
    let savedEmail = "";

    const emailInput = document.getElementById("emailInput");
    const otpInput = document.getElementById("otpInput");
    const newPasswordInput = document.getElementById("newPasswordInput");

    const emailError = document.getElementById("email-error");
    const otpError = document.getElementById("otp-error");
    const resetError = document.getElementById("reset-error");

    const stepEmail = document.getElementById("step-email");
    const stepOtp = document.getElementById("step-otp");
    const stepReset = document.getElementById("step-reset");

    window.sendOtp = function () {
        const email = emailInput.value.trim();
        emailError.innerText = "";

        if (!email || !email.includes("@")) {
            emailError.innerText = "Vui lòng nhập email hợp lệ.";
            return;
        }

        fetch("/Users/SendOtpForReset", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ email: email })
        })
            .then(res => {
                if (res.ok) {
                    savedEmail = email;
                    stepEmail.classList.add("d-none");
                    stepOtp.classList.remove("d-none");
                } else {
                    return res.text().then(text => emailError.innerText = text);
                }
            })
            .catch(() => {
                emailError.innerText = "Lỗi kết nối máy chủ.";
            });
    };

    window.verifyOtp = function () {
        const otp = otpInput.value.trim();
        otpError.innerText = "";

        if (!otp || otp.length !== 6 || !/^\d+$/.test(otp)) {
            otpError.innerText = "Mã OTP không hợp lệ.";
            return;
        }

        fetch("/Users/VerifyOtp", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ email: savedEmail, otp: otp })
        })
            .then(res => {
                if (res.ok) {
                    stepOtp.classList.add("d-none");
                    stepReset.classList.remove("d-none");
                } else {
                    return res.text().then(text => otpError.innerText = text);
                }
            })
            .catch(() => {
                otpError.innerText = "Lỗi xác thực OTP.";
            });
    };

    window.resetPassword = function () {
        const newPassword = newPasswordInput.value.trim();
        const confirmPassword = document.getElementById("confirmPasswordInput").value.trim();
        resetError.innerText = "";

        if (!newPassword || newPassword.length < 6) {
            resetError.innerText = "Mật khẩu phải có ít nhất 6 ký tự.";
            return;
        }

        if (newPassword !== confirmPassword) {
            resetError.innerText = "Mật khẩu xác nhận không khớp.";
            return;
        }

        fetch("/Users/ResetPassword", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                email: savedEmail,
                otp: otpInput.value.trim(),
                newPassword: newPassword
            })
        })
            .then(res => {
                if (res.ok) {
                    alert("Đặt lại mật khẩu thành công!");
                    window.location.href = "/Users/Login";
                } else {
                    return res.text().then(text => resetError.innerText = text);
                }
            })
            .catch(() => {
                resetError.innerText = "Lỗi khi cập nhật mật khẩu.";
            });
    };
});
