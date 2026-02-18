// Hàm Toggle: Thêm hoặc Xóa yêu thích ở trang Danh sách/Chi tiết
function toggleFavorite(btn, productId) {
    // 1. Chặn click liên tục
    if (btn.disabled) return;
    btn.disabled = true;

    const icon = btn.querySelector('i');

    // 2. Gọi API
    fetch('/Favorite/Toggle?productId=' + productId, {
        method: 'POST'
    })
        .then(res => res.json())
        .then(data => {
            if (data.success) {
                // 3. Cập nhật giao diện dựa trên trạng thái trả về
                if (data.status === 'added') {
                    icon.classList.remove('bi-heart', 'text-dark');
                    icon.classList.add('bi-heart-fill', 'text-danger');
                    // (Tùy chọn) Có thể thêm thông báo nhỏ ở đây
                } else {
                    icon.classList.remove('bi-heart-fill', 'text-danger');
                    icon.classList.add('bi-heart', 'text-dark');
                }
            } else {
                // Xử lý lỗi (ví dụ: chưa đăng nhập)
                alert(data.message);
                if (data.message.includes("đăng nhập")) {
                    window.location.href = "/Account/Login";
                }
            }
        })
        .catch(err => {
            console.error("Lỗi:", err);
            alert("Đã có lỗi xảy ra. Vui lòng thử lại.");
        })
        .finally(() => {
            // Mở lại nút
            btn.disabled = false;
        });
}

// Hàm Xóa: Dùng cho trang "Danh sách yêu thích" (có confirm và xóa dòng)
function removeFavoriteItem(productId) {
    if (!confirm("Bạn muốn bỏ sản phẩm này khỏi danh sách yêu thích?")) return;

    fetch('/Favorite/Toggle?productId=' + productId, {
        method: 'POST'
    })
        .then(res => res.json())
        .then(data => {
            if (data.success && data.status === 'removed') {
                // Tìm và xóa thẻ HTML chứa sản phẩm đó
                const itemElement = document.getElementById('fav-item-' + productId);
                if (itemElement) {
                    itemElement.remove();
                }

                // Kiểm tra nếu danh sách trống thì reload để hiện thông báo trống
                const remainingItems = document.querySelectorAll('[id^="fav-item-"]');
                if (remainingItems.length === 0) {
                    location.reload();
                }
            } else {
                alert(data.message);
            }
        })
        .catch(err => console.error(err));
}