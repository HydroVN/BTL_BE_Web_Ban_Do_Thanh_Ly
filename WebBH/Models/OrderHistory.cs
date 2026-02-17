using System;
using System.Collections.Generic;

namespace WebBH.Models
{
    public class OrderHistory
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }

        public string StatusLabel
        {
            get
            {
                return Status switch
                {
                    "Pending" => "Chờ thanh toán",
                    "Processing" => "Đang xử lý",
                    "Shipping" => "Đang giao",
                    "Completed" => "Hoàn thành",
                    "Cancelled" => "Đã hủy",
                    _ => "Không xác định"
                };
            }
        }

        public string StatusClass
        {
            get
            {
                return Status switch
                {
                    "Shipping" => "bg-dark text-white",
                    "Completed" => "bg-success text-white",
                    "Pending" => "bg-light text-dark border",
                    _ => "bg-secondary text-white"
                };
            }
        }

        // ĐỔI TÊN Ở ĐÂY để không trùng với Entity OrderDetail gốc
        public List<OrderHistoryDetail> Items { get; set; } = new List<OrderHistoryDetail>();
    }

    public class OrderHistoryDetail
    {
        public string ProductName { get; set; }
        public string ProductImage { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string Description { get; set; }
    }
}