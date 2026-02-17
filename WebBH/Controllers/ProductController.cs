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
            var products = _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductVariants)
                .Include(p => p.Reviews) // <--- QUAN TRỌNG: Phải thêm dòng này mới hiện sao
                .Where(p => p.IsActive == true) // Chỉ lấy sản phẩm đang bán
                .AsQueryable();

            // Chỉ lấy sản phẩm ACTIVE (So sánh với true để xử lý trường hợp null)
            products = products.Where(p => p.IsActive == true);

            if (categoryId.HasValue)
            {
                products = products.Where(p => p.CategoryId == categoryId);
            }

            ViewBag.CurrentSort = sortOrder;
            switch (sortOrder)
            {
                case "price_asc":
                    products = products.OrderBy(p => p.Price);
                    break;
                case "price_desc":
                    products = products.OrderByDescending(p => p.Price);
                    break;
                default:
                    products = products.OrderByDescending(p => p.ProductId);
                    break;
            }

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
                .Include(p => p.Reviews) // Chỉ load Reviews, không sort ở đây
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