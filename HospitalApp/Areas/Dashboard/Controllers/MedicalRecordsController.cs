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
    [Authorize(Roles = "Admin,Doctor,Receptionist   ")]
    public class MedicalRecordsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public MedicalRecordsController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index(string? search, string? sortOrder, int page = 1)
        {
            const int pageSize = 10;

            var q = _db.MedicalRecords
                .Include(m => m.Patient)
                .Include(m => m.Doctor)
                .AsNoTracking()
                .AsQueryable();

            // ===== TÌM KIẾM =====
            if (!string.IsNullOrWhiteSpace(search))
            {
                var k = search.Trim();

                if (DateTime.TryParse(k, out var dt))
                {
                    var d = dt.Date;
                    q = q.Where(m => m.Time.Date == d);
                }
                else
                {
                    bool tryPay(string s, out PaymentStatus val) => Enum.TryParse(s, true, out val);

                    if (tryPay(k, out var pay))
                    {
                        q = q.Where(m => m.PaymentStatus == pay);
                    }
                    else
                    {
                        q = q.Where(m =>
                            (m.Patient.FullName != null && m.Patient.FullName.Contains(k)) ||
                            (m.Doctor.FullName != null && m.Doctor.FullName.Contains(k)));
                    }
                }

                ViewData["Search"] = k;
            }

            // Tổng trước khi trang
            var totalItems = await q.CountAsync();

            // ===== SẮP XẾP =====
            ViewData["CurrentSort"] = sortOrder;
            ViewData["TimeSort"] = string.IsNullOrEmpty(sortOrder) ? "time_desc" : "";
            ViewData["PatientSort"] = sortOrder == "patient" ? "patient_desc" : "patient";
            ViewData["DoctorSort"] = sortOrder == "doctor" ? "doctor_desc" : "doctor";
            ViewData["PaySort"] = sortOrder == "pay" ? "pay_desc" : "pay";

            q = sortOrder switch
            {
                "time_desc" => q.OrderByDescending(m => m.Time),
                "patient" => q.OrderBy(m => m.Patient.FullName).ThenByDescending(m => m.Time),
                "patient_desc" => q.OrderByDescending(m => m.Patient.FullName).ThenByDescending(m => m.Time),
                "doctor" => q.OrderBy(m => m.Doctor.FullName).ThenByDescending(m => m.Time),
                "doctor_desc" => q.OrderByDescending(m => m.Doctor.FullName).ThenByDescending(m => m.Time),
                "pay" => q.OrderBy(m => m.PaymentStatus).ThenByDescending(m => m.Time),
                "pay_desc" => q.OrderByDescending(m => m.PaymentStatus).ThenByDescending(m => m.Time),
                _ => q.OrderByDescending(m => m.Time)
            };

            // ===== PHÂN TRANG =====
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var data = await q
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewData["Page"] = page;
            ViewData["TotalPages"] = totalPages;
            ViewData["PageSize"] = pageSize;
            ViewData["TotalItems"] = totalItems;

            return View(data);
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

            var record = await _db.MedicalRecords
                .Include(r => r.Information)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (record == null) return NotFound();

            LoadDrop(record.PatientId, record.DoctorId, record.PaymentStatus);

            // Nếu chưa có Information → tạo để View binding không lỗi
            if (record.Information == null)
            {
                record.Information = new MedicalRecordInformation
                {
                    MedicalRecordId = record.Id
                };
            }

            return View(record);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MedicalRecord model)
        {
            if (id != model.Id) return NotFound();

            var record = await _db.MedicalRecords
                .Include(r => r.Information)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (record == null) return NotFound();

            RemoveNav("Patient");
            RemoveNav("Doctor");

            if (!ModelState.IsValid)
            {
                LoadDrop(model.PatientId, model.DoctorId, model.PaymentStatus);
                return View(model);
            }

            // ===== Update main record =====
            record.Time = model.Time;
            record.PatientId = model.PatientId;
            record.DoctorId = model.DoctorId;
            record.Status = model.Status;
            record.PaymentStatus = model.PaymentStatus;

            // ===== Update MedicalRecordInformation =====
            if (record.Information == null)
                record.Information = new MedicalRecordInformation { MedicalRecordId = record.Id };

            record.Information.BloodPressure = model.Information?.BloodPressure;
            record.Information.BodyTemperature = model.Information?.BodyTemperature;
            record.Information.HeartBeat = model.Information?.HeartBeat;
            record.Information.Height = model.Information?.Height;
            record.Information.Weight = model.Information?.Weight;
            record.Information.Diagnose = model.Information?.Diagnose;
            record.Information.Detail = model.Information?.Detail;
            record.Information.Solution = model.Information?.Solution;

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var m = await _db.MedicalRecords
                .Include(x => x.Patient)
                .Include(x => x.Doctor)
                .Include(x => x.Information)
                .Include(x => x.MedicalTests)
                    .ThenInclude(t => t.Services)
                        .ThenInclude(s => s.Service)
                .Include(x => x.Prescriptions)
                    .ThenInclude(p => p.Medicines)
                        .ThenInclude(mp => mp.Medicine)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (m == null) return NotFound();

            return View(m);
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
