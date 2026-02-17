using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBH.Data;
using WebBH.Models;

namespace WebBH.Controllers
{
    public class ProductController : Controller
    {
        private readonly WebThanhLyDbContext _context;

        public ProductController(WebThanhLyDbContext context)
        {
            _context = context;
        }

        // GET: /Product
        public async Task<IActionResult> Index(int? categoryId, string sortOrder)
        {
            // 1. Query cơ bản
            var products = _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductVariants) // Load biến thể nếu cần hiển thị size/màu
                .AsQueryable();

            // 2. Chỉ lấy sản phẩm ACTIVE
            products = products.Where(p => p.IsActive == true);

            // 3. Lọc theo Category (Nếu có chọn)
            if (categoryId.HasValue)
            {
                products = products.Where(p => p.CategoryId == categoryId);
            }

            // 4. Sắp xếp
            ViewBag.CurrentSort = sortOrder; // Giữ lại giá trị sort để bind vào View
            switch (sortOrder)
            {
                case "price_asc":
                    products = products.OrderBy(p => p.Price);
                    break;
                case "price_desc":
                    products = products.OrderByDescending(p => p.Price);
                    break;
                default: // Mặc định: Mới nhất
                    products = products.OrderByDescending(p => p.ProductId); // Hoặc CreatedAt
                    break;
            }

            // 5. Lấy danh sách Category để hiển thị Filter bên trái
            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.CurrentCategory = categoryId;

            return View(await products.ToListAsync());
        }

        // GET: /Product/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductVariants)
                // QUAN TRỌNG: Nạp danh sách đánh giá và thông tin người dùng đánh giá
                .Include(p => p.Reviews.OrderByDescending(r => r.CreatedAt))
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(m => m.ProductId == id);

            if (product == null || product.IsActive != true)
            {
                return NotFound();
            }

            return View(product);
        }
    }
}