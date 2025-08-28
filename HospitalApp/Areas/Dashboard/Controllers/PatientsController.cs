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
    public class PatientsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public PatientsController(ApplicationDbContext db) => _db = db;

        // GET: /Dashboard/Patients
        public async Task<IActionResult> Index()
        {
            var data = await _db.Patients
                .Include(p => p.User)
                .AsNoTracking()
                .ToListAsync();
            return View(data);
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
            // tránh MVC validate navigation
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
                // Phòng trường hợp race-condition
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

            // Cập nhật các trường scalar (giữ đơn giản: set values)
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
            return p == null ? NotFound() : View(p);
        }

        // POST: /Dashboard/Patients/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var p = await _db.Patients.FindAsync(id);
            if (p != null)
            {
                _db.Patients.Remove(p);
                await _db.SaveChangesAsync();
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
