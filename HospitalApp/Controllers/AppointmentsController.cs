using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HospitalApp.Data;
using HospitalApp.Models;
using HospitalApp.Models.Enums;

namespace HospitalApp.Controllers
{
    [Authorize] // Yêu cầu đăng nhập
    public class AppointmentsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userMgr;

        public AppointmentsController(ApplicationDbContext db, UserManager<ApplicationUser> userMgr)
        {
            _db = db; _userMgr = userMgr;
        }

        // GET: /Appointments/Create
        public async Task<IActionResult> Create()
        {
            var user = await _userMgr.GetUserAsync(User);
            var patient = user != null
                ? await _db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == user.Id)
                : null;

            ViewBag.Doctors = await _db.Doctors.AsNoTracking().OrderBy(x => x.FullName).ToListAsync();
            ViewBag.PatientName = patient?.FullName ?? (user?.UserName ?? user?.Email ?? "");
            return View();
        }

        // POST: /Appointments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DateTime date, string timeFrame, int doctorId, string? patientName)
        {
            var user = await _userMgr.GetUserAsync(User);
            if (user == null) return Challenge();

            // --------- Validate đầu vào cơ bản ----------
            if (doctorId <= 0)
                ModelState.AddModelError(nameof(doctorId), "Vui lòng chọn bác sĩ.");
            if (string.IsNullOrWhiteSpace(timeFrame))
                ModelState.AddModelError(nameof(timeFrame), "Vui lòng nhập khung giờ (ví dụ 08:00-08:30).");
            if (date.Date < DateTime.Today)
                ModelState.AddModelError(nameof(date), "Ngày khám phải từ hôm nay trở đi.");

            // Nếu không hợp lệ → trả lại view với dữ liệu đã nhập
            if (!ModelState.IsValid)
            {
                await FillCreateViewBagsAsync(doctorId, date, timeFrame, patientName);
                return View();
            }

            // --------- Đảm bảo có Patient gắn với user & cập nhật tên nếu được nhập ----------
            var patientId = await EnsurePatientProfileAsync(user, patientName);

            // --------- Chặn trùng lịch (Bác sĩ + Ngày + Khung giờ; bỏ qua lịch Cancelled) ----------
            var tf = timeFrame.Trim();
            bool exists = await _db.AppointmentSchedules.AnyAsync(x =>
                x.DoctorId == doctorId
                && x.Date.Date == date.Date
                && x.TimeFrame == tf
                && x.Status != AppointmentStatus.Cancelled);

            if (exists)
            {
                ModelState.AddModelError("", "Khung giờ này đã kín. Vui lòng chọn khung khác.");
                await FillCreateViewBagsAsync(doctorId, date, timeFrame, patientName);
                return View();
            }

            // --------- Tạo lịch ----------
            var appt = new AppointmentSchedule
            {
                Date = date.Date,
                TimeFrame = tf,
                DoctorId = doctorId,
                PatientId = patientId,
                Status = AppointmentStatus.Pending
            };

            _db.AppointmentSchedules.Add(appt);
            await _db.SaveChangesAsync();

            TempData["ok"] = "Đã gửi yêu cầu. Vui lòng chờ xác nhận.";
            return RedirectToAction(nameof(My));
        }

        // GET: /Appointments/My
        public async Task<IActionResult> My()
        {
            var user = await _userMgr.GetUserAsync(User);
            if (user == null) return Challenge();

            var patientId = await _db.Patients
                .Where(p => p.UserId == user.Id)
                .Select(p => p.Id)
                .FirstOrDefaultAsync();

            var data = Enumerable.Empty<AppointmentSchedule>();
            if (patientId != 0)
            {
                data = await _db.AppointmentSchedules
                    .Include(x => x.Doctor).ThenInclude(d => d.Position)
                    .Where(x => x.PatientId == patientId)
                    .OrderByDescending(x => x.Date)
                    .AsNoTracking()
                    .ToListAsync();
            }

            return View(data);
        }

        // ================= Helpers =================

        private async Task FillCreateViewBagsAsync(int doctorId, DateTime date, string timeFrame, string? patientName)
        {
            ViewBag.Doctors = await _db.Doctors.AsNoTracking().OrderBy(x => x.FullName).ToListAsync();
            ViewBag.SelectedDoctorId = doctorId;
            ViewBag.Date = date.ToString("yyyy-MM-dd");
            ViewBag.TimeFrame = timeFrame;
            ViewBag.PatientName = patientName ?? "";
        }

        /// <summary>
        /// Lấy (hoặc tạo) Patient theo User hiện tại. Có thể cập nhật FullName nếu người dùng nhập.
        /// Bản an toàn chống trùng IX_Patients_UserId do race-condition.
        /// </summary>
        private async Task<int> EnsurePatientProfileAsync(ApplicationUser user, string? newDisplayName)
        {
            var patient = await _db.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);

            string fallback = !string.IsNullOrWhiteSpace(user.UserName)
                                ? user.UserName!
                                : (!string.IsNullOrWhiteSpace(user.Email) ? user.Email! : "Khách");

            string desiredName = !string.IsNullOrWhiteSpace(newDisplayName)
                                    ? newDisplayName!.Trim()
                                    : (patient?.FullName ?? fallback);

            if (patient != null)
            {
                if (string.IsNullOrWhiteSpace(patient.FullName) ||
                    (!string.IsNullOrWhiteSpace(newDisplayName) && !string.Equals(patient.FullName, desiredName, StringComparison.Ordinal)))
                {
                    patient.FullName = desiredName;
                    await _db.SaveChangesAsync();
                }
                return patient.Id;
            }

            // Chưa có -> tạo mới (có thể xảy ra race)
            patient = new Patient
            {
                UserId = user.Id,
                FullName = desiredName
            };
            _db.Patients.Add(patient);
            try
            {
                await _db.SaveChangesAsync();
                return patient.Id;
            }
            catch (DbUpdateException ex) when (
                ex.InnerException is Microsoft.Data.SqlClient.SqlException sql &&
                sql.Message.Contains("IX_Patients_UserId"))
            {
                // Bị tạo đồng thời ở request khác -> lấy lại và cập nhật tên nếu cần
                var existing = await _db.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
                if (existing == null) throw;

                if (!string.IsNullOrWhiteSpace(newDisplayName) &&
                    !string.Equals(existing.FullName, desiredName, StringComparison.Ordinal))
                {
                    existing.FullName = desiredName;
                    await _db.SaveChangesAsync();
                }
                return existing.Id;
            }
        }
    }
}
