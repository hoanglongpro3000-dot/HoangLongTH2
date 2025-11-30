using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using HoangLongTH.Models;

namespace HoangLongTH.Controllers
{
    public class CartController : Controller
    {
        private MyStroreEntities1 db = new MyStroreEntities1();

        // =========================
        //  LẤY CUSTOMER ID HIỆN TẠI
        // =========================
        private int GetCurrentCustomerId()
        {
            // 1. Nếu đã cache trong Session thì dùng luôn
            if (Session["CustomerID"] is int cachedId && cachedId > 0)
            {
                return cachedId;
            }

            // 2. Lấy Username từ Session
            var username = Session["Username"] as string;
            if (string.IsNullOrEmpty(username))
                throw new Exception("Vui lòng đăng nhập trước khi sử dụng giỏ hàng.");

            // 3. Tìm Customer theo Username
            var customer = db.Customer.FirstOrDefault(c => c.Username == username);

            // 4. Nếu chưa có Customer thì tạo mới
            if (customer == null)
            {
                customer = new Customer
                {
                    Username = username,
                    CustomerName = username,
                    CustomerPhone = "",
                    CustomerEmail = "",
                    CustomerAddress = ""
                };

                db.Customer.Add(customer);
                db.SaveChanges();
            }

            // 5. Lưu vào Session
            Session["CustomerID"] = customer.CustomerID;

            return customer.CustomerID;
        }

        // Lấy/ tạo Cart hiện tại
        private Cart GetCartService()
        {
            int customerId = GetCurrentCustomerId();

            var cart = db.Cart
                .Include("CartItem.Product")
                .FirstOrDefault(c => c.CustomerID == customerId
                                  && (c.IsCheckedOut == false || c.IsCheckedOut == null));

            if (cart == null)
            {
                cart = new Cart
                {
                    CustomerID = customerId,
                    CreatedAt = DateTime.Now,
                    IsCheckedOut = false
                };

                db.Cart.Add(cart);
                db.SaveChanges();
            }

            return cart;
        }

        // ================= GIỎ HÀNG =================

        // GET: /Cart
        public ActionResult Index()
        {
            try
            {
                var cart = GetCartService();
                db.Entry(cart).Collection(c => c.CartItem).Load();

                foreach (var ci in cart.CartItem)
                {
                    db.Entry(ci).Reference(x => x.Product).Load();
                    if (ci.Product != null)
                        db.Entry(ci.Product).Reference(p => p.Category).Load();
                }

                ViewBag.TotalQuantity = cart.CartItem.Sum(x => x.Quantity);
                ViewBag.TotalPrice = cart.CartItem.Sum(x => x.UnitPrice * x.Quantity);

                return View(cart);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Login", "Account");
            }
        }

        // ================= THÊM SẢN PHẨM =================

        // GET: /Cart/AddItem/5
        // Hỗ trợ ?redirect=checkout cho nút "Mua ngay"
        [AllowAnonymous]
        public ActionResult AddItem(int id, int quantity = 1, string redirect = null)
        {
            // BẮT BUỘC ĐĂNG NHẬP
            if (Session["Username"] == null)
            {
                return new HttpStatusCodeResult(401, "Vui lòng đăng nhập");
            }

            var username = Session["Username"].ToString();

            // Tìm user + customer
            var user = db.User.SingleOrDefault(u => u.Username == username);
            var customer = db.Customer.SingleOrDefault(c => c.Username == username);

            if (user == null || customer == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Không tìm thấy thông tin khách hàng."
                }, JsonRequestBehavior.AllowGet);
            }

