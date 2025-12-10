using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HospitalApp.Data;
using HospitalApp.Models;

namespace HospitalApp.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Admin,Doctor")]
    public class MedicinesController : Controller
    {
        private readonly ApplicationDbContext _db;
        public MedicinesController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index(string? search, string? sortOrder, int page = 1)
        {
            int pageSize = 10; // số dòng mỗi trang

            var q = _db.Medicines
                .AsNoTracking()
                .AsQueryable();

            // ===== TÌM KIẾM =====
            if (!string.IsNullOrWhiteSpace(search))
            {
                var k = search.Trim();

                if (decimal.TryParse(k, out var price))
                {
                    q = q.Where(m =>
                        m.Price == price ||
                        m.Name.Contains(k) ||
                        (m.ActiveElement != null && m.ActiveElement.Contains(k)) ||
                        (m.ProductionUnit != null && m.ProductionUnit.Contains(k)) ||
                        (m.Unit != null && m.Unit.Contains(k)) ||
                        (m.DeclaringUnit != null && m.DeclaringUnit.Contains(k)) ||
                        (m.Packing != null && m.Packing.Contains(k)) ||
                        (m.Using != null && m.Using.Contains(k)) ||
                        (m.UseManual != null && m.UseManual.Contains(k))
                    );
                }
                else if (int.TryParse(k, out var id))
                {
                    q = q.Where(m =>
                        m.Id == id ||
                        m.Name.Contains(k) ||
                        (m.ActiveElement != null && m.ActiveElement.Contains(k)) ||
                        (m.ProductionUnit != null && m.ProductionUnit.Contains(k)) ||
                        (m.Unit != null && m.Unit.Contains(k)) ||
                        (m.DeclaringUnit != null && m.DeclaringUnit.Contains(k)) ||
                        (m.Packing != null && m.Packing.Contains(k)) ||
                        (m.Using != null && m.Using.Contains(k)) ||
                        (m.UseManual != null && m.UseManual.Contains(k))
                    );
                }
                else
                {
                    q = q.Where(m =>
                        m.Name.Contains(k) ||
                        (m.ActiveElement != null && m.ActiveElement.Contains(k)) ||
                        (m.ProductionUnit != null && m.ProductionUnit.Contains(k)) ||
                        (m.Unit != null && m.Unit.Contains(k)) ||
                        (m.DeclaringUnit != null && m.DeclaringUnit.Contains(k)) ||
                        (m.Packing != null && m.Packing.Contains(k)) ||
                        (m.Using != null && m.Using.Contains(k)) ||
                        (m.UseManual != null && m.UseManual.Contains(k))
                    );
                }

                ViewData["Search"] = k;
            }

            // ===== SẮP XẾP =====
            ViewData["CurrentSort"] = sortOrder;
            ViewData["PriceSort"] = sortOrder == "price" ? "price_desc" : "price";
            ViewData["NameSort"] = sortOrder == "name" ? "name_desc" : "name";

            q = sortOrder switch
            {
                "price" => q.OrderBy(m => m.Price),
                "price_desc" => q.OrderByDescending(m => m.Price),
                "name" => q.OrderBy(m => m.Name),
                "name_desc" => q.OrderByDescending(m => m.Name),
                _ => q.OrderBy(m => m.Name)
            };

            // ===== PHÂN TRANG =====
            int totalItems = await q.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var data = await q.Skip((page - 1) * pageSize)
                              .Take(pageSize)
                              .ToListAsync();

            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;

            return View(data);
        }

        // GET: /Dashboard/Medicines/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var m = await _db.Medicines.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return m == null ? NotFound() : View(m);
        }

        // GET: /Dashboard/Medicines/Create
        public IActionResult Create() => View();

        // POST: /Dashboard/Medicines/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Medicine model)
        {
            // Không động vào navigation nào cả
            if (!ModelState.IsValid) return View(model);

            _db.Medicines.Add(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: /Dashboard/Medicines/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var m = await _db.Medicines.FindAsync(id);
            return m == null ? NotFound() : View(m);
        }

        // POST: /Dashboard/Medicines/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Medicine model)
        {
            if (id != model.Id) return NotFound();
            if (!ModelState.IsValid) return View(model);

            _db.Update(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: /Dashboard/Medicines/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var m = await _db.Medicines.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return m == null ? NotFound() : View(m);
        }

        // POST: /Dashboard/Medicines/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var m = await _db.Medicines.FindAsync(id);
            if (m == null) return RedirectToAction(nameof(Index));

            try
            {
                _db.Medicines.Remove(m);
                await _db.SaveChangesAsync();
                TempData["Msg"] = "Đã xoá thuốc.";
            }
            catch (DbUpdateException)
            {
                // Nếu đang bị tham chiếu bởi MedicineOfPrescription → báo lỗi dễ hiểu
                TempData["Error"] = "Không thể xoá vì thuốc đang được sử dụng trong toa. Hãy gỡ khỏi các toa trước.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
