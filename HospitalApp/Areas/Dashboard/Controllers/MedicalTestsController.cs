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
    public class MedicalTestsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public MedicalTestsController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var data = await _db.MedicalTests.Include(t => t.MedicalRecord).AsNoTracking().ToListAsync();
            return View(data);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var m = await _db.MedicalTests.Include(t => t.MedicalRecord)
                        .AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return m == null ? NotFound() : View(m);
        }

        public IActionResult Create()
        {
            LoadMR();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MedicalTest model)
        {
            RemoveNav("MedicalRecord");
            if (!ModelState.IsValid)
            {
                LoadMR(model.MedicalRecordId);
                return View(model);
            }
            _db.MedicalTests.Add(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var m = await _db.MedicalTests.FindAsync(id);
            if (m == null) return NotFound();
            LoadMR(m.MedicalRecordId);
            return View(m);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MedicalTest model)
        {
            if (id != model.Id) return NotFound();
            RemoveNav("MedicalRecord");
            if (!ModelState.IsValid)
            {
                LoadMR(model.MedicalRecordId);
                return View(model);
            }
            _db.Update(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var m = await _db.MedicalTests.Include(t => t.MedicalRecord)
                        .AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return m == null ? NotFound() : View(m);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var m = await _db.MedicalTests.FindAsync(id);
            if (m != null) { _db.MedicalTests.Remove(m); await _db.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }

        private void LoadMR(int? selected = null)
            => ViewData["MedicalRecordId"] = new SelectList(_db.MedicalRecords.AsNoTracking(), "Id", "Id", selected);

        private void RemoveNav(string key)
        {
            foreach (var k in ModelState.Keys.Where(k => k.Equals(key) || k.StartsWith(key + ".", StringComparison.OrdinalIgnoreCase)).ToList())
                ModelState.Remove(k);
        }
    }
}
