document.addEventListener("DOMContentLoaded", function () {
    // Xử lý nút X đóng trang
    const closeBtn = document.getElementById("closeRestrictedPage");
    if (closeBtn) {
        closeBtn.addEventListener("click", function (e) {
            e.preventDefault();
            window.location.href = "/";
        });
    }
});