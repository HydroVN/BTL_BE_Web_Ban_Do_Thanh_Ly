let targetCartItemId = null;
let toastTimeout;

function updateQty(cartItemId, change) {
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    const qtyElement = document.getElementById(`qty-${cartItemId}`);

    fetch(`/Cart/UpdateQuantity?cartItemId=${cartItemId}&change=${change}`, {
        method: 'POST',
        headers: { 'RequestVerificationToken': token }
    })
        .then(res => res.json())
        .then(data => {
            if (data.success) {
                if (qtyElement) qtyElement.innerText = data.newQuantity;
                document.getElementById(`price-${cartItemId}`).innerText = data.itemTotal;
                document.getElementById('cart-total-price').innerText = data.cartTotal;
            } else {
                // === XỬ LÝ LỖI ===

                if (data.isStockError) {
                    // Lỗi 1: Hết hàng
                    if (qtyElement) qtyElement.innerText = data.maxStock;

                    showToast(
                        "Kho hàng có hạn",
                        `Chỉ còn lại <strong class="text-white">${data.maxStock} sản phẩm</strong> trong kho.`
                    );
                }
                else if (data.isMinError) {
                    // Lỗi 2: Tối thiểu là 1
                    if (qtyElement) qtyElement.innerText = 1;

                    showToast(
                        "Số lượng tối thiểu",
                        `Bạn phải mua ít nhất <strong class="text-white">1 sản phẩm</strong>.`
                    );
                }
                else {
                    alert(data.message);
                }
            }
        })
        .catch(err => console.error("Lỗi:", err));
}

// === HÀM HIỆN TOAST (QUAN TRỌNG) ===
function showToast(title, htmlMessage) {
    const toast = document.getElementById('alert-toast');

    // Kiểm tra kỹ xem có tìm thấy thẻ HTML không
    if (!toast) {
        console.error("Lỗi: Không tìm thấy HTML id='alert-toast' trong file View.");
        return;
    }

    // Điền nội dung
    document.getElementById('toast-title').innerText = title;
    document.getElementById('toast-message').innerHTML = htmlMessage;

    // Thêm class 'show' để CSS kích hoạt hiệu ứng hiện ra
    toast.classList.add('show');

    // Reset bộ đếm thời gian cũ (nếu có)
    if (toastTimeout) clearTimeout(toastTimeout);

    // Tự động đóng sau 4 giây
    toastTimeout = setTimeout(() => {
        closeToast();
    }, 4000);
}

function closeToast() {
    const toast = document.getElementById('alert-toast');
    if (toast) {
        toast.classList.remove('show'); // Gỡ class show để CSS ẩn nó đi
    }
}

/* LOGIC MODAL XÓA (Giữ nguyên)                      */
function openDeleteModal(cartItemId, productName, productImg) {
    targetCartItemId = cartItemId;
    document.getElementById('del-modal-name').innerText = productName;
    document.getElementById('del-modal-img').src = productImg;
    document.getElementById('delete-modal').style.display = 'flex';
}

function closeDeleteModal() {
    document.getElementById('delete-modal').style.display = 'none';
    targetCartItemId = null;
}

function confirmDelete() {
    if (!targetCartItemId) return;
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

    fetch(`/Cart/RemoveItem?cartItemId=${targetCartItemId}`, {
        method: 'POST',
        headers: { 'RequestVerificationToken': token }
    })
        .then(res => res.json())
        .then(data => {
            if (data.success) {
                const row = document.getElementById(`row-${targetCartItemId}`);
                if (row) {
                    row.style.opacity = '0';
                    setTimeout(() => row.remove(), 300);
                }
                const totalElement = document.getElementById(`cart-total-price`);
                if (totalElement) totalElement.innerText = data.cartTotal;

                if (data.count === 0) {
                    setTimeout(() => window.location.reload(), 500);
                }
            } else {
                alert("Không thể xóa sản phẩm. Vui lòng thử lại.");
            }
        })
        .catch(err => console.error("Lỗi:", err))
        .finally(() => {
            closeDeleteModal();
        });
}

window.onclick = function (event) {
    const modal = document.getElementById('delete-modal');
    if (event.target == modal) {
        closeDeleteModal();
    }
}