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
    public class UsersController : Controller
    {
        private MyStroreEntities1 db = new MyStroreEntities1();

        // ================== INDEX ==================
        // Chỉ hiển thị tài khoản hệ thống (Admin) => UserRole = "A"
        public ActionResult Index()
        {
            var admins = db.User
                           .Where(u => u.UserRole == "A")
                           .OrderBy(u => u.Username)
                           .ToList();

            return View(admins);
        }

        // ================== DETAILS ==================
        public ActionResult Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var user = db.User.FirstOrDefault(u => u.Username == id);
            if (user == null)
            {
                return HttpNotFound();
            }

            // Lấy khách hàng liên kết nếu có
            var customers = db.Customer
                              .Where(c => c.Username == id)
                              .Include(c => c.Order)
                              .ToList();

            ViewBag.Customers = customers;

            return View(user);
        }

        // ================== CREATE (GET) ==================
        public ActionResult Create()
        {
            // RoleList chỉ có Admin
            ViewBag.RoleList = new List<SelectListItem>
            {
                new SelectListItem { Text = "Admin", Value = "A" }
            };
            return View();
        }

        // ================== CREATE (POST) ==================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Username,Password,UserRole")] User user)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Luôn là admin
                    user.UserRole = "A";

                    db.User.Add(user);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException ex)
                {
                    var errorMessages = new List<string>();
                    foreach (var entityErrors in ex.EntityValidationErrors)
                    {
                        foreach (var error in entityErrors.ValidationErrors)
                        {
                            errorMessages.Add($"Thuộc tính: {error.PropertyName} - Lỗi: {error.ErrorMessage}");
                        }
                    }

                    ViewBag.ErrorDetails = string.Join("<br>", errorMessages);
                }
            }

            ViewBag.RoleList = new List<SelectListItem>
            {
                new SelectListItem { Text = "Admin", Value = "A" }
            };

            return View(user);
        }

        // ================== EDIT (GET) ==================
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var user = db.User.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }

            ViewBag.RoleList = new List<SelectListItem>
            {
                new SelectListItem { Text = "Admin", Value = "A", Selected = true }
            };

            return View(user);
        }

        // ================== EDIT (POST) ==================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(User updatedUser)
        {
            if (ModelState.IsValid)
            {
                var existingUser = db.User.Find(updatedUser.Username);
                if (existingUser == null)
                {
                    return HttpNotFound();
                }

                existingUser.Password = updatedUser.Password;
                existingUser.UserRole = "A"; // luôn admin

                db.Entry(existingUser).State = EntityState.Modified;
                db.SaveChanges();

                return RedirectToAction("Index");
            }

            ViewBag.RoleList = new List<SelectListItem>
            {
                new SelectListItem { Text = "Admin", Value = "A", Selected = true }
            };
            return View(updatedUser);
        }

        // ================== DELETE (GET) ==================
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var user = db.User.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }

            return View(user);
        }

        // ================== DELETE (POST) ==================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            var user = db.User.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }

            // Xóa Customer + Order nếu có
            var customers = db.Customer.Where(c => c.Username == id).ToList();

            foreach (var customer in customers)
            {
                var orders = db.Order.Where(o => o.CustomerID == customer.CustomerID).ToList();
                foreach (var order in orders)
                {
                    db.Order.Remove(order);
                }

                db.Customer.Remove(customer);
            }

            db.User.Remove(user);
            db.SaveChanges();

            return RedirectToAction("Index");
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
