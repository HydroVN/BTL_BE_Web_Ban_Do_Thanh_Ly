/**
 * File: wwwroot/js/product-details.js
 * Chức năng: 
 * 1. Kiểm tra tồn kho & Chặn chọn phân loại hết hàng.
 * 2. Xử lý thêm vào giỏ hàng bằng Ajax & Hiện thông báo.
 */

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
        const selectedSize = document.querySelector('input[name="size"]:checked')?.value;
        const selectedColor = document.querySelector('input[name="color"]:checked')?.value;

        // A. Xử lý các nút MÀU (Dựa trên Size đang chọn)
        colorInputs.forEach(input => {
            const color = input.value;
            const label = document.querySelector(`label[for="${input.id}"]`);
            let isAvailable = false;

            if (selectedSize) {
                // Đã chọn Size -> Check xem cặp (Size này + Màu này) có hàng không?
                const variant = variants.find(v => v.Size === selectedSize && v.Color === color);
                isAvailable = variant && variant.Quantity > 0;
            } else {
                // Chưa chọn Size -> Check xem Màu này có hàng ở bất kỳ Size nào không?
                isAvailable = variants.some(v => v.Color === color && v.Quantity > 0);
            }

            toggleOptionState(input, label, isAvailable);
        });

        // B. Xử lý các nút SIZE (Dựa trên Màu đang chọn)
        sizeInputs.forEach(input => {
            const size = input.value;
            const label = document.querySelector(`label[for="${input.id}"]`);
            let isAvailable = false;

            if (selectedColor) {
                // Đã chọn Màu -> Check xem cặp (Màu này + Size này) có hàng không?
                const variant = variants.find(v => v.Size === size && v.Color === selectedColor);
                isAvailable = variant && variant.Quantity > 0;
            } else {
                // Chưa chọn Màu -> Check xem Size này có hàng ở bất kỳ Màu nào không?
                isAvailable = variants.some(v => v.Size === size && v.Quantity > 0);
            }

            toggleOptionState(input, label, isAvailable);
        });

        // C. Cập nhật nút Mua hàng & Thông báo chữ
        updateBuyButtonState(selectedSize, selectedColor);
    }

    // Hàm phụ: Bật/Tắt trạng thái nút
    function toggleOptionState(input, label, isAvailable) {
        if (isAvailable) {
            label.classList.remove('option-disabled');
            input.disabled = false;
        } else {
            label.classList.add('option-disabled');
            input.disabled = true;
            // Nếu nút đang chọn bị disable (do chọn chéo) thì bỏ chọn nó đi
            if (input.checked) input.checked = false;
        }
    }

    // --- HÀM 2: CẬP NHẬT NÚT MUA & THÔNG BÁO CHỮ ---
    function updateBuyButtonState(size, color) {
        // Reset trạng thái mặc định
        stockInfo.innerHTML = "";
        btnAdd.disabled = true;
        btnBuy.disabled = true;
        btnAdd.innerHTML = "THÊM GIỎ HÀNG";

        // Nếu chưa chọn đủ (hoặc vừa bị bỏ chọn do hết hàng)
        if (!size || !color) {
            stockInfo.innerHTML = '<span class="text-muted small">Vui lòng chọn Size và Màu sắc</span>';
            return;
        }

        // Tìm biến thể khớp
        const match = variants.find(v => v.Size === size && v.Color === color);

        if (match && match.Quantity > 0) {
            // CÒN HÀNG
            stockInfo.innerHTML = `<span class="text-success fw-bold"><i class="bi bi-check-circle"></i> Còn lại: ${match.Quantity} sản phẩm</span>`;

            btnAdd.disabled = false;
            btnBuy.disabled = false;

            // Cập nhật max cho ô nhập số lượng
            const qtyInput = document.querySelector('input[name="quantity"]');
            if (qtyInput) {
                qtyInput.max = match.Quantity;
                if (parseInt(qtyInput.value) > match.Quantity) qtyInput.value = match.Quantity;
            }
        } else {
            // HẾT HÀNG (Trường hợp hiếm vì đã chặn click ở trên)
            stockInfo.innerHTML = '<span class="text-danger fw-bold">Sản phẩm này tạm hết hàng</span>';
        }
    }

    // Gắn sự kiện Change
    sizeInputs.forEach(el => el.addEventListener('change', updateOptionsAvailability));
    colorInputs.forEach(el => el.addEventListener('change', updateOptionsAvailability));

    // Chạy 1 lần lúc mới tải trang để chặn những món hết sạch
    updateOptionsAvailability();
});

// ============================================================
// PHẦN XỬ LÝ MUA HÀNG & TOAST (AJAX)
// ============================================================

function handlePurchase(url, isAjax) {
    const form = document.getElementById('cartForm');

    // Kiểm tra Form hợp lệ (đã chọn size/color chưa)
    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    // Kiểm tra lần cuối xem nút có bị disabled không
    const btnAdd = document.getElementById('btnAddToCart');
    if (btnAdd.disabled) {
        alert("Sản phẩm này hiện không khả dụng!");
        return;
    }

    if (isAjax) {
        const formData = new FormData(form);
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
                    // Cập nhật giao diện khi thành công
                    updateCartBadge(data.count);
                    showCartToast(data.newItem);
                } else if (data) {
                    alert(data.message || "Lỗi không xác định");
                }
            })
            .catch(err => console.error("Error:", err));
    } else {
        // Mua ngay -> Submit form bình thường
        form.action = url;
        form.submit();
    }
}

// Cập nhật số lượng trên Header
function updateCartBadge(count) {
    const badge = document.getElementById('cart-badge'); // Đảm bảo Header có id này
    if (badge) {
        badge.innerText = count;
        badge.style.display = count > 0 ? 'inline-block' : 'none';
    }
}

// Hiển thị thông báo trượt (Toast)
function showCartToast(item) {
    const toast = document.getElementById('cart-notification');
    if (!toast) return;

    // Điền dữ liệu
    document.getElementById('toast-img').src = item.imageUrl || "https://dummyimage.com/50x50/eee/aaa";
    document.getElementById('toast-name').innerText = item.name;
    document.getElementById('toast-size').innerText = item.size;
    document.getElementById('toast-color').innerText = item.color;
    document.getElementById('toast-price').innerText = item.price;

    // Hiện Toast
    toast.classList.add('show');

    // Tự động ẩn sau 4 giây
    if (window.toastTimeout) clearTimeout(window.toastTimeout);
    window.toastTimeout = setTimeout(() => { closeToast(); }, 4000);
}

function closeToast() {
    const toast = document.getElementById('cart-notification');
    if (toast) toast.classList.remove('show');
}