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
        private readonly MyStroreEntities1 db = new MyStroreEntities1();

        /// <summary>
        /// Lấy CustomerID từ Session["Username"] và map sang bảng Customer.
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
        /// Lấy giỏ hàng chưa checkout (IsCheckedOut == false/null)
        /// </summary>
        private Cart GetOpenCart(int customerId)
        {
            return db.Cart
                     .Include("CartItem.Product")
                     .Where(c => c.CustomerID == customerId && (c.IsCheckedOut == false || c.IsCheckedOut == null))
                     .OrderByDescending(c => c.CreatedAt)
                     .FirstOrDefault();
        }

        /// <summary>
        /// ✅ FIX: Tạo Cart sạch không có Entity Framework Proxy
        /// </summary>
        private Cart CreateCleanCart(Cart cart)
        {
            if (cart == null) return null;

            return new Cart
            {
                CartID = cart.CartID,
                CustomerID = cart.CustomerID,
                CreatedAt = cart.CreatedAt,
                UpdatedAt = cart.UpdatedAt,
                IsCheckedOut = cart.IsCheckedOut,
                CartItem = cart.CartItem?.Select(ci => new CartItem
                {
                    CartItemID = ci.CartItemID,
                    CartID = ci.CartID,
                    ProductID = ci.ProductID,
                    Quantity = ci.Quantity,
                    UnitPrice = ci.UnitPrice,
                    Product = ci.Product == null ? null : new Product
                    {
                        ProductID = ci.Product.ProductID,
                        ProductName = ci.Product.ProductName,
                        ProductPrice = ci.Product.ProductPrice,
                        ProductImage = ci.Product.ProductImage,
                        Quantity = ci.Product.Quantity,
                        CategoryID = ci.Product.CategoryID
                    }
                }).ToList()
            };
        }

        // ====================== CHECKOUT ======================

        public ActionResult Checkout()
        {
            try
            {
                int customerId = GetCurrentCustomerId();
                var cart = GetOpenCart(customerId);

                if (cart == null || cart.CartItem == null || !cart.CartItem.Any())
                {
                    TempData["CartMessage"] = "Giỏ hàng trống. Vui lòng thêm sản phẩm trước khi thanh toán.";
                    return RedirectToAction("Index", "Cart");
                }

                // ✅ Trả về Cart sạch không có proxy
                var cleanCart = CreateCleanCart(cart);
                return View(cleanCart);
            }
            catch (Exception ex)
            {
                TempData["AuthMessage"] = ex.Message;
                return RedirectToAction("Login", "Account");
            }
        }

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
                // ✅ Trả về cart sạch
                return View(CreateCleanCart(cart));
            }

            using (var tran = db.Database.BeginTransaction())
            {
                try
                {
                    decimal total = 0m;
                    foreach (var i in cart.CartItem)
                    {
                        var unit = i.UnitPrice != 0 ? i.UnitPrice : i.Product.ProductPrice;
                        total += unit * i.Quantity;
                    }

                    // ✅ Lấy giờ Việt Nam (UTC+7)
                    DateTime utcNow = DateTime.UtcNow;
                    DateTime vnTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

                    var order = new Order
                    {
                        CustomerID = customerId,
                        OrderDate = vnTime,
                        TotalAmount = total,
                        PaymentStatus = (paymentMethod ?? "COD").ToUpper() == "PAID" ? "Đã thanh toán" : "Chưa thanh toán",
                        AddressDelivery = addressDelivery
                    };

                    db.Order.Add(order);
                    db.SaveChanges();

                    // Thêm chi tiết đơn hàng
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
                    }

                    // Đánh dấu giỏ hàng đã checkout
                    cart.IsCheckedOut = true;
                    cart.UpdatedAt = vnTime;

                    db.SaveChanges();
                    tran.Commit();

                    return RedirectToAction("OrderSuccess", new { id = order.OrderID });
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    ModelState.AddModelError("", "Có lỗi khi tạo đơn hàng: " + ex.Message);
                    // ✅ Trả về cart sạch
                    return View(CreateCleanCart(cart));
                }
            }
        }

        // ====================== TRANG THÀNH CÔNG ======================

        public ActionResult OrderSuccess(int id)
        {
            try
            {
                int customerId = GetCurrentCustomerId();
                var order = db.Order
                              .Include(o => o.Customer)
                              .FirstOrDefault(o => o.OrderID == id && o.CustomerID == customerId);

                if (order == null)
                    return HttpNotFound();

                return View(order);
            }
            catch
            {
                // Cho xem nếu session hết
                var safe = db.Order.FirstOrDefault(o => o.OrderID == id);
                if (safe == null) return HttpNotFound();
                return View(safe);
            }
        }

        // ====================== LỊCH SỬ ĐƠN HÀNG ======================

        public ActionResult MyOrders(int page = 1, int pageSize = 10)
        {
            try
            {
                int customerId = GetCurrentCustomerId();

                var query = db.Order
                              .Where(o => o.CustomerID == customerId)
                              .OrderByDescending(o => o.OrderDate);

                var total = query.Count();
                var orders = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                ViewBag.Total = total;
                ViewBag.Page = page;
                ViewBag.PageSize = pageSize;

                return View(orders);
            }
            catch (Exception ex)
            {
                TempData["AuthMessage"] = ex.Message;
                return RedirectToAction("Login", "Account");
            }
        }

        // ====================== CHI TIẾT ĐƠN HÀNG ======================

        public ActionResult Details(int id)
        {
            try
            {
                int customerId = GetCurrentCustomerId();

                var order = db.Order
                              .Include(o => o.Customer)
                              .Include(o => o.OrderDetail.Select(od => od.Product))
                              .FirstOrDefault(o => o.OrderID == id && o.CustomerID == customerId);

                if (order == null) return HttpNotFound();

                return View(order);
            }
            catch (Exception ex)
            {
                TempData["AuthMessage"] = ex.Message;
                return RedirectToAction("Login", "Account");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();
            base.Dispose(disposing);
        }
    }
}