// order-review.js

document.addEventListener('DOMContentLoaded', function () {
    // 1. Gán sự kiện cho các nút mở Modal
    const rateBtns = document.querySelectorAll('.btn-open-rate');
    rateBtns.forEach(btn => {
        btn.addEventListener('click', function () {
            // Lấy dữ liệu từ data-attributes
            const pId = this.getAttribute('data-pid');
            const oId = this.getAttribute('data-oid');
            const pName = this.getAttribute('data-name');
            const pImg = this.getAttribute('data-img');
            const rating = parseInt(this.getAttribute('data-rating')) || 0;
            const comment = this.getAttribute('data-comment') || '';
            const media = this.getAttribute('data-media') || '';

            openRateModal(pId, oId, pName, pImg, rating, comment, media);
        });
    });

    // 2. Logic chọn sao
    const stars = document.querySelectorAll('.rm-star');
    const texts = ["Rất tệ", "Tệ", "Bình thường", "Tốt", "Tuyệt vời"];

    stars.forEach(star => {
        star.addEventListener('mouseover', function () {
            if (document.querySelector('textarea[name="comment"]').readOnly) return;
            let val = this.getAttribute('data-val');
            highlightStars(val);
        });

        star.addEventListener('click', function () {
            if (document.querySelector('textarea[name="comment"]').readOnly) return;
            let val = this.getAttribute('data-val');
            document.getElementById('rmRating').value = val;
            document.getElementById('rmRatingText').innerText = texts[val - 1];
            highlightStars(val);
        });
    });

    document.querySelector('.rm-stars').addEventListener('mouseleave', function () {
        if (document.querySelector('textarea[name="comment"]').readOnly) return;
        let currentRating = document.getElementById('rmRating').value;
        highlightStars(currentRating);
    });

    function highlightStars(val) {
        stars.forEach(s => {
            if (s.getAttribute('data-val') <= val) s.classList.add('active');
            else s.classList.remove('active');
        });
    }

    // 3. Logic upload file (Preview)
    const fileInput = document.getElementById('rmFile');
    const previewContainer = document.getElementById('rmPreview');
    if (fileInput) {
        fileInput.addEventListener('change', function () {
            previewContainer.innerHTML = "";
            if (this.files.length > 5) {
                alert("Bạn chỉ được chọn tối đa 5 file!");
                this.value = "";
                return;
            }
            Array.from(this.files).forEach(file => {
                let reader = new FileReader();
                reader.onload = function (e) {
                    let mediaElement;
                    if (file.type.startsWith('video/')) {
                        mediaElement = document.createElement('video');
                        mediaElement.src = e.target.result;
                        mediaElement.muted = true;
                    } else {
                        mediaElement = document.createElement('img');
                        mediaElement.src = e.target.result;
                    }
                    mediaElement.className = "rm-preview-file border";
                    mediaElement.style.width = "60px";
                    mediaElement.style.height = "60px";
                    mediaElement.style.objectFit = "cover";
                    mediaElement.style.borderRadius = "5px";
                    mediaElement.style.marginRight = "5px";
                    previewContainer.appendChild(mediaElement);
                };
                reader.readAsDataURL(file);
            });
        });
    }
});

