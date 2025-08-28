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

        public async Task<IActionResult> Index()
        {
            var q = _db.Services.Include(s => s.MedicalDepartment).AsNoTracking();
            return View(await q.ToListAsync());
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
