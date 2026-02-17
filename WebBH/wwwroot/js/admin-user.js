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
});