// Hàm mở Modal chính
function openRateModal(pId, oId, pName, pImg, rating = 0, comment = '', media = '') {
    // Gán dữ liệu cơ bản
    document.getElementById('rmProduct').value = pId;
    document.getElementById('rmOrder').value = oId;
    document.getElementById('rmOrderIdDisplay').innerText = oId;
    document.getElementById('rmName').innerText = pName;
    document.getElementById('rmImg').src = pImg && pImg.trim() !== "" ? pImg : "https://dummyimage.com/100x100/eee/aaa";

    const ratingInput = document.getElementById('rmRating');
    const commentInput = document.querySelector('textarea[name="comment"]');
    const fileInputArea = document.querySelector('.rm-upload-area');
    const submitBtn = document.querySelector('.rm-submit-btn');
    const ratingText = document.getElementById('rmRatingText');
    const stars = document.querySelectorAll('.rm-star');
    const previewContainer = document.getElementById('rmPreview');
    const closeBtn = document.querySelector('.rm-cancel-btn');

    // Reset giao diện
    previewContainer.innerHTML = "";
    document.getElementById('rmFile').value = "";

    // === CHẾ ĐỘ XEM LẠI (READ-ONLY) ===
    if (rating > 0) {
        ratingInput.value = rating;
        commentInput.value = comment;

        // Hiển thị text đánh giá
        const texts = ["Rất tệ", "Tệ", "Bình thường", "Tốt", "Tuyệt vời"];
        ratingText.innerText = texts[rating - 1] + " (Đã đánh giá)";

        // Hiển thị sao cứng
        stars.forEach(s => {
            let val = s.getAttribute('data-val');
            if (val <= rating) s.classList.add('active');
            else s.classList.remove('active');
            s.style.pointerEvents = 'none';
        });

        // Hiển thị Media cũ (SỬA LỖI TẠI ĐÂY)
        if (media && media.trim() !== "") {
            fileInputArea.style.display = 'none';
            const urls = media.split(';');

            urls.forEach(url => {
                let mediaEl;
                // Kiểm tra đuôi file (không phân biệt hoa thường)
                if (url.toLowerCase().match(/\.(mp4|mov|webm|ogg)$/)) {
                    mediaEl = document.createElement('video');
                    mediaEl.src = url;
                    mediaEl.controls = true; // Hiện thanh điều khiển để xem
                } else {
                    mediaEl = document.createElement('img');
                    mediaEl.src = url;
                    mediaEl.onclick = () => window.open(url);
                    mediaEl.style.cursor = "pointer";
                }

                mediaEl.className = "rm-preview-file border";
                mediaEl.style.width = "70px";
                mediaEl.style.height = "70px";
                mediaEl.style.objectFit = "cover";
                mediaEl.style.marginRight = "10px";
                mediaEl.style.borderRadius = "8px";
                previewContainer.appendChild(mediaEl);
            });
        } else {
            fileInputArea.style.display = 'none';
            previewContainer.innerHTML = '<span class="text-muted small">Không có hình ảnh/video</span>';
        }

        // Khóa form
        commentInput.readOnly = true;
        commentInput.style.backgroundColor = "#f2f2f2";
        submitBtn.style.display = 'none';
        closeBtn.innerText = "Đóng";
    }
    // === CHẾ ĐỘ VIẾT MỚI ===
    else {
        ratingInput.value = 0;
        commentInput.value = "";
        ratingText.innerText = "";

        commentInput.readOnly = false;
        commentInput.style.backgroundColor = "#fff";
        fileInputArea.style.display = 'flex';
        submitBtn.style.display = 'block';
        closeBtn.innerText = "Hủy";

        stars.forEach(s => {
            s.classList.remove('active');
            s.style.pointerEvents = 'auto';
        });
    }

    document.getElementById('rateModal').style.display = 'flex';
}

function closeRateModal() {
    document.getElementById('rateModal').style.display = 'none';
}

function submitReview() {
    const rating = document.getElementById('rmRating').value;
    if (rating == 0) {
        alert("Vui lòng chọn số sao đánh giá!");
        return;
    }

    let form = document.getElementById('reviewForm');
    let formData = new FormData(form);
    const submitBtn = document.querySelector('.rm-submit-btn');

    submitBtn.innerHTML = "Đang gửi...";
    submitBtn.disabled = true;

    fetch('/Review/SubmitReview', {
        method: 'POST',
        body: formData
    })
        .then(res => res.json())
        .then(data => {
            if (data.success || data.exist) {
                closeRateModal();
                location.reload();
            } else {
                alert(data.message);
            }
        })
        .catch(err => {
            console.error(err);
            alert("Lỗi kết nối.");
        })
        .finally(() => {
            submitBtn.innerHTML = 'Gửi đánh giá <i class="bi bi-arrow-right"></i>';
            submitBtn.disabled = false;
        });
}

// Đóng modal khi click ra ngoài
window.onclick = function (event) {
    const modal = document.getElementById('rateModal');
    if (event.target == modal) {
        closeRateModal();
    }
}