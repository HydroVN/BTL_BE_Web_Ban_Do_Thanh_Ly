document.addEventListener('DOMContentLoaded', function () {
    // Lấy phần tử Modal theo ID
    var updateStatusModal = document.getElementById('updateStatusModal');

    // Kiểm tra nếu Modal tồn tại (để tránh lỗi ở trang khác)
    if (updateStatusModal) {
        updateStatusModal.addEventListener('show.bs.modal', function (event) {
            // 1. Xác định nút nào vừa được bấm
            var button = event.relatedTarget;

            // 2. Lấy dữ liệu từ data-attribute của nút đó (đã khai báo bên View)
            var orderId = button.getAttribute('data-order-id');
            var currentStatus = button.getAttribute('data-current-status');

            // 3. Tìm các thẻ input/select bên trong Modal
            var modalOrderIdInput = updateStatusModal.querySelector('#modalOrderId');
            var displayOrderIdSpan = updateStatusModal.querySelector('#displayOrderId');
            var statusSelect = updateStatusModal.querySelector('#statusSelect');

            // 4. Gán giá trị vào Modal
            if (modalOrderIdInput) {
                modalOrderIdInput.value = orderId;
            }

            if (displayOrderIdSpan) {
                displayOrderIdSpan.textContent = '#' + orderId;
            }

            if (statusSelect) {
                statusSelect.value = currentStatus; // Tự động chọn trạng thái hiện tại
            }
        });
    }
});