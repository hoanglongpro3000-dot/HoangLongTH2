using HoangLongTH.Models;
using PagedList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HoangLongTH.Controllers
{
    public class CategoryController : Controller
    {
        private readonly MyStroreEntities1 db = new MyStroreEntities1();

        // GET: /Category/
        public ActionResult Index(
            string search,
            int? categoryId,
            decimal? minPrice,
            decimal? maxPrice,
            int page = 1,
            int pageSize = 5)
        {
            var products = db.Product
                             .Where(p => p.Quantity > 0);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();
                products = products.Where(p => p.ProductName.Contains(keyword));
            }

            if (categoryId.HasValue)
            {
                products = products.Where(p => p.CategoryID == categoryId.Value);
            }

            if (minPrice.HasValue)
            {
                products = products.Where(p => p.ProductPrice >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                products = products.Where(p => p.ProductPrice <= maxPrice.Value);
            }

            products = products.OrderBy(p => p.ProductName);

            var pagedProducts = products.ToPagedList(page, pageSize);

            ViewBag.CurrentSearch = search;
            ViewBag.CategoryID = categoryId;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.Categories = db.Category.ToList();

            return View(pagedProducts);
        }

        // ===== MENU DANH MỤC CHO HEADER =====
        [ChildActionOnly]
        public PartialViewResult CategoryMenu()
        {
            var cats = db.Category
                         .OrderBy(c => c.CategoryName)
                         .ToList();

            return PartialView("_CategoryMenu", cats);
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
