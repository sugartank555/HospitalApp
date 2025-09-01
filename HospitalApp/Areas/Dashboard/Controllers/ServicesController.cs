using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HospitalApp.Data;
using HospitalApp.Models;

namespace HospitalApp.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Admin,Doctor")]
    public class ServicesController : Controller
    {
        private readonly ApplicationDbContext _db;
        public ServicesController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index(string? search, string? sortOrder)
        {
            var q = _db.Services
                .Include(s => s.MedicalDepartment)
                .AsNoTracking()
                .AsQueryable();

            // ====== SEARCH ======
            if (!string.IsNullOrWhiteSpace(search))
            {
                var k = search.Trim();

                // Nếu người dùng gõ số -> cho phép lọc theo đúng giá
                if (decimal.TryParse(k, out var price))
                {
                    q = q.Where(s => s.Price == price
                                     || s.Name.Contains(k)
                                     || (s.Description != null && s.Description.Contains(k))
                                     || (s.MedicalDepartment != null && s.MedicalDepartment.Name.Contains(k)));
                }
                else
                {
                    q = q.Where(s =>
                        s.Name.Contains(k) ||
                        (s.Description != null && s.Description.Contains(k)) ||
                        (s.MedicalDepartment != null && s.MedicalDepartment.Name.Contains(k)));
                }

                ViewData["Search"] = k;
            }

            // ====== SORT (theo GIÁ) ======
            ViewData["CurrentSort"] = sortOrder;
            ViewData["PriceSort"] = sortOrder == "price" ? "price_desc" : "price";

            q = sortOrder switch
            {
                "price" => q.OrderBy(s => s.Price),
                "price_desc" => q.OrderByDescending(s => s.Price),
                _ => q.OrderBy(s => s.Name) // mặc định: theo tên
            };

            var data = await q.ToListAsync();
            return View(data);
        }


        public IActionResult Create() { LoadDeps(); return View(); }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Service m)
        {
            RemoveNav("MedicalDepartment");
            if (!ModelState.IsValid) { LoadDeps(m.MedicalDepartmentId); return View(m); }
            _db.Add(m); await _db.SaveChangesAsync(); return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var m = await _db.Services.FindAsync(id);
            if (m == null) return NotFound();
            LoadDeps(m.MedicalDepartmentId); return View(m);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Service m)
        {
            if (id != m.Id) return NotFound();
            RemoveNav("MedicalDepartment");
            if (!ModelState.IsValid) { LoadDeps(m.MedicalDepartmentId); return View(m); }
            _db.Update(m); await _db.SaveChangesAsync(); return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var m = await _db.Services.Include(s => s.MedicalDepartment)
                    .AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return m == null ? NotFound() : View(m);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var m = await _db.Services.Include(s => s.MedicalDepartment)
                    .AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return m == null ? NotFound() : View(m);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var m = await _db.Services.FindAsync(id);
            if (m != null) { _db.Services.Remove(m); await _db.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }

        private void LoadDeps(int? selected = null)
            => ViewData["MedicalDepartmentId"] = new SelectList(_db.MedicalDepartments.AsNoTracking(), "Id", "Name", selected);

        private void RemoveNav(string navKey)
        {
            foreach (var k in ModelState.Keys.Where(k => k.Equals(navKey) || k.StartsWith(navKey + ".", StringComparison.OrdinalIgnoreCase)).ToList())
                ModelState.Remove(k);
        }
    }
}
