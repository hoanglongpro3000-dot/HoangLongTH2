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
    public class CategoriesController : Controller
    {
        private MyStroreEntities1 db = new MyStroreEntities1();

        // GET: Admin/Categories
        // Thêm tìm kiếm theo tên danh mục
        public ActionResult Index(string search)
        {
            var categories = db.Category.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string keyword = search.Trim();
                categories = categories.Where(c => c.CategoryName.Contains(keyword));
            }

            // Gửi lại từ khóa để hiển thị lại trong ô search
            ViewBag.Search = search;

            return View(categories
                        .OrderBy(c => c.CategoryName)
                        .ToList());
        }

        // GET: Admin/Categories/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Category category = db.Category.Find(id);
            if (category == null)
                return HttpNotFound();

            return View(category);
        }

        // GET: Admin/Categories/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Admin/Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "CategoryID,CategoryName")] Category category)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra trùng tên (không phân biệt hoa thường)
                bool isDuplicate = db.Category
                    .Any(c => c.CategoryName.ToLower() == category.CategoryName.ToLower());

                if (isDuplicate)
                {
                    TempData["ErrorMessage"] = "⚠️ Danh mục này đã tồn tại, vui lòng nhập tên khác.";
                    return RedirectToAction("Index");
                }

                db.Category.Add(category);
                db.SaveChanges();
                TempData["SuccessMessage"] = "✅ Thêm danh mục mới thành công!";
                return RedirectToAction("Index");
            }

            return View(category);
        }

        // GET: Admin/Categories/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Category category = db.Category.Find(id);
            if (category == null)
                return HttpNotFound();

            return View(category);
        }

        // POST: Admin/Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "CategoryID,CategoryName")] Category category)
        {
            if (ModelState.IsValid)
            {
                db.Entry(category).State = EntityState.Modified;
                db.SaveChanges();
                TempData["SuccessMessage"] = "Cập nhật danh mục thành công!";
                return RedirectToAction("Index");
            }
            return View(category);
        }

        // GET: Admin/Categories/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Category category = db.Category.Find(id);
            if (category == null)
                return HttpNotFound();

            return View(category);
        }

        // POST: Admin/Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var category = db.Category.Find(id);

            if (category == null)
            {
                TempData["ErrorMessage"] = "Danh mục không tồn tại.";
                return RedirectToAction("Index");
            }

            // Kiểm tra xem có sản phẩm nào thuộc danh mục này không
            bool hasProducts = db.Product.Any(p => p.CategoryID == id);

            if (hasProducts)
            {
                TempData["ErrorMessage"] = "❌ Không thể xóa danh mục vì vẫn còn sản phẩm thuộc danh mục này.";
                return RedirectToAction("Index");
            }

            db.Category.Remove(category);
            db.SaveChanges();
            TempData["SuccessMessage"] = "✅ Đã xóa danh mục thành công!";
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
