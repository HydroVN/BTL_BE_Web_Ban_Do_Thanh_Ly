document.addEventListener("DOMContentLoaded", function () {
    const passwordInput = document.getElementById("passwordInput");
    const confirmInput = document.getElementById("confirmPasswordInput");
    const requirementsBox = document.getElementById("password-requirements");

    // Các dòng check yêu cầu
    const reqLength = document.getElementById("req-length");
    const reqLower = document.getElementById("req-lower");
    const reqUpper = document.getElementById("req-upper");
    const reqSpecial = document.getElementById("req-special");
    const matchMessage = document.getElementById("match-message");

    // --- CÁCH SỬA MỚI (Dùng Focus/Blur) ---

    // 1. Khi ô mật khẩu được Focus (bấm vào ô hoặc bấm vào Label): HIỆN
    passwordInput.addEventListener("focus", function () {
        requirementsBox.style.display = "block";
    });

    // 2. Khi ô mật khẩu mất Focus (bấm ra ngoài): ẨN
    passwordInput.addEventListener("blur", function () {
        requirementsBox.style.display = "none";
    });

    // 3. (Quan trọng) Ngăn việc bấm vào chính cái bảng yêu cầu làm mất focus
    // Giúp bạn có thể bấm/bôi đen text trong bảng mà không bị tắt bảng
    requirementsBox.addEventListener("mousedown", function (e) {
        e.preventDefault();
    });

    // ---------------------------------------

    // Logic kiểm tra Real-time (Giữ nguyên)
    passwordInput.addEventListener("input", function () {
        const val = passwordInput.value;

        updateRequirement(reqLength, val.length >= 8);
        updateRequirement(reqLower, /[a-z]/.test(val));
        updateRequirement(reqUpper, /[A-Z]/.test(val));
        updateRequirement(reqSpecial, /[\W_]/.test(val));

        checkMatch();
    });

    confirmInput.addEventListener("input", function () {
        checkMatch();
    });

    function updateRequirement(element, isValid) {
        const icon = element.querySelector("i");
        if (isValid) {
            element.classList.add("valid");
            icon.className = "bi bi-check-circle-fill";
        } else {
            element.classList.remove("valid");
            icon.className = "bi bi-x-circle";
        }
    }

    function checkMatch() {
        if (!confirmInput.value) {
            matchMessage.style.display = "none";
            return;
        }
        matchMessage.style.display = "block";
        if (passwordInput.value === confirmInput.value && passwordInput.value !== "") {
            matchMessage.innerHTML = '<i class="bi bi-check-lg text-success"></i>';
        } else {
            matchMessage.innerHTML = '<i class="bi bi-x-lg text-danger"></i>';
        }
    }
});