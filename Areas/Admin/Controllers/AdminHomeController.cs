using System;
using System.Linq;
using System.Web.Mvc;
using HoangLongTH.Models;

namespace HoangLongTH.Areas.Admin.Controllers
{
    public class AdminHomeController : Controller
    {
        private readonly MyStroreEntities1 db = new MyStroreEntities1();

        // GET: Admin/AdminHome
        public ActionResult Index()
        {
            // ===== Thống kê cơ bản =====
            ViewBag.TotalCustomers = db.Customer.Count();   // Số khách hàng
            ViewBag.TotalOrders = db.Order.Count();      // Số đơn hàng
            ViewBag.TotalProducts = db.Product.Count();    // Số sản phẩm
            ViewBag.TotalCategories = db.Category.Count();   // Số danh mục

            // ===== Tài khoản Admin (UserRole = "A") =====
            ViewBag.TotalAdmins = db.User
                .Count(u => u.UserRole != null && u.UserRole.Trim() == "A");

            // ===== Đơn hàng theo PaymentStatus =====
            // Quy ước chung:
            // - PaymentStatus null hoặc rỗng  => đơn chưa thanh toán / chờ xử lý
            // - PaymentStatus khác rỗng      => đơn đã xử lý / có trạng thái cụ thể
            ViewBag.PendingOrders = db.Order.Count(o =>
                o.PaymentStatus == null ||
                o.PaymentStatus.Trim() == ""
            );

            ViewBag.CompletedOrders = db.Order.Count(o =>
                o.PaymentStatus != null &&
                o.PaymentStatus.Trim() != ""
            );

            return View();
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
