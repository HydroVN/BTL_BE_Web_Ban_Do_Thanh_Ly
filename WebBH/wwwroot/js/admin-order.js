// File: wwwroot/js/admin-order.js

document.addEventListener('DOMContentLoaded', function () {
    var updateStatusModal = document.getElementById('updateStatusModal');

    if (updateStatusModal) {
        updateStatusModal.addEventListener('show.bs.modal', function (event) {
            // 1. Nút nào kích hoạt modal?
            var button = event.relatedTarget;

            // 2. Lấy dữ liệu từ data attribute
            var orderId = button.getAttribute('data-id');
            var currentStatus = button.getAttribute('data-status');

            // 3. Tìm các input trong modal
            var modalInputId = updateStatusModal.querySelector('input[name="orderId"]');
            var modalDisplayId = updateStatusModal.querySelector('#displayOrderId');
            var modalSelectStatus = updateStatusModal.querySelector('select[name="status"]');

            // 4. Điền dữ liệu
            if (modalInputId) modalInputId.value = orderId;
            if (modalDisplayId) modalDisplayId.textContent = '#' + orderId;
            if (modalSelectStatus) modalSelectStatus.value = currentStatus;
        });
    }
});