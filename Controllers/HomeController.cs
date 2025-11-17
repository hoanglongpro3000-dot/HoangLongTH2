using HoangLongTH.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;


namespace HoangLongTH.Controllers
{
    public class HomeController : Controller
    {
        private readonly MyStroreEntities db = new MyStroreEntities(); // đổi tên nếu context khác

        public ActionResult Index()
        {
            var products = db.Set<Product>()       // an toàn cho Database-First
                             .OrderByDescending(p => p.ProductID)
                             .Take(20)
                             .ToList();

            return View(products);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}