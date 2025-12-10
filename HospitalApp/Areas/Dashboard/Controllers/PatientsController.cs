using HospitalApp.Data;
using HospitalApp.Models;
using HospitalApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HospitalApp.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Admin,Doctor,Receptionist")]
    public class PatientsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public PatientsController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 10)
        {
            var q = _db.Patients
                .Include(p => p.User)
                .AsNoTracking()
                .AsQueryable();

            // ===== SEARCH =====
            if (!string.IsNullOrWhiteSpace(search))
            {
                var k = search.Trim();
                q = q.Where(p =>
                    (p.FullName != null && p.FullName.Contains(k)) ||
                    (p.User != null && (
                        (p.User.UserName != null && p.User.UserName.Contains(k)) ||
                        (p.User.Email != null && p.User.Email.Contains(k))
                    )));
                ViewData["Search"] = k;
            }

            // ===== PAGING =====
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            int totalItems = await q.CountAsync();

            var items = await q
                .OrderBy(p => p.FullName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vm = new PatientIndexViewModel
            {
                Items = items,
                Search = search,
                PagingInfo = new PagingInfo
                {
                    PageIndex = page,
                    PageSize = pageSize,
                    TotalItems = totalItems,
                    TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
                }
            };

            return View(vm);
        }

        // GET: /Dashboard/Patients/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var p = await _db.Patients.Include(x => x.User)
                        .AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return p == null ? NotFound() : View(p);
        }

        // GET: /Dashboard/Patients/Create
        public async Task<IActionResult> Create()
        {
            await LoadUsersAsync();
            return View();
        }

        // POST: /Dashboard/Patients/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Patient model)
        {
            ModelState.Remove("User");

            // Không cho trùng UserId
            if (!string.IsNullOrWhiteSpace(model.UserId))
            {
                bool exists = await _db.Patients.AnyAsync(p => p.UserId == model.UserId);
                if (exists)
                    ModelState.AddModelError("UserId", "Tài khoản này đã có hồ sơ bệnh nhân.");
            }

            // Nếu FullName trống, gán mặc định từ User
            if (string.IsNullOrWhiteSpace(model.FullName) && !string.IsNullOrWhiteSpace(model.UserId))
            {
                var display = await _db.Users
                    .Where(u => u.Id == model.UserId)
                    .Select(u => (u.UserName ?? u.Email ?? u.Id))
                    .FirstOrDefaultAsync();
                model.FullName = string.IsNullOrWhiteSpace(display) ? "Khách" : display;
            }

            if (!ModelState.IsValid)
            {
                await LoadUsersAsync(model.UserId);
                return View(model);
            }

            _db.Add(model);
            try
            {
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sql &&
                                               sql.Message.Contains("IX_Patients_UserId"))
            {
                ModelState.AddModelError("UserId", "Tài khoản này đã có hồ sơ bệnh nhân.");
                await LoadUsersAsync(model.UserId);
                return View(model);
            }
        }

        // GET: /Dashboard/Patients/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var p = await _db.Patients.FindAsync(id);
            if (p == null) return NotFound();
            await LoadUsersAsync(p.UserId, p.Id); // giữ được user hiện tại
            return View(p);
        }

        // POST: /Dashboard/Patients/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Patient model)
        {
            if (id != model.Id) return NotFound();
            ModelState.Remove("User");

            // Không cho đổi sang UserId đã được user khác dùng
            if (!string.IsNullOrWhiteSpace(model.UserId))
            {
                bool exists = await _db.Patients.AnyAsync(p => p.UserId == model.UserId && p.Id != id);
                if (exists)
                    ModelState.AddModelError("UserId", "Tài khoản này đã có hồ sơ bệnh nhân.");
            }

            // Nếu FullName trống, gán mặc định từ User
            if (string.IsNullOrWhiteSpace(model.FullName) && !string.IsNullOrWhiteSpace(model.UserId))
            {
                var display = await _db.Users
                    .Where(u => u.Id == model.UserId)
                    .Select(u => (u.UserName ?? u.Email ?? u.Id))
                    .FirstOrDefaultAsync();
                model.FullName = string.IsNullOrWhiteSpace(display) ? "Khách" : display;
            }

            if (!ModelState.IsValid)
            {
                await LoadUsersAsync(model.UserId, id);
                return View(model);
            }

            var entity = await _db.Patients.FirstOrDefaultAsync(p => p.Id == id);
            if (entity == null) return NotFound();

            _db.Entry(entity).CurrentValues.SetValues(model);

            try
            {
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sql &&
                                               sql.Message.Contains("IX_Patients_UserId"))
            {
                ModelState.AddModelError("UserId", "Tài khoản này đã có hồ sơ bệnh nhân.");
                await LoadUsersAsync(model.UserId, id);
                return View(model);
            }
        }

        // GET: /Dashboard/Patients/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var p = await _db.Patients.Include(x => x.User)
                        .AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (p == null) return NotFound();

            // Đếm dữ liệu phụ thuộc để cảnh báo
            ViewBag.AppointmentCount = await _db.AppointmentSchedules.CountAsync(a => a.PatientId == id);
            ViewBag.RecordCount = await _db.MedicalRecords.CountAsync(m => m.PatientId == id);

            return View(p);
        }

        // POST: /Dashboard/Patients/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Chặn xoá nếu còn phụ thuộc
            bool hasApp = await _db.AppointmentSchedules.AnyAsync(a => a.PatientId == id);
            bool hasRecord = await _db.MedicalRecords.AnyAsync(m => m.PatientId == id);

            if (hasApp || hasRecord)
            {
                TempData["Error"] = "Không thể xoá vì bệnh nhân còn lịch hẹn hoặc hồ sơ bệnh án. "
                                  + "Vui lòng huỷ/xoá chúng trước.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var p = await _db.Patients.FindAsync(id);
            if (p != null)
            {
                _db.Patients.Remove(p);
                await _db.SaveChangesAsync();
                TempData["Msg"] = "Đã xoá bệnh nhân.";
            }
            return RedirectToAction(nameof(Index));
        }

        // Tuỳ chọn: Xoá cưỡng bức (chỉ Admin) — XOÁ SẠCH DỮ LIỆU LIÊN QUAN
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ForceDelete(int id)
        {
            // Xoá lịch hẹn
            var apps = await _db.AppointmentSchedules.Where(a => a.PatientId == id).ToListAsync();
            _db.AppointmentSchedules.RemoveRange(apps);

            // Xoá hồ sơ bệnh án (các bảng con đã cascade theo cấu hình)
            var records = await _db.MedicalRecords.Where(m => m.PatientId == id).ToListAsync();
            _db.MedicalRecords.RemoveRange(records);

            await _db.SaveChangesAsync();

            var p = await _db.Patients.FindAsync(id);
            if (p != null)
            {
                _db.Patients.Remove(p);
                await _db.SaveChangesAsync();
                TempData["Msg"] = "Đã xoá bệnh nhân và toàn bộ dữ liệu liên quan.";
            }
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Nạp danh sách Users cho dropdown:
        /// - Mặc định: chỉ hiển thị user CHƯA có Patient.
        /// - Khi Edit: vẫn hiển thị user đang được gắn với patient hiện tại.
        /// </summary>
        private async Task LoadUsersAsync(string? selectedUserId = null, int? currentPatientId = null)
        {
            // map UserId -> PatientId
            var used = await _db.Patients
                .Select(p => new { p.Id, p.UserId })
                .ToListAsync();

            var usedByOther = used
                .Where(x => x.UserId != null && (!currentPatientId.HasValue || x.Id != currentPatientId.Value))
                .Select(x => x.UserId!)
                .ToHashSet();

            var users = await _db.Users.AsNoTracking()
                .Select(u => new { u.Id, Display = (u.UserName ?? u.Email ?? u.Id) })
                .Where(u => !usedByOther.Contains(u.Id) || u.Id == selectedUserId)
                .OrderBy(u => u.Display)
                .ToListAsync();

            ViewData["UserId"] = new SelectList(users, "Id", "Display", selectedUserId);
        }
    }
}
