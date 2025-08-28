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

        // GET: /Dashboard/Medicines
        public async Task<IActionResult> Index()
        {
            var data = await _db.Medicines
                .AsNoTracking()
                .OrderBy(m => m.Name)
                .ToListAsync();
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
