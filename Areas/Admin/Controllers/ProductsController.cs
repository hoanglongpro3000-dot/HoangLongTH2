using HoangLongTH.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace HoangLongTH.Areas.Admin.Controllers
{
    public class ProductsController : Controller
    {
        private MyStroreEntities1 db = new MyStroreEntities1();

        // GET: Admin/Products
        public ActionResult Index(string SearchString)
        {
            // Lấy danh sách sản phẩm + include Category
            var products = db.Product.Include(p => p.Category);

            // Nếu có từ khóa tìm kiếm
            if (!String.IsNullOrEmpty(SearchString))
            {
                products = products.Where(p => p.ProductName.Contains(SearchString));
            }

            // Trả về view
            return View(products.ToList());
        }

        // GET: Admin/Products/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product product = db.Product.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }
            return View(product);
        }

        // GET: Admin/Products/Create
        public ActionResult Create()
        {
            ViewBag.CategoryID = new SelectList(db.Category, "CategoryID", "CategoryName");
            return View();
        }

        // POST: Admin/Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ProductID,CategoryID,ProductName,ProductDecription,ProductPrice,ProductImage,Quantity")] Product product)
        {
            // Chuẩn hóa đầu vào (tránh null/space)
            product.ProductName = product.ProductName?.Trim();
            product.ProductDecription = product.ProductDecription?.Trim();
            product.ProductImage = string.IsNullOrWhiteSpace(product.ProductImage)
                                        ? "/Content/imgs/no-image.png"  // ảnh mặc định
                                        : product.ProductImage.Trim();

            // Kiểm tra danh mục tồn tại
            if (!db.Category.Any(c => c.CategoryID == product.CategoryID))
                ModelState.AddModelError("CategoryID", "Vui lòng chọn danh mục hợp lệ.");

            // Ràng buộc cơ bản
            if (string.IsNullOrWhiteSpace(product.ProductName))
                ModelState.AddModelError("ProductName", "Tên sản phẩm là bắt buộc.");

            if (product.ProductPrice <= 0)
                ModelState.AddModelError("ProductPrice", "Giá phải lớn hơn 0.");

            if (product.Quantity < 0)
                ModelState.AddModelError("Quantity", "Số lượng không được âm.");

            if (!ModelState.IsValid)
            {
                ViewBag.CategoryID = new SelectList(db.Category, "CategoryID", "CategoryName", product.CategoryID);
                return View(product);
            }

            try
            {
                db.Product.Add(product);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            catch (DbEntityValidationException ex)
            {
                // Hiển thị lỗi cột nào sai đúng theo EF
                foreach (var eve in ex.EntityValidationErrors)
                {
                    foreach (var ve in eve.ValidationErrors)
                    {
                        ModelState.AddModelError(ve.PropertyName, ve.ErrorMessage);
                        System.Diagnostics.Debug.WriteLine($"EF Validate: {ve.PropertyName} - {ve.ErrorMessage}");
                    }
                }
                ViewBag.CategoryID = new SelectList(db.Category, "CategoryID", "CategoryName", product.CategoryID);
                return View(product);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                ViewBag.CategoryID = new SelectList(db.Category, "CategoryID", "CategoryName", product.CategoryID);
                return View(product);
            }
        }

        // GET: Admin/Products/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product product = db.Product.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }
            ViewBag.CategoryID = new SelectList(db.Category, "CategoryID", "CategoryName", product.CategoryID);
            return View(product);
        }

        // POST: Admin/Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ProductID,CategoryID,ProductName,ProductDecription,ProductPrice,ProductImage,Quantity")] Product product)
        {
            if (product.Quantity < 0)
                ModelState.AddModelError("Quantity", "Số lượng không được âm.");

            if (ModelState.IsValid)
            {
                db.Entry(product).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.CategoryID = new SelectList(db.Category, "CategoryID", "CategoryName", product.CategoryID);
            return View(product);
        }

        // GET: Admin/Products/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product product = db.Product.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }
            return View(product);
        }

        // POST: Admin/Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Product product = db.Product.Find(id);
            db.Product.Remove(product);
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
