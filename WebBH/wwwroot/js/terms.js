document.addEventListener("DOMContentLoaded", function () {
    const sections = Array.from(document.querySelectorAll(".terms-content h4"));
    const navLinks = document.querySelectorAll(".terms-nav .nav-link");

    function updateActiveMenu() {
        let scrollY = window.pageYOffset;
        let current = "";

        // vạch kích hoạt (cách đỉnh 160px để né Navbar)
        const triggerPoint = scrollY + 160;

        // 1. KIỂM TRA CHẠM ĐÁY: Nếu lướt kịch sàn thì sáng mục cuối cùng ngay
        const isAtBottom = (window.innerHeight + scrollY) >= document.documentElement.scrollHeight - 20;

        if (isAtBottom) {
            current = sections[sections.length - 1].getAttribute("id");
        } else {
            // 2. QUÉT NGƯỢC: Tìm mục gần nhất phía trên vạch kích hoạt
            // Cách này giúp nhận diện chính xác kể cả khi mục đó rất ngắn
            for (let i = sections.length - 1; i >= 0; i--) {
                if (triggerPoint >= sections[i].offsetTop) {
                    current = sections[i].getAttribute("id");
                    break;
                }
            }
        }

        // 3. Cập nhật UI
        navLinks.forEach(link => {
            link.classList.remove("active");
            const href = link.getAttribute("href").replace("#", "");
            if (current === href) {
                link.classList.add("active");
            }
        });
    }

    window.addEventListener("scroll", updateActiveMenu);
    updateActiveMenu(); // Chạy ngay khi load
});