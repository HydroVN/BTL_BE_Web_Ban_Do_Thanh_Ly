document.addEventListener("DOMContentLoaded", function () {
    // Lấy phần tử Carousel
    var myCarouselElement = document.querySelector('#aboutCarousel');

    if (myCarouselElement) {
        // Khởi tạo Carousel với các tùy chọn ép buộc tự động chạy
        var carousel = new bootstrap.Carousel(myCarouselElement, {
            interval: 3000,   // Thời gian chuyển slide (3000ms = 3 giây)
            ride: 'carousel', // Ép tự động trượt
            pause: 'hover',   // Di chuột vào thì dừng, bỏ chuột ra chạy tiếp
            wrap: true        // Lướt đến ảnh cuối sẽ vòng lại ảnh đầu
        });

        // Kích hoạt chạy ngay lập tức
        carousel.cycle();
    }
});