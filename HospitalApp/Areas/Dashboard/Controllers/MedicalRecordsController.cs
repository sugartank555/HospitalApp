using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HospitalApp.Data;
using HospitalApp.Models;
using HospitalApp.Models.Enums;

namespace HospitalApp.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Admin,Doctor")]
    public class MedicalRecordsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public MedicalRecordsController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var q = _db.MedicalRecords.Include(m => m.Patient).Include(m => m.Doctor).AsNoTracking();
            return View(await q.ToListAsync());
        }

        public IActionResult Create() { LoadDrop(); return View(new MedicalRecord { Time = DateTime.Now }); }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MedicalRecord m)
        {
            RemoveNav("Patient"); RemoveNav("Doctor");
            if (!ModelState.IsValid) { LoadDrop(m.PatientId, m.DoctorId, m.PaymentStatus); return View(m); }
            _db.Add(m); await _db.SaveChangesAsync(); return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var m = await _db.MedicalRecords.FindAsync(id);
            if (m == null) return NotFound();
            LoadDrop(m.PatientId, m.DoctorId, m.PaymentStatus);
            return View(m);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MedicalRecord m)
        {
            if (id != m.Id) return NotFound();
            RemoveNav("Patient"); RemoveNav("Doctor");
            if (!ModelState.IsValid) { LoadDrop(m.PatientId, m.DoctorId, m.PaymentStatus); return View(m); }
            _db.Update(m); await _db.SaveChangesAsync(); return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var m = await _db.MedicalRecords.Include(x => x.Patient).Include(x => x.Doctor)
                .AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return m == null ? NotFound() : View(m);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var m = await _db.MedicalRecords.Include(x => x.Patient).Include(x => x.Doctor)
                .AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return m == null ? NotFound() : View(m);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var m = await _db.MedicalRecords.FindAsync(id);
            if (m != null) { _db.MedicalRecords.Remove(m); await _db.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }

        private void LoadDrop(int? patientId = null, int? doctorId = null, PaymentStatus? pay = null)
        {
            ViewData["PatientId"] = new SelectList(_db.Patients.AsNoTracking(), "Id", "FullName", patientId);
            ViewData["DoctorId"] = new SelectList(_db.Doctors.AsNoTracking(), "Id", "FullName", doctorId);
            ViewData["PaymentStatus"] = Enum.GetValues(typeof(PaymentStatus)).Cast<PaymentStatus>()
                .Select(e => new SelectListItem { Value = ((int)e).ToString(), Text = e.ToString(), Selected = pay == e });
        }
        private void RemoveNav(string key)
        {
            foreach (var k in ModelState.Keys.Where(k => k.Equals(key) || k.StartsWith(key + ".")).ToList())
                ModelState.Remove(k);
        }
    }
}
