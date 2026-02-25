/**
 * File: wwwroot/js/account-restricted.js
 * Dùng cho trang báo lỗi khóa tài khoản
 */
document.addEventListener("DOMContentLoaded", function () {
    // 1. Xử lý nút X đóng trang
    const closeBtn = document.getElementById("closeRestrictedPage");
    if (closeBtn) {
        closeBtn.addEventListener("click", function (e) {
            e.preventDefault();
            window.location.href = "/";
        });
    }

    // 2. Thuật toán Đồng hồ đếm ngược
    const countdownContainer = document.getElementById("countdownContainer");
    if (countdownContainer) {
        // Lấy thời gian chuẩn ISO từ thuộc tính data-
        const targetIso = countdownContainer.getAttribute("data-until");
        if (targetIso) {
            const targetDate = new Date(targetIso).getTime();

            // Cập nhật mỗi 1 giây (1000ms)
            const timerInterval = setInterval(function () {
                const now = new Date().getTime();
                const distance = targetDate - now;

                // NẾU ĐÃ HẾT GIỜ PHẠT
                if (distance <= 0) {
                    clearInterval(timerInterval);
                    document.getElementById("countdownTimer").classList.add("d-none");
                    document.getElementById("countdownFinished").classList.remove("d-none");

                    // Thêm nút Đăng nhập nhấp nháy gọi mời người dùng quay lại
                    const supportBtn = document.querySelector(".btn-support");
                    if (supportBtn) {
                        supportBtn.innerHTML = '<i class="bi bi-box-arrow-in-right me-2"></i> ĐĂNG NHẬP NGAY';
                        supportBtn.href = "/Account/Login";
                        supportBtn.classList.replace("background-color", "#198754"); // Đổi màu xanh
                    }
                    return;
                }

                // TÍNH TOÁN NGÀY, GIỜ, PHÚT, GIÂY
                const days = Math.floor(distance / (1000 * 60 * 60 * 24));
                const hours = Math.floor((distance % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
                const minutes = Math.floor((distance % (1000 * 60 * 60)) / (1000 * 60));
                const seconds = Math.floor((distance % (1000 * 60)) / 1000);

                // IN RA GIAO DIỆN
                document.getElementById("cd-days").innerText = days.toString().padStart(2, '0');
                document.getElementById("cd-hours").innerText = hours.toString().padStart(2, '0');
                document.getElementById("cd-minutes").innerText = minutes.toString().padStart(2, '0');
                document.getElementById("cd-seconds").innerText = seconds.toString().padStart(2, '0');

            }, 1000);
        }
    }
});