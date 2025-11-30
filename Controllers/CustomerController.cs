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
    public class CustomerController : Controller
    {
        private MyStroreEntities1 db = new MyStroreEntities1();

        // GET: /Customer
        public ActionResult Index()
        {
            var username = Session["Username"] as string;
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            var customer = db.Customer
                             .Include("Order")
                             .FirstOrDefault(c => c.Username == username);

            if (customer == null)
            {
                // lệch dữ liệu -> logout cho sạch
                return RedirectToAction("Logout", "Account");
            }

            return View(customer);
        }
    }
}