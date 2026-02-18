using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
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
                .Include(p => p.Reviews)
                .Where(p => p.IsActive == true)
                .AsQueryable();

            if (categoryId.HasValue)
            {
                products = products.Where(p => p.CategoryId == categoryId);
            }

            ViewBag.CurrentSort = sortOrder;
            switch (sortOrder)
            {
                case "price_asc": products = products.OrderBy(p => p.Price); break;
                case "price_desc": products = products.OrderByDescending(p => p.Price); break;
                default: products = products.OrderByDescending(p => p.ProductId); break;
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.CurrentCategory = categoryId;

            // --- LOGIC YÊU THÍCH ---
            var listProducts = await products.ToListAsync();
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                var likedIds = await _context.FavoriteProducts
                    .Where(f => f.UserId == userId)
                    .Select(f => f.ProductId)
                    .ToListAsync();
                ViewBag.LikedProductIds = likedIds;
            }
            else
            {
                ViewBag.LikedProductIds = new List<int>();
            }
            // -----------------------

            return View(listProducts);
        }

        // GET: /Product/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductVariants)
                .Include(p => p.Reviews).ThenInclude(r => r.User)
                .FirstOrDefaultAsync(m => m.ProductId == id);

            if (product == null || product.IsActive != true) return NotFound();

            // Check xem user có thích sản phẩm này không
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewBag.IsLiked = false;
            if (int.TryParse(userIdStr, out int userId))
            {
                ViewBag.IsLiked = await _context.FavoriteProducts
                    .AnyAsync(f => f.UserId == userId && f.ProductId == id);
            }

            return View(product);
        }
    }
}