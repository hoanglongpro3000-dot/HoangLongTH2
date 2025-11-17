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
    public class OrderController : Controller
    {
        private readonly MyStroreEntities db = new MyStroreEntities();

        /// <summary>
        /// Lấy CustomerID từ Session["Username"] và map sang bảng Customer.
        /// Ném lỗi khi chưa đăng nhập hoặc không tìm thấy khách.
        /// </summary>
        private int GetCurrentCustomerId()
        {
            var username = Session["Username"] as string;
            if (string.IsNullOrEmpty(username))
                throw new Exception("Vui lòng đăng nhập trước khi đặt hàng.");

            var customer = db.Customer.FirstOrDefault(c => c.Username == username);
            if (customer == null)
                throw new Exception("Không tìm thấy thông tin khách hàng.");

            return customer.CustomerID;
        }

        /// <summary>
        /// Lấy giỏ hàng đang mở (IsCheckedOut == false/null) kèm CartItem + Product.
        /// Nếu có nhiều cart mở vì lỗi dữ liệu, lấy cart mới nhất theo CreatedAt.
        /// </summary>
        private Cart GetOpenCart(int customerId)
        {
            var carts = db.Cart
                          .Include("CartItem.Product")
                          .Where(c => c.CustomerID == customerId && (c.IsCheckedOut == false || c.IsCheckedOut == null));

            // Ưu tiên cart mới nhất
            var cart = carts.OrderByDescending(c => c.CreatedAt).FirstOrDefault();
            return cart;
        }

        // ====================== CHECKOUT ======================

        // GET: /Order/Checkout
        public ActionResult Checkout()
        {
            int customerId;
            try
            {
                customerId = GetCurrentCustomerId();
            }
            catch (Exception ex)
            {
                TempData["AuthMessage"] = ex.Message;
                return RedirectToAction("Login", "Account");
            }

            var cart = GetOpenCart(customerId);
            if (cart == null || cart.CartItem == null || !cart.CartItem.Any())
            {
                TempData["CartMessage"] = "Giỏ hàng trống. Vui lòng thêm sản phẩm trước khi thanh toán.";
                return RedirectToAction("Index", "Cart");
            }

            return View(cart); // View mạnh kiểu Cart
        }

        // POST: /Order/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Checkout(string addressDelivery, string paymentMethod = "COD", string note = null)
        {
            int customerId;
            try
            {
                customerId = GetCurrentCustomerId();
            }
            catch (Exception ex)
            {
                TempData["AuthMessage"] = ex.Message;
                return RedirectToAction("Login", "Account");
            }

            var cart = GetOpenCart(customerId);
            if (cart == null || cart.CartItem == null || !cart.CartItem.Any())
            {
                TempData["CartMessage"] = "Giỏ hàng trống. Vui lòng thêm sản phẩm trước khi thanh toán.";
                return RedirectToAction("Index", "Cart");
            }

            if (string.IsNullOrWhiteSpace(addressDelivery))
            {
                ModelState.AddModelError("", "Vui lòng nhập địa chỉ giao hàng.");
                return View(cart);
            }

            using (var tran = db.Database.BeginTransaction())
            {
                try
                {
                    // Tính tổng tiền dựa trên UnitPrice đã snapshot tại thời điểm thêm giỏ; fallback ProductPrice
                    decimal total = 0m;
                    foreach (var i in cart.CartItem)
                    {
                        var unit = i.UnitPrice != 0 ? i.UnitPrice : i.Product.ProductPrice;
                        total += unit * i.Quantity;
                    }

                    var order = new Order
                    {
                        CustomerID = customerId,
                        OrderDate = DateTime.Now,
                        TotalAmount = total,
                        PaymentStatus = (paymentMethod ?? "COD").ToUpper() == "PAID" ? "Đã thanh toán" : "Chưa thanh toán",
                        AddressDelivery = addressDelivery
                    };
                    db.Order.Add(order);
                    db.SaveChanges(); // có OrderID

                    // Tạo OrderDetail từ CartItem
                    foreach (var ci in cart.CartItem)
                    {
                        var unit = ci.UnitPrice != 0 ? ci.UnitPrice : ci.Product.ProductPrice;

                        var detail = new OrderDetail
                        {
                            OrderID = order.OrderID,
                            ProductID = ci.ProductID,
                            Quantity = ci.Quantity,
                            UnitPrice = unit
                        };
                        db.OrderDetail.Add(detail);

                        // (Tuỳ chọn) Kiểm tra tồn kho tại đây nếu có cột Stock trong Product
                        // if (ci.Product.Stock < ci.Quantity) throw new Exception("Sản phẩm " + ci.Product.ProductName + " không đủ tồn.");
                        // ci.Product.Stock -= ci.Quantity;
                    }

                    // Đánh dấu cart đã checkout
                    cart.IsCheckedOut = true;
                    cart.UpdatedAt = DateTime.Now;

                    db.SaveChanges();
                    tran.Commit();

                    // (Optional) Xoá sạch item của cart đã checkout nếu bạn muốn
                    // db.CartItem.RemoveRange(cart.CartItem);
                    // db.SaveChanges();

                    return RedirectToAction("OrderSuccess", new { id = order.OrderID });
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    ModelState.AddModelError("", "Có lỗi khi tạo đơn hàng: " + ex.Message);
                    // Nạp lại cart kèm lỗi
                    return View(cart);
                }
            }
        }

        // GET: /Order/OrderSuccess/123
        public ActionResult OrderSuccess(int id)
        {
            int customerId;
            try
            {
                customerId = GetCurrentCustomerId();
            }
            catch (Exception)
            {
                // Cho xem trang thành công ngay cả khi session hết, nhưng không lộ đơn người khác
                var safe = db.Order.FirstOrDefault(o => o.OrderID == id);
                if (safe == null) return HttpNotFound();
                return View(safe);
            }

            var order = db.Order
                          .Include(o => o.Customer)
                          .FirstOrDefault(o => o.OrderID == id && o.CustomerID == customerId);

            if (order == null) return HttpNotFound();
            return View(order);
        }

        // ====================== LỊCH SỬ ĐƠN HÀNG ======================

        // GET: /Order/MyOrders?page=1
        public ActionResult MyOrders(int page = 1, int pageSize = 10)
        {
            int customerId;
            try
            {
                customerId = GetCurrentCustomerId();
            }
            catch (Exception ex)
            {
                TempData["AuthMessage"] = ex.Message;
                return RedirectToAction("Login", "Account");
            }

            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 50) pageSize = 10;

            var query = db.Order
                          .Where(o => o.CustomerID == customerId)
                          .OrderByDescending(o => o.OrderDate);

            var total = query.Count();
            var orders = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.Total = total;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;

            return View(orders); // View mạnh kiểu List<Order>
        }

        // GET: /Order/Details/123
        public ActionResult Details(int id)
        {
            int customerId;
            try
            {
                customerId = GetCurrentCustomerId();
            }
            catch (Exception ex)
            {
                TempData["AuthMessage"] = ex.Message;
                return RedirectToAction("Login", "Account");
            }

            var order = db.Order
                          .Include(o => o.Customer)
                          .Include(o => o.OrderDetail.Select(od => od.Product))
                          .FirstOrDefault(o => o.OrderID == id && o.CustomerID == customerId);

            if (order == null) return HttpNotFound();
            return View(order); // View mạnh kiểu Order (hiển thị list OrderDetail)
        }

        // ====================== HỦY ĐƠN (tuỳ chọn) ======================
        // Nếu có field Status (Pending/Processing/Shipped/Cancelled) thì nên thêm vào model Order.
        // Ở hiện tại chỉ có PaymentStatus, nên không implement huỷ để tránh mập mờ trạng thái.

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
