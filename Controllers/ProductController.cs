using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using HoangLongTH.Models;
using System.Data.Entity;

namespace HoangLongTH.Controllers
{
    public class ProductController : Controller
    {
        private readonly MyStroreEntities db = new MyStroreEntities(); // đổi tên nếu context khác
        public ActionResult Index()
        {
            var products = db.Product.Include(p => p.Category).ToList();
            return View(products);
        }
        // GET: /Product/Details/5
        public ActionResult Details(int id)
        {
            // Eager load Category
            var product = db.Set<Product>()
                            .Include(p => p.Category)          // nếu báo lỗi compile, đổi sang .Include("Category")
                            .FirstOrDefault(p => p.ProductID == id);

            if (product == null)
                return HttpNotFound();

            // (Tuỳ chọn) Gợi ý sản phẩm liên quan cùng danh mục
            ViewBag.Related = db.Set<Product>()
                                .Where(x => x.CategoryID == product.CategoryID && x.ProductID != id)
                                .OrderByDescending(x => x.ProductID)
                                .Take(8)
                                .ToList();

            return View(product); // View mạnh kiểu Product
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}