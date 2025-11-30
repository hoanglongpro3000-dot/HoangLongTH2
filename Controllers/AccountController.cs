using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;
using HoangLongTH.Models;

namespace HoangLongTH.Controllers
{
    public class AccountController : Controller
    {
        private MyStroreEntities1 db = new MyStroreEntities1();

        // ========== HASH MẬT KHẨU ==========
        private string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return "";

            using (var sha = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha.ComputeHash(bytes);
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
        public ActionResult Register(string Username, string Email, string SDT,
                                     string Password, string ConfirmPassword)
        {
            // ----- VALIDATE CƠ BẢN -----
            if (string.IsNullOrWhiteSpace(Username))
                ModelState.AddModelError("Username", "Vui lòng nhập tên tài khoản.");

            if (string.IsNullOrWhiteSpace(Email))
                ModelState.AddModelError("Email", "Vui lòng nhập email.");

            if (string.IsNullOrWhiteSpace(SDT))
                ModelState.AddModelError("SDT", "Vui lòng nhập số điện thoại.");

            if (string.IsNullOrWhiteSpace(Password))
                ModelState.AddModelError("Password", "Vui lòng nhập mật khẩu.");

            if (Password != ConfirmPassword)
                ModelState.AddModelError("ConfirmPassword", "Mật khẩu nhập lại không khớp.");

            if (db.User.Any(u => u.Username == Username))
                ModelState.AddModelError("Username", "Tên tài khoản đã tồn tại.");

            if (!ModelState.IsValid)
                return View();

            Username = Username.Trim();
            Email = Email?.Trim();
            SDT = SDT?.Trim();

            // ----- TẠO USER (BẢNG User) -----
            var user = new User
            {
                Username = Username,
                Password = HashPassword(Password),
                UserRole = "C",          // C = Customer
                Email = Email,
                SDT = SDT
            };
            db.User.Add(user);

            // ----- TẠO CUSTOMER GẮN VỚI USER.Username -----
            var customer = new Customer
            {
                Username = Username,
                CustomerName = Username,
                CustomerPhone = SDT ?? "",
                CustomerEmail = Email ?? "",
                CustomerAddress = ""
            };
            db.Customer.Add(customer);

            try
            {
                db.SaveChanges();

                // Không auto login, bắt đăng nhập lại
                TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login", "Account");
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

            // Tìm hoặc tạo Customer tương ứng với User
            var customer = db.Customer.FirstOrDefault(c => c.Username == user.Username);
            if (customer == null)
            {
                customer = new Customer
                {
                    Username = user.Username,
                    CustomerName = user.Username,
                    CustomerPhone = "",
                    CustomerEmail = "",
                    CustomerAddress = ""
                };
                db.Customer.Add(customer);
                db.SaveChanges();
            }

            // Lưu session – dùng cho header, phân quyền...
            Session["Username"] = user.Username;
            Session["UserRole"] = user.UserRole;
            Session["CustomerID"] = customer.CustomerID;

            // PHÂN QUYỀN
            if (user.UserRole == "A") // Admin
            {
                return RedirectToAction("Index", "AdminHome", new { area = "Admin" });
            }
            else // Customer
            {
                return RedirectToAction("Index", "Customer");
            }
        }

        // ========== ĐĂNG XUẤT ==========

        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // ========== HỒ SƠ CÁ NHÂN ==========

        // PROFILE - GET
        public ActionResult Profile()
        {
            if (Session["Username"] == null)
                return RedirectToAction("Login", "Account");

            var username = Session["Username"].ToString();

            var user = db.User.SingleOrDefault(u => u.Username == username);
            if (user == null)
                return HttpNotFound("Không tìm thấy tài khoản người dùng.");

            // Lấy Customer theo Username, nếu chưa có thì tạo rỗng
            var customer = db.Customer.SingleOrDefault(c => c.Username == username);
            if (customer == null)
            {
                customer = new Customer
                {
                    Username = username,
                    CustomerName = "",
                    CustomerPhone = "",
                    CustomerEmail = "",
                    CustomerAddress = ""
                };
                db.Customer.Add(customer);
                db.SaveChanges();
            }

            var vm = new AccountProfileViewModel
            {
                CustomerID = customer.CustomerID,
                Username = customer.Username,
                CustomerName = customer.CustomerName,
                CustomerPhone = customer.CustomerPhone,
                CustomerEmail = customer.CustomerEmail,
                CustomerAddress = customer.CustomerAddress,

                EditCustomerName = customer.CustomerName,
                EditCustomerPhone = customer.CustomerPhone,
                EditCustomerEmail = customer.CustomerEmail,
                EditCustomerAddress = customer.CustomerAddress
            };

            ViewBag.SuccessMessage = TempData["SuccessMessage"];

            return View(vm);
        }

        // PROFILE - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Profile(AccountProfileViewModel model)
        {
            if (Session["Username"] == null)
                return RedirectToAction("Login", "Account");

            var username = Session["Username"].ToString();

            var user = db.User.SingleOrDefault(u => u.Username == username);
            if (user == null)
                return HttpNotFound("Không tìm thấy tài khoản người dùng.");

            var customer = db.Customer.SingleOrDefault(c => c.Username == username);
            if (customer == null)
            {
                customer = new Customer { Username = username };
                db.Customer.Add(customer);
            }

            if (!ModelState.IsValid)
            {
                // Đổ lại info đang có trong DB để view dùng
                model.CustomerID = customer.CustomerID;
                model.Username = customer.Username;
                model.CustomerName = customer.CustomerName;
                model.CustomerPhone = customer.CustomerPhone;
                model.CustomerEmail = customer.CustomerEmail;
                model.CustomerAddress = customer.CustomerAddress;
                return View(model);
            }

            // Cập nhật Customer
            customer.CustomerName = model.EditCustomerName;
            customer.CustomerPhone = model.EditCustomerPhone;
            customer.CustomerEmail = model.EditCustomerEmail;
            customer.CustomerAddress = model.EditCustomerAddress;

            // Đổi mật khẩu nếu có nhập
            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                // So sánh mật khẩu hiện tại dùng HASH
                var currentHash = HashPassword(model.CurrentPassword ?? "");

                if (string.IsNullOrWhiteSpace(model.CurrentPassword) ||
                    currentHash != user.Password)
                {
                    ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không đúng");

                    model.CustomerID = customer.CustomerID;
                    model.Username = customer.Username;
                    model.CustomerName = customer.CustomerName;
                    model.CustomerPhone = customer.CustomerPhone;
                    model.CustomerEmail = customer.CustomerEmail;
                    model.CustomerAddress = customer.CustomerAddress;
                    return View(model);
                }

                if (model.NewPassword != model.ConfirmNewPassword)
                {
                    ModelState.AddModelError("ConfirmNewPassword", "Mật khẩu nhập lại không khớp");

                    model.CustomerID = customer.CustomerID;
                    model.Username = customer.Username;
                    model.CustomerName = customer.CustomerName;
                    model.CustomerPhone = customer.CustomerPhone;
                    model.CustomerEmail = customer.CustomerEmail;
                    model.CustomerAddress = customer.CustomerAddress;
                    return View(model);
                }

                // Lưu mật khẩu mới dưới dạng HASH
                user.Password = HashPassword(model.NewPassword);
            }

            db.SaveChanges();

            TempData["SuccessMessage"] = "Cập nhật thông tin thành công.";
            return RedirectToAction("Profile");
        }
    }
}
