/**
 * Dashboard Chart Logic
 * Xử lý vẽ biểu đồ doanh thu bằng Chart.js cho trang Admin
 */

document.addEventListener("DOMContentLoaded", () => {
    // 1. Tìm thẻ chứa dữ liệu được đẩy từ Backend (đã đặt ở Index.cshtml)
    const dataElement = document.getElementById("revenueDataInput");

    // Nếu không tìm thấy thẻ hoặc canvas thì thoát để tránh lỗi JS
    const ctx = document.getElementById("revenueChart");
    if (!dataElement || !ctx) return;

    try {
        // 2. Phân tích dữ liệu JSON từ value của input
        const monthlyRevenue = JSON.parse(dataElement.value);

        // 3. Cấu hình và vẽ biểu đồ
        new Chart(ctx, {
            type: "line",
            data: {
                labels: ["T1", "T2", "T3", "T4", "T5", "T6", "T7", "T8", "T9", "T10", "T11", "T12"],
                datasets: [{
                    label: "Doanh thu",
                    data: monthlyRevenue,
                    borderColor: "#3b82f6", // Màu xanh dương nổi bật trên nền tối
                    borderWidth: 3,
                    pointBackgroundColor: "#111827", // Khớp với màu nền card dark
                    pointBorderColor: "#3b82f6",
                    pointBorderWidth: 2,
                    pointRadius: 4,
                    pointHoverRadius: 6,
                    tension: 0.4, // Tạo độ cong cho đường biểu đồ
                    fill: true,
                    backgroundColor: "rgba(59, 130, 246, 0.1)" // Màu đổ bóng phía dưới đường
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false // Ẩn chú thích vì đã có tiêu đề card
                    },
                    tooltip: {
                        backgroundColor: "#1f2937",
                        titleColor: "#f3f4f6",
                        bodyColor: "#f3f4f6",
                        displayColors: false,
                        callbacks: {
                            // Định dạng hiển thị tiền tệ VNĐ trong tooltip
                            label: function (context) {
                                let label = context.dataset.label || '';
                                if (label) label += ': ';
                                if (context.parsed.y !== null) {
                                    label += new Intl.NumberFormat('vi-VN', {
                                        style: 'currency',
                                        currency: 'VND'
                                    }).format(context.parsed.y);
                                }
                                return label;
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        grid: {
                            color: "rgba(255, 255, 255, 0.05)",
                            drawBorder: false
                        },
                        ticks: {
                            color: "#9ca3af",
                            // Rút gọn hiển thị số tiền trên trục Y (ví dụ 10M thay vì 10.000.000)
                            callback: function (value) {
                                if (value >= 1000000) return (value / 1000000) + 'M';
                                if (value >= 1000) return (value / 1000) + 'K';
                                return value;
                            }
                        }
                    },
                    x: {
                        grid: {
                            display: false,
                            drawBorder: false
                        },
                        ticks: {
                            color: "#9ca3af"
                        }
                    }
                }
            }
        });
    } catch (error) {
        console.error("Lỗi khi xử lý dữ liệu biểu đồ:", error);
    }
});