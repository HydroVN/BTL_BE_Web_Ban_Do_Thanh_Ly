document.addEventListener('DOMContentLoaded', function () {
    // 1. Lấy dữ liệu kho từ Input Hidden
    const variantsDataEl = document.getElementById('variantsData');
    if (!variantsDataEl) return;

    const variants = JSON.parse(variantsDataEl.value);

    // 2. Lấy các Element
    const sizeInputs = document.querySelectorAll('input[name="size"]');
    const colorInputs = document.querySelectorAll('input[name="color"]');
    const stockInfo = document.getElementById('stock-info');
    const btnAdd = document.getElementById('btnAddToCart');
    const btnBuy = document.getElementById('btnBuyNow');

    // --- HÀM 1: CẬP NHẬT TRẠNG THÁI NÚT (LÀM MỜ KHI HẾT HÀNG) ---
    function updateOptionsAvailability() {
        const selectedSize = document.querySelector('input[name="size"]:checked')?.value || "";
        const selectedColor = document.querySelector('input[name="color"]:checked')?.value || "";

        // A. Xử lý các nút MÀU
        colorInputs.forEach(input => {
            const color = input.value;
            const label = document.querySelector(`label[for="${input.id}"]`);
            let isAvailable = false;

            if (selectedSize) {
                const variant = variants.find(v => (v.Size || "") === selectedSize && (v.Color || "") === color);
                isAvailable = variant && variant.Quantity > 0;
            } else {
                isAvailable = variants.some(v => (v.Color || "") === color && v.Quantity > 0);
            }
            toggleOptionState(input, label, isAvailable);
        });

        // B. Xử lý các nút SIZE
        sizeInputs.forEach(input => {
            const size = input.value;
            const label = document.querySelector(`label[for="${input.id}"]`);
            let isAvailable = false;

            if (selectedColor) {
                const variant = variants.find(v => (v.Size || "") === size && (v.Color || "") === selectedColor);
                isAvailable = variant && variant.Quantity > 0;
            } else {
                isAvailable = variants.some(v => (v.Size || "") === size && v.Quantity > 0);
            }
            toggleOptionState(input, label, isAvailable);
        });

        // C. Cập nhật nút Mua hàng
        updateBuyButtonState(selectedSize, selectedColor);
    }

    function toggleOptionState(input, label, isAvailable) {
        if (isAvailable) {
            label.classList.remove('option-disabled');
            input.disabled = false;
        } else {
            label.classList.add('option-disabled');
            input.disabled = true;
            // Xử lý an toàn: nếu nút đang bị disable mà lại đang check thì bỏ check
            if (input.checked) {
                input.checked = false;
                // Cần reset lại biến theo dõi click bên dưới nếu nó bị disable
                if (input.name === 'size') lastSelectedSize = null;
                if (input.name === 'color') lastSelectedColor = null;
            }
        }
    }

    // --- HÀM 2: CẬP NHẬT NÚT MUA & THÔNG BÁO CHỮ ---
    function updateBuyButtonState(size, color) {
        stockInfo.innerHTML = "";
        btnAdd.disabled = true;
        btnBuy.disabled = true;
        btnAdd.innerHTML = "THÊM GIỎ HÀNG";

        const hasSizeOptions = sizeInputs.length > 0;
        const hasColorOptions = colorInputs.length > 0;

        if ((hasSizeOptions && !size) || (hasColorOptions && !color)) {
            stockInfo.innerHTML = '<span class="text-muted small">Vui lòng chọn đầy đủ phân loại</span>';
            return;
        }

        const match = variants.find(v => (v.Size || "") === size && (v.Color || "") === color);

        if (match && match.Quantity > 0) {
            stockInfo.innerHTML = `<span class="text-success fw-bold"><i class="bi bi-check-circle"></i> Còn lại: ${match.Quantity} sản phẩm</span>`;
            btnAdd.disabled = false;
            btnBuy.disabled = false;

            const qtyInput = document.querySelector('input[name="quantity"]');
            if (qtyInput) {
                qtyInput.max = match.Quantity;
                if (parseInt(qtyInput.value) > match.Quantity) qtyInput.value = match.Quantity;
            }
        } else {
            stockInfo.innerHTML = '<span class="text-danger fw-bold">Sản phẩm này tạm hết hàng</span>';
        }
    }


    // ==============================================================
    // FIX: LOGIC BẤM LẠI ĐỂ BỎ CHỌN (TOGGLE RADIO BUTTONS)
    // ==============================================================
    let lastSelectedSize = null;
    sizeInputs.forEach(el => {
        // Sự kiện click để bắt trường hợp bấm vào cái đang chọn
        el.addEventListener('click', function (e) {
            if (lastSelectedSize === this) {
                this.checked = false; // Gỡ check
                lastSelectedSize = null; // Trả về null
                updateOptionsAvailability(); // Cập nhật lại kho
            } else {
                lastSelectedSize = this; // Lưu lại cái mới chọn
            }
        });
        // Sự kiện change chạy mặc định khi chọn cái mới
        el.addEventListener('change', updateOptionsAvailability);
    });

    let lastSelectedColor = null;
    colorInputs.forEach(el => {
        el.addEventListener('click', function (e) {
            if (lastSelectedColor === this) {
                this.checked = false;
                lastSelectedColor = null;
                updateOptionsAvailability();
            } else {
                lastSelectedColor = this;
            }
        });
        el.addEventListener('change', updateOptionsAvailability);
    });

    updateOptionsAvailability();
});

