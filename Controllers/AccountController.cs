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
    public class AccountController : Controller
    {
        private MyStroreEntities db = new MyStroreEntities();

        // ========== HASH MẬT KHẨU ==========
        private string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return "";

            using (var sha = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha.ComputeHash(bytes);
                // Dùng Base64 -> 44 ký tự, < 50, hợp constraint
                return Convert.ToBase64String(hash);
            }
        }

        // ========== ĐĂNG KÝ ==========

        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(string Username, string Password, string ConfirmPassword)
        {
            // Validate
            if (string.IsNullOrWhiteSpace(Username))
                ModelState.AddModelError("Username", "Vui lòng nhập tên tài khoản.");

            if (string.IsNullOrWhiteSpace(Password))
                ModelState.AddModelError("Password", "Vui lòng nhập mật khẩu.");

            if (Password != ConfirmPassword)
                ModelState.AddModelError("ConfirmPassword", "Mật khẩu nhập lại không khớp.");

            if (db.User.Any(u => u.Username == Username))
                ModelState.AddModelError("Username", "Tên tài khoản đã tồn tại.");

            if (!ModelState.IsValid)
                return View();

            Username = Username.Trim();

            // Tạo User
            var user = new User
            {
                Username = Username,
                Password = HashPassword(Password),
                UserRole = "C"   // C = Customer, đúng max length = 1
            };
            db.User.Add(user);

            // Tạo Customer gắn với User.Username
            var customer = new Customer
            {
                Username = Username,
                CustomerName = Username,
                CustomerPhone = "",
                CustomerEmail = "",
                CustomerAddress = ""
            };
            db.Customer.Add(customer);

            try
            {
                db.SaveChanges();

                // Auto login
                Session["Username"] = user.Username;
                Session["UserRole"] = user.UserRole;

                // Vào trang khách hàng
                return RedirectToAction("Index", "Customer");
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                foreach (var eve in ex.EntityValidationErrors)
                    foreach (var ve in eve.ValidationErrors)
                        ModelState.AddModelError("", ve.PropertyName + ": " + ve.ErrorMessage);

                return View();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi hệ thống: " + ex.Message);
                return View();
            }
        }

        // ========== ĐĂNG NHẬP ==========

        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string Username, string Password)
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ModelState.AddModelError("", "Vui lòng nhập đầy đủ tài khoản và mật khẩu.");
                return View();
            }

            var hash = HashPassword(Password);
            var user = db.User.FirstOrDefault(u => u.Username == Username && u.Password == hash);

            if (user == null)
            {
                ModelState.AddModelError("", "Sai tài khoản hoặc mật khẩu.");
                return View();
            }

            Session["Username"] = user.Username;
            Session["UserRole"] = user.UserRole;

            return RedirectToAction("Index", "Customer");
        }

        // ========== ĐĂNG XUẤT ==========

        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}