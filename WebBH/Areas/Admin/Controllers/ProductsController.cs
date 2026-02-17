using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebBH.Data;
using WebBH.Models;
using System.Text.Json;

namespace WebBH.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductsController : Controller
    {
        private readonly WebThanhLyDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductsController(WebThanhLyDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.Categories = new SelectList(_context.Categories, "CategoryId", "Name");

            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductVariants) // Load variants để tính tổng hiển thị
                .OrderByDescending(p => p.ProductId)
                .ToListAsync();

            return View(products);
        }

        [HttpGet]
        public async Task<IActionResult> GetProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.ProductVariants)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return NotFound();

            return Json(new
            {
                productId = product.ProductId,
                name = product.Name,
                categoryId = product.CategoryId,
                price = product.Price,
                description = product.Description,
                isActive = product.IsActive,
                // Trả về danh sách variants
                variants = product.ProductVariants.Select(v => new {
                    v.Size,
                    v.Color,
                    v.Quantity
                }).ToList()
            });
        }

        [HttpPost]
        public async Task<IActionResult> Save(Product product, IFormFile? imageFile, string variantsJson)
        {
            // 1. XỬ LÝ ẢNH
            if (imageFile != null)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
                if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);
                using (var fileStream = new FileStream(Path.Combine(uploadPath, fileName), FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }
                product.ImageUrl = "/images/products/" + fileName;
            }

            // 2. LƯU SẢN PHẨM CHA
            if (product.ProductId == 0)
            {
                product.CreatedAt = DateTime.Now;
                _context.Add(product);
            }
            else
            {
                var existingProduct = await _context.Products.Include(p => p.ProductVariants)
                                            .FirstOrDefaultAsync(p => p.ProductId == product.ProductId);
                if (existingProduct != null)
                {
                    if (imageFile == null) product.ImageUrl = existingProduct.ImageUrl;
                    product.CreatedAt = existingProduct.CreatedAt;

                    _context.Entry(existingProduct).CurrentValues.SetValues(product);

                    // Xóa variants cũ để cập nhật lại
                    _context.ProductVariants.RemoveRange(existingProduct.ProductVariants);
                }
            }

            // 3. LƯU VARIANTS
            if (!string.IsNullOrEmpty(variantsJson))
            {
                try
                {
                    var variants = JsonSerializer.Deserialize<List<ProductVariant>>(variantsJson);
                    if (variants != null)
                    {
                        foreach (var v in variants)
                        {
                            v.ProductId = product.ProductId; // Gán ID cha
                            if (product.ProductId > 0) _context.ProductVariants.Add(v);
                            else product.ProductVariants.Add(v);
                        }
                    }
                }
                catch { /* Ignore JSON error */ }
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }
    }
}