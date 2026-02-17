/* ================= QUẢN LÝ SẢN PHẨM (Full Logic) ================= */
document.addEventListener("DOMContentLoaded", function () {

    const sidebar = document.getElementById("sidebar");
    const toggleBtn = document.getElementById("toggleSidebar");

    if (!sidebar || !toggleBtn) return;

    toggleBtn.addEventListener("click", function (e) {
        e.preventDefault();

        sidebar.classList.toggle("expanded");

        localStorage.setItem(
            "sidebar-expanded",
            sidebar.classList.contains("expanded")
        );
    });

    // restore state
    if (localStorage.getItem("sidebar-expanded") === "true") {
        sidebar.classList.add("expanded");
    }

});

/**
 * 1. Mở Modal (Form)
 * @param {number} id - ID sản phẩm (0 là thêm mới, >0 là sửa)
 */
// Mở Modal
function openProductModal(id) {
    const form = document.getElementById('productForm');
    if (!form) return;

    form.reset();
    document.getElementById('ProductId').value = 0;
    document.getElementById('modalTitle').innerText = "Thêm mới sản phẩm";
    document.getElementById('variantContainer').innerHTML = "";
    checkEmptyVariant();

    if (id > 0) {
        document.getElementById('modalTitle').innerText = "Cập nhật sản phẩm";

        fetch(`/Admin/Products/GetProduct/${id}`)
            .then(res => res.json())
            .then(data => {
                document.getElementById('ProductId').value = data.productId;
                document.getElementById('Name').value = data.name;
                document.getElementById('CategoryId').value = data.categoryId;
                document.getElementById('Price').value = data.price;
                document.getElementById('Description').value = data.description || '';
                document.getElementById('IsActive').checked = data.isActive;

                // Load variants
                if (data.variants && data.variants.length > 0) {
                    data.variants.forEach(v => {
                        // Xử lý hiển thị null thành chuỗi rỗng để không hiện chữ "null" lên ô input
                        addVariantRow(v.size || '', v.color || '', v.quantity);
                    });
                }
            })
            .catch(err => console.error(err));
    }

    new bootstrap.Modal(document.getElementById('productModal')).show();
}

function addVariantRow(size = '', color = '', qty = 1) {
    const container = document.getElementById('variantContainer');
    const row = document.createElement('tr');
    row.innerHTML = `
        <td><input type="text" class="form-control form-control-sm bg-dark text-white border-secondary variant-size" placeholder="Size" value="${size}"></td>
        <td><input type="text" class="form-control form-control-sm bg-dark text-white border-secondary variant-color" placeholder="Màu" value="${color}"></td>
        <td><input type="number" class="form-control form-control-sm bg-dark text-white border-secondary variant-qty" value="${qty}" min="0"></td>
        <td class="text-end">
            <button type="button" class="btn btn-sm text-danger" onclick="this.closest('tr').remove(); checkEmptyVariant();"><i class="bi bi-x-lg"></i></button>
        </td>
    `;
    container.appendChild(row);
    checkEmptyVariant();
}

function checkEmptyVariant() {
    const container = document.getElementById('variantContainer');
    const msg = document.getElementById('emptyVariantMsg');
    if (container.children.length === 0) msg.style.display = 'block';
    else msg.style.display = 'none';
}

function saveProduct() {
    const form = document.getElementById('productForm');
    const formData = new FormData(form);

    // Xử lý checkbox
    formData.set('IsActive', document.getElementById('IsActive').checked);

    // Gom variants
    const variants = [];
    document.querySelectorAll('#variantContainer tr').forEach(row => {
        const size = row.querySelector('.variant-size').value.trim();
        const color = row.querySelector('.variant-color').value.trim();
        const qty = row.querySelector('.variant-qty').value;

        // --- SỬA ĐỔI TẠI ĐÂY ---
        // Cũ: if (size && color) -> Bắt buộc cả hai
        // Mới: if (size || color) -> Chỉ cần một trong hai (hoặc cả hai)
        // Nếu bạn muốn cho phép để trống cả 2 (chỉ nhập số lượng), hãy bỏ luôn điều kiện if này.
        if (size || color || qty) {
            variants.push({ Size: size, Color: color, Quantity: parseInt(qty) || 0 });
        }
    });
    formData.append('variantsJson', JSON.stringify(variants));

    fetch('/Admin/Products/Save', {
        method: 'POST',
        body: formData
    })
        .then(res => res.json())
        .then(data => {
            if (data.success) location.reload();
            else alert("Lỗi: " + (data.message || "Không lưu được"));
        })
        .catch(err => alert("Lỗi kết nối server"));
}

function deleteProduct(id) {
    if (confirm("Bạn có chắc chắn muốn xóa?")) {
        fetch(`/Admin/Products/Delete/${id}`, { method: 'POST' })
            .then(res => res.json())
            .then(data => {
                if (data.success) location.reload();
                else alert("Lỗi xóa");
            });
    }
}