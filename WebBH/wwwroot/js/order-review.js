document.addEventListener('DOMContentLoaded', function () {
    const stars = document.querySelectorAll('.rm-star');
    const texts = ["Poor quality", "Fair", "Average", "Good", "Excellent quality"];

    stars.forEach(star => {
        star.addEventListener('mouseover', function () {
            let val = this.getAttribute('data-val');
            highlightStars(val);
        });
        star.addEventListener('click', function () {
            let val = this.getAttribute('data-val');
            document.getElementById('rmRating').value = val;
            document.getElementById('rmRatingText').innerText = texts[val - 1];

            stars.forEach(s => {
                if (s.getAttribute('data-val') <= val) s.classList.add('selected');
                else s.classList.remove('selected');
            });
        });
    });

    document.querySelector('.rm-stars').addEventListener('mouseleave', function () {
        let currentRating = document.getElementById('rmRating').value;
        highlightStars(currentRating);
    });

    function highlightStars(val) {
        stars.forEach(s => {
            if (s.getAttribute('data-val') <= val) s.classList.add('active');
            else s.classList.remove('active');
        });
    }

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
                        mediaElement.className = "rm-preview-file";
                        mediaElement.muted = true;
                    } else {
                        mediaElement = document.createElement('img');
                        mediaElement.src = e.target.result;
                        mediaElement.className = "rm-preview-file";
                    }
                    previewContainer.appendChild(mediaElement);
                };
                reader.readAsDataURL(file);
            });
        });
    }
});

function openRateModal(pId, oId, pName, pImg) {
    document.getElementById('rmProduct').value = pId;
    document.getElementById('rmOrder').value = oId;
    document.getElementById('rmOrderIdDisplay').innerText = oId;
    document.getElementById('rmName').innerText = pName;

    const imgElement = document.getElementById('rmImg');
    imgElement.src = pImg && pImg.trim() !== "" ? pImg : "https://dummyimage.com/100x100/eee/aaa";

    document.getElementById('rmRating').value = 0;
    document.getElementById('rmRatingText').innerText = "";
    document.querySelector('textarea[name="comment"]').value = "";
    document.getElementById('rmFile').value = "";
    document.getElementById('rmPreview').innerHTML = "";

    document.querySelectorAll('.rm-star').forEach(s => {
        s.classList.remove('active');
        s.classList.remove('selected');
    });

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
    submitBtn.innerHTML = "Submitting...";
    submitBtn.disabled = true;

    fetch('/Review/SubmitReview', {
        method: 'POST',
        body: formData
    })
        .then(res => res.json())
        .then(data => {
            if (data.success) {
                alert(data.message);
                closeRateModal();
                location.reload();
            } else {
                alert(data.message);
            }
        })
        .catch(err => {
            console.error(err);
            alert("Đã có lỗi xảy ra. Vui lòng thử lại sau.");
        })
        .finally(() => {
            submitBtn.innerHTML = 'Submit Review <i class="bi bi-arrow-right"></i>';
            submitBtn.disabled = false;
        });
}

window.onclick = function (event) {
    const modal = document.getElementById('rateModal');
    if (event.target == modal) {
        closeRateModal();
    }
}