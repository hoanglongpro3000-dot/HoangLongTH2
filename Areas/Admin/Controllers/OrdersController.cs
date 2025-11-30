using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using HoangLongTH.Models;

namespace HoangLongTH.Areas.Admin.Controllers
{
    public class OrdersController : Controller
    {
        private MyStroreEntities1 db = new MyStroreEntities1();

        // GET: Admin/Orders
        // Thêm tìm kiếm theo mã đơn (OrderID) hoặc tên khách hàng
        public ActionResult Index(string search)
        {
            var orders = db.Order
                           .Include(o => o.Customer)
                           .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                int orderId;
                bool isNumeric = int.TryParse(search, out orderId);

                orders = orders.Where(o =>
                    // Nếu nhập số, thử match theo OrderID
                    (isNumeric && o.OrderID == orderId)
                    // Hoặc tên khách hàng chứa từ khóa
                    || (o.Customer != null && o.Customer.CustomerName.Contains(search))
                );
            }

            ViewBag.Search = search; // để hiển thị lại trong ô tìm kiếm

            return View(orders.ToList());
        }

        // GET: Admin/Orders/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var order = db.Order
                          .Include(o => o.Customer)
                          .Include(o => o.OrderDetail) // nếu navigation khác tên thì sửa lại cho đúng
                          .SingleOrDefault(o => o.OrderID == id);

            if (order == null)
            {
                return HttpNotFound();
            }

            return View(order);
        }

        // GET: Admin/Orders/Create
        // UI của bạn đang ẩn nút Thêm, nhưng để action lại cũng không sao
        public ActionResult Create()
        {
            ViewBag.CustomerID = new SelectList(db.Customer, "CustomerID", "CustomerName");
            return View();
        }

        // POST: Admin/Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "OrderID,CustomerID,OrderDate,TotalAmount,PaymentStatus,AddressDelivery")] Order order)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.CustomerID = new SelectList(db.Customer, "CustomerID", "CustomerName", order.CustomerID);
                return View(order);
            }

            db.Order.Add(order);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // GET: Admin/Orders/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var order = db.Order
                          .Include(o => o.Customer)
                          .SingleOrDefault(o => o.OrderID == id);

            if (order == null)
            {
                return HttpNotFound();
            }

            // View Edit mới chỉ dùng Model, không cần ViewBag.CustomerID
            return View(order);
        }

        // POST: Admin/Orders/Edit/5
        // CHỈ cho phép sửa PaymentStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "OrderID,PaymentStatus")] Order formOrder)
        {
            if (!ModelState.IsValid)
            {
                // Load lại đầy đủ để hiển thị view
                var orderForView = db.Order
                                     .Include(o => o.Customer)
                                     .SingleOrDefault(o => o.OrderID == formOrder.OrderID);
                return View(orderForView);
            }

            var order = db.Order.Find(formOrder.OrderID);
            if (order == null)
            {
                return HttpNotFound();
            }

            // Chuẩn hóa trạng thái thanh toán về 3 trạng thái chính
            if (string.Equals(formOrder.PaymentStatus, "Đã thanh toán", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(formOrder.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase))
            {
                order.PaymentStatus = "Đã thanh toán";
            }
            else if (string.Equals(formOrder.PaymentStatus, "Chờ xử lý", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(formOrder.PaymentStatus, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                order.PaymentStatus = "Chờ xử lý";
            }
            else
            {
                order.PaymentStatus = "Chưa thanh toán";
            }

            db.SaveChanges();
            TempData["Success"] = "Cập nhật trạng thái thanh toán thành công.";
            return RedirectToAction("Index");
        }

        // KHÔNG còn Delete / DeleteConfirmed để tránh xóa đơn hàng của khách

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
