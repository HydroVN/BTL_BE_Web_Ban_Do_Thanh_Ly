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

        // A. Xử lý các nút MÀU (Dựa trên Size đang chọn)
        colorInputs.forEach(input => {
            const color = input.value;
            const label = document.querySelector(`label[for="${input.id}"]`);
            let isAvailable = false;

            if (selectedSize) {
                // Đã chọn Size -> Check xem cặp (Size này + Màu này) có hàng không?
                const variant = variants.find(v => (v.Size || "") === selectedSize && (v.Color || "") === color);
                isAvailable = variant && variant.Quantity > 0;
            } else {
                // Chưa chọn Size (hoặc không có Size) -> Check xem Màu này có hàng không?
                // Logic: Tìm variant có màu này và (Size rỗng hoặc Size bất kỳ còn hàng)
                isAvailable = variants.some(v => (v.Color || "") === color && v.Quantity > 0);
            }
            toggleOptionState(input, label, isAvailable);
        });

        // B. Xử lý các nút SIZE (Dựa trên Màu đang chọn)
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
            if (input.checked) input.checked = false;
        }
    }

    // --- HÀM 2: CẬP NHẬT NÚT MUA & THÔNG BÁO CHỮ ---
    function updateBuyButtonState(size, color) {
        stockInfo.innerHTML = "";
        btnAdd.disabled = true;
        btnBuy.disabled = true;
        btnAdd.innerHTML = "THÊM GIỎ HÀNG";

        // Logic mới: Chỉ bắt buộc chọn NẾU CÓ danh sách chọn
        const hasSizeOptions = sizeInputs.length > 0;
        const hasColorOptions = colorInputs.length > 0;

        if ((hasSizeOptions && !size) || (hasColorOptions && !color)) {
            stockInfo.innerHTML = '<span class="text-muted small">Vui lòng chọn đầy đủ phân loại</span>';
            return;
        }

        // Tìm biến thể khớp (Lưu ý so sánh chuỗi rỗng)
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

    sizeInputs.forEach(el => el.addEventListener('change', updateOptionsAvailability));
    colorInputs.forEach(el => el.addEventListener('change', updateOptionsAvailability));
    updateOptionsAvailability();
});

// ============================================================
// PHẦN XỬ LÝ MUA HÀNG (SỬA ĐỔI)
// ============================================================

function handlePurchase(url, isAjax) {
    const form = document.getElementById('cartForm');

    // 1. Validate Size (chỉ khi có nút chọn)
    const sizeRadios = document.querySelectorAll('input[name="size"]');
    if (sizeRadios.length > 0 && !document.querySelector('input[name="size"]:checked')) {
        alert("Vui lòng chọn Size!");
        return;
    }

    // 2. Validate Color (chỉ khi có nút chọn)
    const colorRadios = document.querySelectorAll('input[name="color"]');
    if (colorRadios.length > 0 && !document.querySelector('input[name="color"]:checked')) {
        alert("Vui lòng chọn Màu!");
        return;
    }

    // 3. Kiểm tra nút mua có bị disable không
    const btnAdd = document.getElementById('btnAddToCart');
    if (btnAdd.disabled) {
        alert("Sản phẩm này hiện không khả dụng!");
        return;
    }

    if (isAjax) {
        const formData = new FormData(form);

        // Đảm bảo gửi chuỗi rỗng nếu không có giá trị (để Backend nhận được)
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
            .then(res => {
                if (res.status === 401) {
                    alert("Vui lòng đăng nhập để tiếp tục!");
                    window.location.href = '/Account/Login';
                    return;
                }
                return res.json();
            })
            .then(data => {
                if (data && data.success) {
                    updateCartBadge(data.count);
                    showCartToast(data.newItem);
                } else if (data) {
                    alert(data.message || "Lỗi không xác định");
                }
            })
            .catch(err => console.error("Error:", err));
    } else {
        form.action = url;
        form.submit();
    }
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
    document.getElementById('toast-size').innerText = item.size || "Tiêu chuẩn"; // Hiện chữ khác nếu rỗng
    document.getElementById('toast-color').innerText = item.color || "Tiêu chuẩn";
    document.getElementById('toast-price').innerText = item.price;

    toast.classList.add('show');
    if (window.toastTimeout) clearTimeout(window.toastTimeout);
    window.toastTimeout = setTimeout(() => { closeToast(); }, 4000);
}

function closeToast() {
    const toast = document.getElementById('cart-notification');
    if (toast) toast.classList.remove('show');
}