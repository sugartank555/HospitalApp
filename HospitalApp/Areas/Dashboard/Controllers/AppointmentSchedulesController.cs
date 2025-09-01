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
    public class AppointmentSchedulesController : Controller
    {
        private readonly ApplicationDbContext _db;
        public AppointmentSchedulesController(ApplicationDbContext db) => _db = db;

        // GET: /Dashboard/AppointmentSchedules
        // GET: /Dashboard/AppointmentSchedules
        // GET: /Dashboard/AppointmentSchedules
        public async Task<IActionResult> Index(string? search, string? sortOrder, int page = 1)
        {
            const int pageSize = 10;

            var q = _db.AppointmentSchedules
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .AsNoTracking()
                .AsQueryable();

            // ====== FILTER / SEARCH ======
            if (!string.IsNullOrWhiteSpace(search))
            {
                var k = search.Trim();

                if (DateTime.TryParse(k, out var day))
                {
                    var d = day.Date;
                    q = q.Where(a => a.Date.Date == d);
                }
                else
                {
                    q = q.Where(a =>
                        (a.Patient.FullName != null && a.Patient.FullName.Contains(k)) ||
                        (a.Doctor.FullName != null && a.Doctor.FullName.Contains(k)) ||
                        (a.TimeFrame != null && a.TimeFrame.Contains(k))
                    );
                }
                ViewData["Search"] = k;
            }

            // Tổng trước khi trang
            var totalItems = await q.CountAsync();

            // ====== SORT TOGGLES FOR VIEW ======
            ViewData["CurrentSort"] = sortOrder;
            ViewData["DateSort"] = string.IsNullOrEmpty(sortOrder) ? "date_desc" : "";
            ViewData["TimeSort"] = sortOrder == "time" ? "time_desc" : "time";
            ViewData["StatusSort"] = sortOrder == "status" ? "status_desc" : "status";
            ViewData["PatientSort"] = sortOrder == "patient" ? "patient_desc" : "patient";
            ViewData["DoctorSort"] = sortOrder == "doctor" ? "doctor_desc" : "doctor";

            // ====== APPLY SORT ======
            q = sortOrder switch
            {
                "date_desc" => q.OrderByDescending(a => a.Date).ThenByDescending(a => a.TimeFrame),
                "time" => q.OrderBy(a => a.TimeFrame),
                "time_desc" => q.OrderByDescending(a => a.TimeFrame),
                "status" => q.OrderBy(a => a.Status).ThenBy(a => a.Date),
                "status_desc" => q.OrderByDescending(a => a.Status).ThenByDescending(a => a.Date),
                "patient" => q.OrderBy(a => a.Patient.FullName),
                "patient_desc" => q.OrderByDescending(a => a.Patient.FullName),
                "doctor" => q.OrderBy(a => a.Doctor.FullName),
                "doctor_desc" => q.OrderByDescending(a => a.Doctor.FullName),
                _ => q.OrderBy(a => a.Date).ThenBy(a => a.TimeFrame)
            };

            // ====== PAGINATION ======
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var data = await q
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Info cho View
            ViewData["Page"] = page;
            ViewData["TotalPages"] = totalPages;
            ViewData["PageSize"] = pageSize;
            ViewData["TotalItems"] = totalItems;

            return View(data);
        }



        // GET: /Dashboard/AppointmentSchedules/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var m = await _db.AppointmentSchedules
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
            return m == null ? NotFound() : View(m);
        }

        // GET: Create
        public IActionResult Create()
        {
            LoadDrop();
            return View(new AppointmentSchedule { Date = DateTime.Today }); // <-- DateTime
        }

        // POST: Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AppointmentSchedule model)
        {
            RemoveNav("Patient"); RemoveNav("Doctor");
            if (!ModelState.IsValid)
            {
                LoadDrop(model.PatientId, model.DoctorId);
                return View(model);
            }
            _db.AppointmentSchedules.Add(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Edit
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var m = await _db.AppointmentSchedules.FindAsync(id);
            if (m == null) return NotFound();
            LoadDrop(m.PatientId, m.DoctorId);
            return View(m);
        }

        // POST: Edit
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AppointmentSchedule model)
        {
            if (id != model.Id) return NotFound();
            RemoveNav("Patient"); RemoveNav("Doctor");
            if (!ModelState.IsValid)
            {
                LoadDrop(model.PatientId, model.DoctorId);
                return View(model);
            }
            _db.Update(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Delete
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var m = await _db.AppointmentSchedules
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
            return m == null ? NotFound() : View(m);
        }

        // POST: Delete
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var m = await _db.AppointmentSchedules.FindAsync(id);
            if (m != null)
            {
                _db.AppointmentSchedules.Remove(m);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // (tuỳ chọn) Cập nhật nhanh trạng thái từ Index
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, AppointmentStatus status)
        {
            var item = await _db.AppointmentSchedules.FindAsync(id);
            if (item == null) return NotFound();
            item.Status = status;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private void LoadDrop(int? patientId = null, int? doctorId = null)
        {
            ViewData["PatientId"] = new SelectList(_db.Patients.AsNoTracking(), "Id", "FullName", patientId);
            ViewData["DoctorId"] = new SelectList(_db.Doctors.AsNoTracking(), "Id", "FullName", doctorId);
        }

        private void RemoveNav(string key)
        {
            foreach (var k in ModelState.Keys
                     .Where(k => k.Equals(key) || k.StartsWith(key + ".", StringComparison.OrdinalIgnoreCase))
                     .ToList())
                ModelState.Remove(k);
        }
    }
}
