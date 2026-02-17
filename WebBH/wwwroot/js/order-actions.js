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
function showConfirmModal(orderId, name, img, size, color, price) {
    // Điền dữ liệu vào các thẻ trong Modal
    document.getElementById('conf-orderId').value = orderId;
    document.getElementById('conf-name').innerText = name;
    document.getElementById('conf-img').src = img || '/images/default-prod.jpg';
    document.getElementById('conf-meta').innerText = `Size: ${size || 'N/A'} | Màu: ${color || 'N/A'}`;
    document.getElementById('conf-price').innerText = price;

    // Hiển thị Modal
    var myModal = new bootstrap.Modal(document.getElementById('receivedConfirmModal'));
    myModal.show();
}
function showReceivedModal(orderId, name, img, meta, price) {
    // 1. Điền dữ liệu vào các thẻ trong Modal
    const orderIdInput = document.getElementById('conf-orderId');
    const nameEl = document.getElementById('conf-name');
    const imgEl = document.getElementById('conf-img');
    const metaEl = document.getElementById('conf-meta');
    const priceEl = document.getElementById('conf-price');

    if (orderIdInput) orderIdInput.value = orderId;
    if (nameEl) nameEl.innerText = name;
    if (imgEl) imgEl.src = img;
    if (metaEl) metaEl.innerText = meta;
    if (priceEl) priceEl.innerText = price;

    // 2. Hiển thị Modal
    const modalEl = document.getElementById('receivedConfirmModal');
    if (modalEl) {
        const myModal = new bootstrap.Modal(modalEl);
        myModal.show();
    } else {
        console.error("Không tìm thấy Modal có id='receivedConfirmModal'");
    }
}