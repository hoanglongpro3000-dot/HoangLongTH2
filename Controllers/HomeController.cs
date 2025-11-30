using HoangLongTH.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace HoangLongTH.Controllers
{
    public class HomeController : Controller
    {
        private readonly MyStroreEntities1 db = new MyStroreEntities1();

        public ActionResult Index()
        {
            // 1. Sản phẩm mới (hiện 5 cái)
            var newProducts = db.Product
                                .OrderByDescending(p => p.ProductID)
                                .Take(5)
                                .ToList();

            // 2. Sản phẩm bán chạy theo số dòng OrderDetail
            //    (mỗi dòng coi như 1 lượt mua)
            var bestSellerIds = db.OrderDetail
                                  .GroupBy(od => od.ProductID)
                                  .Select(g => new
                                  {
                                      ProductID = g.Key,
                                      OrderCount = g.Count()
                                  })
                                  .OrderByDescending(x => x.OrderCount)
                                  .Take(5)
                                  .ToList();

            var idList = bestSellerIds.Select(x => x.ProductID).ToList();

            var bestSellerProducts = new List<Product>();

            if (idList.Any())
            {
                bestSellerProducts = db.Product
                                       .Where(p => idList.Contains(p.ProductID))
                                       .ToList();
            }

            // 3. Nếu chưa có đơn hàng nào → lấy tạm 5 sản phẩm giá cao làm "nổi bật"
            if (!bestSellerProducts.Any())
            {
                bestSellerProducts = db.Product
                                       .OrderByDescending(p => p.ProductPrice)
                                       .Take(5)
                                       .ToList();
            }

            // 4. Gán vào ViewModel
            var vm = new HomeViewModel
            {
                NewProducts = newProducts,
                BestSellerProducts = bestSellerProducts
            };

            return View(vm);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
