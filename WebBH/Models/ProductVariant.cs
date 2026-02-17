using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebBH.Models;

[Index("ProductId", Name = "IX_ProductVariants_ProductId")]
// Lưu ý: Index Unique này có thể gây lỗi nếu DB của bạn coi 2 dòng NULL là trùng nhau (tùy phiên bản SQL)
// Nhưng về mặt Code C#, sửa như dưới đây là đúng yêu cầu:
[Index("ProductId", "Size", "Color", Name = "UQ_Product_Size_Color", IsUnique = true)]
public partial class ProductVariant
{
    [Key]
    public int VariantId { get; set; }

    public int ProductId { get; set; }

    [StringLength(50)]
    public string? Size { get; set; } // Đã thêm ? và xóa = null!

    [StringLength(50)]
    public string? Color { get; set; } // Đã thêm ? và xóa = null!

    public int Quantity { get; set; }

    [ForeignKey("ProductId")]
    [InverseProperty("ProductVariants")]
    public virtual Product Product { get; set; } = null!;
}