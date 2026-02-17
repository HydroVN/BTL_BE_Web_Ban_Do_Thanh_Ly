/**
 * File: wwwroot/js/order-actions.js
 * Xử lý các hành động trên trang Lịch sử đơn hàng (Hủy đơn)
 */

document.addEventListener('DOMContentLoaded', function () {
    // 1. Tìm Modal hủy đơn
    var cancelModal = document.getElementById('cancelOrderModal');

    if (cancelModal) {
        // 2. Lắng nghe sự kiện khi Modal chuẩn bị hiện lên
        cancelModal.addEventListener('show.bs.modal', function (event) {
            // Nút bấm đã kích hoạt modal
            var button = event.relatedTarget;

            // Lấy ID đơn hàng từ data-id của nút đó
            var orderId = button.getAttribute('data-id');

            // Tìm input hidden trong form của modal và gán giá trị
            var modalInput = cancelModal.querySelector('#cancelOrderId');
            if (modalInput) {
                modalInput.value = orderId;
            }
        });
    }
});