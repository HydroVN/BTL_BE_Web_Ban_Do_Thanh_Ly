function toggleFavorite(btn, productId) {
    if (btn.disabled) return;
    btn.disabled = true;

    const icon = btn.querySelector('i');

    fetch('/Favorite/Toggle?productId=' + productId, {
        method: 'POST',
        headers: {
            'X-Requested-With': 'XMLHttpRequest'
        }
    })
        .then(res => res.json())
        .then(data => {
            // ========================================================
            // ĐÂY LÀ ĐOẠN HIỆN MODAL YÊU CẦU ĐĂNG NHẬP GIỐNG MUA HÀNG
            // ========================================================
            if (data.requireLogin) {
                var loginModal = new bootstrap.Modal(document.getElementById('loginRequireModal'));
                loginModal.show();
                return; // Dừng lại không chạy tiếp code bên dưới
            }

            if (data.success) {
                if (data.status === 'added') {
                    icon.classList.remove('bi-heart', 'text-dark');
                    icon.classList.add('bi-heart-fill', 'text-danger');
                } else {
                    icon.classList.remove('bi-heart-fill', 'text-danger');
                    icon.classList.add('bi-heart', 'text-dark');
                }
            } else {
                alert(data.message || "Đã xảy ra lỗi không xác định!");
            }
        })
        .catch(err => {
            console.error("Lỗi:", err);
            alert("Đã có lỗi xảy ra. Vui lòng thử lại.");
        })
        .finally(() => {
            btn.disabled = false;
        });
}

function removeFavoriteItem(productId) {
    if (!confirm("Bạn muốn bỏ sản phẩm này khỏi danh sách yêu thích?")) return;

    fetch('/Favorite/Toggle?productId=' + productId, {
        method: 'POST',
        headers: {
            'X-Requested-With': 'XMLHttpRequest'
        }
    })
        .then(res => res.json())
        .then(data => {
            // Hiện Modal nếu lỡ hết hạn đăng nhập
            if (data.requireLogin) {
                var loginModal = new bootstrap.Modal(document.getElementById('loginRequireModal'));
                loginModal.show();
                return;
            }

            if (data.success && data.status === 'removed') {
                const itemElement = document.getElementById('fav-item-' + productId);
                if (itemElement) itemElement.remove();

                const remainingItems = document.querySelectorAll('[id^="fav-item-"]');
                if (remainingItems.length === 0) location.reload();
            } else {
                alert(data.message || "Lỗi không xác định!");
            }
        })
        .catch(err => console.error(err));
}