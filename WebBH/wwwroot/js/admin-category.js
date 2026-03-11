let categoryModal;
let deleteConfirmModal; // Thêm biến cho modal xóa

// Hàm hỗ trợ hiển thị Toast
function showToast(message, isSuccess = true) {
    const toastEl = document.getElementById('customToast');
    const toastMessage = document.getElementById('toastMessage');
    const toastIcon = document.getElementById('toastIcon');

    toastMessage.innerText = message;

    if (isSuccess) {
        toastEl.classList.remove('border-danger');
        toastEl.classList.add('border-success');
        toastIcon.className = 'bi bi-check-circle-fill text-success fs-5 me-2';
    } else {
        toastEl.classList.remove('border-success');
        toastEl.classList.add('border-danger');
        toastIcon.className = 'bi bi-x-circle-fill text-danger fs-5 me-2';
    }

    const toast = new bootstrap.Toast(toastEl, { delay: 3000 });
    toast.show();
}

document.addEventListener("DOMContentLoaded", function () {
    // Khởi tạo Modal Thêm/Sửa
    const modalElement = document.getElementById('categoryModal');
    if (modalElement) {
        categoryModal = new bootstrap.Modal(modalElement);
    }

    // Khởi tạo Modal Xác nhận Xóa
    const deleteModalEl = document.getElementById('deleteConfirmModal');
    if (deleteModalEl) {
        deleteConfirmModal = new bootstrap.Modal(deleteModalEl);
    }

    // --- KIỂM TRA XEM CÓ THÔNG BÁO CHỜ NÀO KHÔNG ---
    const pendingMessage = sessionStorage.getItem('toastMessage');
    if (pendingMessage) {
        const isSuccess = sessionStorage.getItem('toastSuccess') === 'true';
        showToast(pendingMessage, isSuccess);

        sessionStorage.removeItem('toastMessage');
        sessionStorage.removeItem('toastSuccess');
    }
});

// Mở modal Thêm/Sửa
function openCategoryModal(id) {
    document.getElementById('categoryForm').reset();
    document.getElementById('CategoryId').value = id;
    document.getElementById('modalTitle').innerText = id === 0 ? "Thêm danh mục mới" : "Cập nhật danh mục";

    if (id > 0) {
        fetch(`/Admin/Categories/GetCategory/${id}`)
            .then(res => res.json())
            .then(data => {
                document.getElementById('Name').value = data.name;
                document.getElementById('Description').value = data.description;
                categoryModal.show();
            });
    } else {
        categoryModal.show();
    }
}

// Lưu danh mục (AJAX)
function saveCategory() {
    const formData = new FormData(document.getElementById('categoryForm'));

    fetch('/Admin/Categories/SaveCategory', {
        method: 'POST',
        body: formData
    })
        .then(res => res.json())
        .then(data => {
            if (data.success) {
                sessionStorage.setItem('toastMessage', data.message);
                sessionStorage.setItem('toastSuccess', 'true');
                categoryModal.hide();
                location.reload();
            } else {
                showToast(data.message, false);
            }
        })
        .catch(err => console.error(err));
}

// 1. GỌI MODAL XÁC NHẬN KHI BẤM NÚT THÙNG RÁC
function deleteCategory(id) {
    // Lưu ID vào thẻ input ẩn trong Modal
    document.getElementById('deleteCategoryId').value = id;
    // Hiển thị Modal
    deleteConfirmModal.show();
}

// 2. THỰC THI XÓA KHI BẤM NÚT "TIẾP TỤC" TRONG MODAL
function executeDeleteCategory() {
    // Lấy ID từ thẻ ẩn
    const id = document.getElementById('deleteCategoryId').value;
    const formData = new FormData();
    formData.append('id', id);

    fetch(`/Admin/Categories/Delete`, {
        method: 'POST',
        body: formData
    })
        .then(res => res.json())
        .then(data => {
            deleteConfirmModal.hide(); // Đóng Modal xác nhận

            if (data.success) {
                sessionStorage.setItem('toastMessage', data.message);
                sessionStorage.setItem('toastSuccess', 'true');
                location.reload();
            } else {
                showToast(data.message, false); // Hiện lỗi (VD: không cho xóa vì đang có sản phẩm)
            }
        })
        .catch(err => console.error(err));
}