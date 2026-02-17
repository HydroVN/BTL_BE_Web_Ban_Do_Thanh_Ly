using System.Collections.Generic;

namespace WebBH.Areas.Admin.Models
{
    // BẮT BUỘC PHẢI CÓ CLASS NÀY ĐỂ CHỨA DỮ LIỆU SẢN PHẨM BÁN CHẠY
    public class TopProductVM
    {
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public int TotalSold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    // Class tổng để gom mọi thứ đẩy ra ngoài View
    public class DashboardVM
    {
        public int TotalUsers { get; set; }
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }

        // Mảng chứa doanh thu 12 tháng để vẽ biểu đồ
        public List<decimal> RevenueByMonth { get; set; } = new List<decimal>();

        // Danh sách Top 5 sản phẩm bán chạy (sử dụng class TopProductVM ở trên)
        public List<TopProductVM> TopProducts { get; set; } = new List<TopProductVM>();
    }
}