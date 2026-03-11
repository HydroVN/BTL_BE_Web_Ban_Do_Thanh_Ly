using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBH.Data;
using WebBH.Models;

namespace WebBH.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")] // Bắt buộc phải là Admin
    public class CategoriesController : Controller
    {
        private readonly WebThanhLyDbContext _context;

        public CategoriesController(WebThanhLyDbContext context)
        {
            _context = context;
        }

        // --- 1. HIỂN THỊ DANH SÁCH DANH MỤC ---
        public async Task<IActionResult> Index()
        {
            // Lấy danh sách danh mục kèm theo số lượng sản phẩm của mỗi danh mục (để hiển thị)
            var categories = await _context.Categories
                                           .Include(c => c.Products)
                                           .ToListAsync();
            return View(categories);
        }

        // --- 2. LẤY DATA 1 DANH MỤC (Đổ dữ liệu lên Modal Sửa) ---
        [HttpGet]
        public async Task<IActionResult> GetCategory(int id)
        {
            if (id == 0) return Json(new { categoryId = 0, name = "", description = "" });

            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            return Json(category);
        }

        // --- 3. LƯU DANH MỤC (THÊM MỚI HOẶC CẬP NHẬT) ---
        [HttpPost]
        public async Task<IActionResult> SaveCategory([FromForm] Category model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                return Json(new { success = false, message = "Tên danh mục không được để trống!" });
            }

            if (model.CategoryId == 0) // Thêm mới
            {
                _context.Categories.Add(model);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Thêm danh mục thành công!" });
            }
            else // Cập nhật
            {
                var cat = await _context.Categories.FindAsync(model.CategoryId);
                if (cat == null) return Json(new { success = false, message = "Không tìm thấy danh mục!" });

                cat.Name = model.Name;
                cat.Description = model.Description;

                _context.Categories.Update(cat);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Cập nhật danh mục thành công!" });
            }
        }

        // --- 4. XÓA DANH MỤC ---
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
                return Json(new { success = false, message = "Không tìm thấy danh mục!" });

            // Kiểm tra xem danh mục này có đang chứa sản phẩm nào không
            if (category.Products.Any())
            {
                return Json(new { success = false, message = "Không thể xóa! Danh mục này đang chứa sản phẩm." });
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Xóa danh mục thành công!" });
        }
    }
}