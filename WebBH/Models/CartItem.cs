using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebBH.Models;

[Index("UserId", "ProductId", "Size", "Color", Name = "UQ_User_Product_Variant", IsUnique = true)]
public partial class CartItem
{
    [Key]
    public int CartItemId { get; set; }

    public int UserId { get; set; }

    public int ProductId { get; set; }

    [StringLength(50)]
    public string? Size { get; set; }

    [StringLength(50)]
    public string? Color { get; set; }

    public int Quantity { get; set; }

    [ForeignKey("ProductId")]
    [InverseProperty("CartItems")]
    public virtual Product Product { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("CartItems")]
    public virtual User User { get; set; } = null!;
}
