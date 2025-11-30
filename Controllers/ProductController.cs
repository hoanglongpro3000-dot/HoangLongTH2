using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using HoangLongTH.Models;

namespace HoangLongTH.Controllers
{
    public class ProductController : Controller
    {
        private readonly MyStroreEntities1 db = new MyStroreEntities1();

        // GET: /Product
        // /Product?categoryId=1&SearchString=iphone
        public ActionResult Index(int? categoryId, string SearchString)
        {
            // Lấy danh sách sản phẩm + kèm Category
            var products = db.Product
                             .Include(p => p.Category)
                             .AsQueryable();

            // Lọc theo danh mục (nếu có)
            if (categoryId.HasValue)
            {
                products = products.Where(p => p.CategoryID == categoryId.Value);

                ViewBag.CurrentCategory = db.Category
                                            .Where(c => c.CategoryID == categoryId.Value)
                                            .Select(c => c.CategoryName)
                                            .FirstOrDefault() ?? "Tất cả sản phẩm";
                ViewBag.CurrentCategoryId = categoryId.Value;
            }
            else
            {
                ViewBag.CurrentCategory = "Tất cả sản phẩm";
                ViewBag.CurrentCategoryId = null;
            }

            // Lưu lại chuỗi tìm kiếm để fill lại ở ô input
            ViewBag.SearchString = SearchString;

            // Nếu có từ khóa thì mới lọc
            if (!string.IsNullOrWhiteSpace(SearchString))
            {
                var keyword = SearchString.Trim().ToLower();

                products = products.Where(p =>
                    (p.ProductName != null &&
                     p.ProductName.ToLower().Contains(keyword)) ||

                    (p.Category != null &&
                     p.Category.CategoryName.ToLower().Contains(keyword))
                );
            }

            return View(products.ToList());
        }

        // GET: /Product/Details/5
        public ActionResult Details(int id)
        {
            var product = db.Set<Product>()
                            .Include(p => p.Category)
                            .FirstOrDefault(p => p.ProductID == id);

            if (product == null)
                return HttpNotFound();

            ViewBag.Related = db.Set<Product>()
                                .Where(x => x.CategoryID == product.CategoryID && x.ProductID != id)
                                .OrderByDescending(x => x.ProductID)
                                .Take(8)
                                .ToList();

            return View(product);
        }

        // MENU DANH MỤC DÙNG CHO HEADER
        [ChildActionOnly]
        public ActionResult CategoryMenu()
        {
            var categories = db.Category
                               .OrderBy(c => c.CategoryName)
                               .ToList();

            return PartialView("_CategoryMenu", categories);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