            // Tìm product
            var product = db.Product.SingleOrDefault(p => p.ProductID == id);
            if (product == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Không tìm thấy sản phẩm."
                }, JsonRequestBehavior.AllowGet);
            }

            // CHECK tồn kho
            var stock = product.Quantity ?? 0;   // nếu null coi như 0
            if (stock <= 0)
            {
                return Json(new
                {
                    success = false,
                    message = "Sản phẩm đã hết hàng, không thể thêm vào giỏ."
                }, JsonRequestBehavior.AllowGet);
            }

            // ===== TÌM HOẶC TẠO CART CHƯA CHECKOUT =====
            var cart = db.Cart.FirstOrDefault(c =>
                c.CustomerID == customer.CustomerID && c.IsCheckedOut == false);

            if (cart == null)
            {
                cart = new Cart
                {
                    CustomerID = customer.CustomerID,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    IsCheckedOut = false
                };
                db.Cart.Add(cart);
                db.SaveChanges(); // để có CartID
            }
            else
            {
                cart.UpdatedAt = DateTime.Now;
            }

            // ===== TÌM HOẶC TẠO CARTITEM =====
            var cartItem = db.CartItem.FirstOrDefault(ci =>
                ci.CartID == cart.CartID && ci.ProductID == product.ProductID);

            if (cartItem == null)
            {
                var addQty = Math.Min(quantity, stock);
                if (addQty <= 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Sản phẩm đã hết hàng, không thể thêm vào giỏ."
                    }, JsonRequestBehavior.AllowGet);
                }

                cartItem = new CartItem
                {
                    CartID = cart.CartID,
                    ProductID = product.ProductID,
                    Quantity = addQty,
                    UnitPrice = product.ProductPrice,
                    AddedAt = DateTime.Now
                };
                db.CartItem.Add(cartItem);
            }
            else
            {
                // Cộng thêm nhưng không vượt tồn kho
                var newQty = cartItem.Quantity + quantity;
                if (newQty > stock) newQty = stock;

                if (newQty <= 0)
                {
                    db.CartItem.Remove(cartItem);
                }
                else
                {
                    cartItem.Quantity = newQty;
                    cartItem.UnitPrice = product.ProductPrice;
                }
            }

            db.SaveChanges();

            // Tổng số lượng trong giỏ để update badge
            var totalItems = db.CartItem
                .Where(ci => ci.CartID == cart.CartID)
                .Sum(ci => (int?)ci.Quantity) ?? 0;

            // ====== TRƯỜNG HỢP "MUA NGAY" ======
            if (!string.IsNullOrEmpty(redirect) &&
                redirect.Equals("checkout", StringComparison.OrdinalIgnoreCase))
            {
                // Thêm xong chuyển thẳng sang trang thanh toán
                return RedirectToAction("Checkout", "Order");
            }

            // Mặc định: trả JSON cho nút "Thêm giỏ"
            return Json(new
            {
                success = true,
                count = totalItems
            }, JsonRequestBehavior.AllowGet);
        }

        // ================= XOÁ / CLEAR =================

        // GET: /Cart/RemoveItem/5
        public ActionResult RemoveItem(int id)
        {
            try
            {
                var cart = GetCartService();
                db.Entry(cart).Collection(c => c.CartItem).Load();

                var item = cart.CartItem.FirstOrDefault(x => x.ProductID == id);
                if (item != null)
                {
                    db.CartItem.Remove(item);
                    cart.UpdatedAt = DateTime.Now;
                    db.SaveChanges();
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Login", "Account");
            }
        }

        // GET: /Cart/ClearCart
        public ActionResult ClearCart()
        {
            try
            {
                if (Session["Username"] == null)
                    return RedirectToAction("Login", "Account");

                var username = Session["Username"].ToString();

                var customer = db.Customer.FirstOrDefault(c => c.Username == username);
                if (customer == null)
                    return RedirectToAction("Index");

                // Lấy TẤT CẢ cart chưa checkout của customer
                var carts = db.Cart
                    .Where(c => c.CustomerID == customer.CustomerID && c.IsCheckedOut == false)
                    .ToList();

                if (carts.Any())
                {
                    // Lấy hết CartItem của các cart đó
                    var cartIds = carts.Select(c => c.CartID).ToList();
                    var items = db.CartItem.Where(ci => cartIds.Contains(ci.CartID)).ToList();

                    if (items.Any())
                    {
                        db.CartItem.RemoveRange(items);
                    }

                    foreach (var c in carts)
                    {
                        c.UpdatedAt = DateTime.Now;
                    }

                    db.SaveChanges();
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Login", "Account");
            }
        }

        // ================= UPDATE QUANTITY (AJAX) =================

        [HttpPost]
        public JsonResult UpdateQuantity(int id, int quantity)
        {
            try
            {
                var cart = GetCartService();
                db.Entry(cart).Collection(c => c.CartItem).Load();

                var item = cart.CartItem.FirstOrDefault(x => x.ProductID == id);
                if (item != null)
                {
                    if (quantity <= 0)
                    {
                        db.CartItem.Remove(item);
                    }
                    else
                    {
                        item.Quantity = quantity;
                    }

                    cart.UpdatedAt = DateTime.Now;
                    db.SaveChanges();
                }

                var totalQuantity = cart.CartItem.Sum(x => x.Quantity);
                var totalPrice = cart.CartItem.Sum(x => x.Quantity * x.UnitPrice);
                var itemSubtotal = item != null ? item.Quantity * item.UnitPrice : 0;

                return Json(new
                {
                    success = true,
                    totalQuantity,
                    totalPrice,
                    itemSubtotal
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ================= SỐ LƯỢNG GIỎ HÀNG (HEADER) =================

        // GET: /Cart/GetCartCount
        public JsonResult GetCartCount()
        {
            try
            {
                var cart = GetCartService();
                db.Entry(cart).Collection(c => c.CartItem).Load();

                var count = cart.CartItem.Sum(x => x.Quantity);

                return Json(new { count = count }, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(new { count = 0 }, JsonRequestBehavior.AllowGet);
            }
        }

        // ================= DISPOSE =================
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();
            base.Dispose(disposing);
        }
    }
}
