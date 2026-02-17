document.addEventListener('DOMContentLoaded', function () {
    // 1. Lấy dữ liệu kho từ input hidden
    const variantsDataEl = document.getElementById('variantsData');
    if (!variantsDataEl) return;

    const variants = JSON.parse(variantsDataEl.value);

    const sizeInputs = document.querySelectorAll('input[name="size"]');
    const colorInputs = document.querySelectorAll('input[name="color"]');
    const btnAdd = document.getElementById('btnAddToCart');
    const btnBuy = document.getElementById('btnBuyNow');
    const stockDisplay = document.getElementById('stock-display');
    const qtyInput = document.getElementById('quantityInput');

    function updateUI() {
        const selectedSize = document.querySelector('input[name="size"]:checked')?.value;
        const selectedColor = document.querySelector('input[name="color"]:checked')?.value;

        // Nếu chưa chọn đủ Size và Màu
        if (!selectedSize || !selectedColor) {
            stockDisplay.textContent = "(Vui lòng chọn đủ Size và Màu)";
            stockDisplay.style.color = "#6c757d";
            btnAdd.disabled = true;
            btnBuy.disabled = true;
            return;
        }

        // Tìm biến thể khớp trong mảng JSON
        const match = variants.find(v => v.Size === selectedSize && v.Color === selectedColor);

        if (match) {
            if (match.Quantity > 0) {
                // CÒN HÀNG
                stockDisplay.textContent = "Còn lại: " + match.Quantity + " sản phẩm";
                stockDisplay.style.color = "#198754";

                btnAdd.disabled = false;
                btnAdd.innerHTML = '<i class="bi bi-cart-plus"></i> Thêm vào giỏ';

                btnBuy.disabled = false;
                btnBuy.style.display = "inline-block";

                qtyInput.max = match.Quantity;
                if (parseInt(qtyInput.value) > match.Quantity) {
                    qtyInput.value = match.Quantity;
                }
            } else {
                // HẾT HÀNG
                stockDisplay.textContent = "Xin lỗi, phân loại này đã hết hàng!";
                stockDisplay.style.color = "#dc3545";

                btnAdd.disabled = true;
                btnAdd.innerHTML = '<i class="bi bi-x-circle"></i> Hết hàng';

                btnBuy.disabled = true;
                btnBuy.style.display = "none"; // Ẩn luôn nút mua ngay khi hết
            }
        } else {
            // KHÔNG CÓ CẶP PHÂN LOẠI NÀY
            stockDisplay.textContent = "Phân loại này không tồn tại";
            stockDisplay.style.color = "#6c757d";
            btnAdd.disabled = true;
            btnBuy.disabled = true;
        }
    }

    // Gán sự kiện
    sizeInputs.forEach(el => el.addEventListener('change', updateUI));
    colorInputs.forEach(el => el.addEventListener('change', updateUI));
});

// Hàm tăng giảm số lượng (gọi trực tiếp từ onclick trong HTML)
function changeQuantity(val) {
    const input = document.getElementById('quantityInput');
    let current = parseInt(input.value);
    let max = parseInt(input.max) || 999;
    let next = current + val;

    if (next >= 1 && next <= max) {
        input.value = next;
    }
}