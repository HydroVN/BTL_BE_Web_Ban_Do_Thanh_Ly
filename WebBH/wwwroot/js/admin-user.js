/**
 * File: wwwroot/js/admin-user.js
 * Xử lý sự kiện Modal Edit User
 */

document.addEventListener('DOMContentLoaded', function () {
    var editUserModal = document.getElementById('editModal');

    if (editUserModal) {
        editUserModal.addEventListener('show.bs.modal', function (event) {
            // Nút bấm kích hoạt modal
            var button = event.relatedTarget;

            // Lấy dữ liệu từ các thuộc tính data-
            var id = button.getAttribute('data-id');
            var name = button.getAttribute('data-name');
            var email = button.getAttribute('data-email');
            var phone = button.getAttribute('data-phone');

            // Điền dữ liệu vào các ô input trong Modal
            var modal = this;
            modal.querySelector('#m_id').value = id;
            modal.querySelector('#m_name').value = name;
            modal.querySelector('#m_email').value = email;
            modal.querySelector('#m_phone').value = phone;
        });
    }
})
// Hàm mở Modal Khóa Tài Khoản
function openUnbanModal(id, email, name) {
    const unbanUserId = document.getElementById('unbanUserId');
    const unbanUserName = document.getElementById('unbanUserName');
    const unbanUserDisplayId = document.getElementById('unbanUserDisplayId');
    const unbanUserEmail = document.getElementById('unbanUserEmail');

    if (unbanUserId) unbanUserId.value = id;
    if (unbanUserName) unbanUserName.innerText = name || "Khách hàng";

    // Format ID giống thiết kế (VD: USR-00012)
    if (unbanUserDisplayId) unbanUserDisplayId.innerText = "USR-" + id.toString().padStart(5, '0');
    if (unbanUserEmail) unbanUserEmail.innerText = email;

    const modalEl = document.getElementById('unbanUserModal');
    if (modalEl) {
        const modal = new bootstrap.Modal(modalEl);
        modal.show();
    }
}
// Hàm mở Modal Xóa Người Dùng
function openDeleteModal(id, email, name) {
    const deleteUserId = document.getElementById('deleteUserId');
    const deleteUserName = document.getElementById('deleteUserName');
    const deleteUserDisplayId = document.getElementById('deleteUserDisplayId');
    const deleteUserEmail = document.getElementById('deleteUserEmail');

    if (deleteUserId) deleteUserId.value = id;
    if (deleteUserName) deleteUserName.innerText = name || "Khách hàng";
    if (deleteUserDisplayId) deleteUserDisplayId.innerText = "USR-" + id.toString().padStart(5, '0');
    if (deleteUserEmail) deleteUserEmail.innerText = email;

    const modalEl = document.getElementById('deleteUserModal');
    if (modalEl) {
        const modal = new bootstrap.Modal(modalEl);
        modal.show();
    }
}