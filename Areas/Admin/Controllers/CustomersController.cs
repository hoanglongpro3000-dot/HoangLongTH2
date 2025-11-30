using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using HoangLongTH.Models;

namespace HoangLongTH.Areas.Admin.Controllers
{
    public class CustomersController : Controller
    {
        private MyStroreEntities1 db = new MyStroreEntities1();

        // GET: Admin/Customers
        public ActionResult Index()
        {
            var customers = db.Customer.Include(c => c.User);
            return View(customers.ToList());
        }

        // GET: Admin/Customers/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Customer customer = db.Customer.Find(id);
            if (customer == null)
                return HttpNotFound();

            // Lấy danh sách đơn hàng của khách
            ViewBag.Orders = db.Order
                               .Include(o => o.OrderDetail)
                               .Where(o => o.CustomerID == id)
                               .ToList();

            return View(customer);
        }

        // Không cho phép thêm, sửa, xóa khách hàng
        // => Comment hoặc bỏ hẳn các hành động Create, Edit, Delete

        /*
        // GET: Admin/Customers/Create
        public ActionResult Create()
        {
            ViewBag.Username = new SelectList(db.User, "Username", "Password");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "CustomerID,CustomerName,CustomerPhone,CustomerEmail,CustomerAddress,Username")] Customer customer)
        {
            if (ModelState.IsValid)
            {
                db.Customer.Add(customer);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.Username = new SelectList(db.User, "Username", "Password", customer.Username);
            return View(customer);
        }

        // GET: Admin/Customers/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Customer customer = db.Customer.Find(id);
            if (customer == null)
                return HttpNotFound();

            ViewBag.Username = new SelectList(db.User, "Username", "Password", customer.Username);
            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "CustomerID,CustomerName,CustomerPhone,CustomerEmail,CustomerAddress,Username")] Customer customer)
        {
            if (ModelState.IsValid)
            {
                db.Entry(customer).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.Username = new SelectList(db.User, "Username", "Password", customer.Username);
            return View(customer);
        }

        // GET: Admin/Customers/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Customer customer = db.Customer.Find(id);
            if (customer == null)
                return HttpNotFound();

            return View(customer);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var customer = db.Customer.Find(id);
            if (customer == null)
                return HttpNotFound();

            db.Customer.Remove(customer);
            db.SaveChanges();
            return RedirectToAction("Index");
        }
        */

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