// ============================================================
// PHẦN XỬ LÝ MUA HÀNG & GỌI MODAL NẾU CHƯA ĐĂNG NHẬP
// ============================================================

function handlePurchase(url, isBuyNow) {
    const form = document.getElementById('cartForm');

    // 1. Validate Size 
    const sizeRadios = document.querySelectorAll('input[name="size"]');
    if (sizeRadios.length > 0 && !document.querySelector('input[name="size"]:checked')) {
        alert("Vui lòng chọn Size!");
        return;
    }

    // 2. Validate Color
    const colorRadios = document.querySelectorAll('input[name="color"]');
    if (colorRadios.length > 0 && !document.querySelector('input[name="color"]:checked')) {
        alert("Vui lòng chọn Màu!");
        return;
    }

    // 3. Kiểm tra nút có bị disable không
    const btnAdd = document.getElementById('btnAddToCart');
    if (btnAdd.disabled) {
        alert("Sản phẩm này hiện không khả dụng!");
        return;
    }

    const formData = new FormData(form);
    if (!formData.has('size')) formData.append('size', '');
    if (!formData.has('color')) formData.append('color', '');

    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    const token = tokenInput ? tokenInput.value : '';

    fetch(url, {
        method: 'POST',
        body: formData,
        headers: {
            'RequestVerificationToken': token,
            'X-Requested-With': 'XMLHttpRequest'
        }
    })
        .then(res => res.json())
        .then(data => {
            // === GỌI MODAL HTML NẾU CHƯA ĐĂNG NHẬP ===
            if (data.requireLogin) {
                var loginModal = new bootstrap.Modal(document.getElementById('loginRequireModal'));
                loginModal.show();
                return;
            }

            // === KHI ĐÃ ĐĂNG NHẬP ===
            if (data.success) {
                if (isBuyNow && data.redirectUrl) {
                    window.location.href = data.redirectUrl;
                } else {
                    updateCartBadge(data.count);
                    showCartToast(data.newItem);
                }
            } else {
                alert(data.message || "Lỗi không xác định");
            }
        })
        .catch(err => console.error("Error:", err));
}

function updateCartBadge(count) {
    const badge = document.getElementById('cart-badge');
    if (badge) {
        badge.innerText = count;
        badge.style.display = count > 0 ? 'inline-block' : 'none';
    }
}

function showCartToast(item) {
    const toast = document.getElementById('cart-notification');
    if (!toast) return;

    document.getElementById('toast-img').src = item.imageUrl || "https://dummyimage.com/50x50/eee/aaa";
    document.getElementById('toast-name').innerText = item.name;
    document.getElementById('toast-size').innerText = item.size || "Mặc định";
    document.getElementById('toast-color').innerText = item.color || "Mặc định";
    document.getElementById('toast-price').innerText = item.price;

    toast.classList.add('show');
    if (window.toastTimeout) clearTimeout(window.toastTimeout);
    window.toastTimeout = setTimeout(() => { closeToast(); }, 4000);
}

function closeToast() {
    const toast = document.getElementById('cart-notification');
    if (toast) toast.classList.remove('show');
}