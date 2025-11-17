using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using HoangLongTH.Models;

namespace HoangLongTH.Controllers
{
    public class CartController : Controller
    {
        private MyStroreEntities db = new MyStroreEntities();

        private int GetCurrentCustomerId()
        {
            var username = Session["Username"] as string;
            if (string.IsNullOrEmpty(username))
                throw new Exception("Vui lòng đăng nhập trước khi sử dụng giỏ hàng.");

            var customer = db.Customer.FirstOrDefault(c => c.Username == username);
            if (customer == null)
                throw new Exception("Không tìm thấy thông tin khách hàng.");

            return customer.CustomerID;
        }

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

        // GET: /Cart/AddItem/5?quantity=1&redirect=checkout
        public ActionResult AddItem(int id, int quantity = 1, string redirect = null)
        {
            try
            {
                if (quantity <= 0) quantity = 1;

                var product = db.Product.Find(id);
                if (product == null) return HttpNotFound();

                var cart = GetCartService();
                db.Entry(cart).Collection(c => c.CartItem).Load();

                var item = cart.CartItem.FirstOrDefault(x => x.ProductID == id);
                if (item == null)
                {
                    item = new CartItem
                    {
                        CartID = cart.CartID,
                        ProductID = product.ProductID,
                        Quantity = quantity,
                        UnitPrice = product.ProductPrice
                    };
                    db.CartItem.Add(item);
                }
                else
                {
                    item.Quantity += quantity;
                }

                cart.UpdatedAt = DateTime.Now;
                db.SaveChanges();

                // Kiểm tra nếu có yêu cầu redirect sang checkout (cho nút "Mua ngay")
                if (!string.IsNullOrEmpty(redirect) && redirect.ToLower() == "checkout")
                {
                    return RedirectToAction("Checkout", "Order");
                }

                // Mặc định: quay lại trang trước hoặc về giỏ hàng
                var back = Request.UrlReferrer != null
                    ? Request.UrlReferrer.ToString()
                    : Url.Action("Index", "Cart");

                return Redirect(back);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Login", "Account");
            }
        }

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
                var cart = GetCartService();
                db.Entry(cart).Collection(c => c.CartItem).Load();

                if (cart.CartItem.Any())
                {
                    db.CartItem.RemoveRange(cart.CartItem);
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

        // POST: /Cart/UpdateQuantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateQuantity(int id, int quantity)
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

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Login", "Account");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
        // Đã được thêm vào artifact "cart_controller_updated"
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
    }
}