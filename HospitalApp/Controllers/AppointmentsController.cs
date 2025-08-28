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
            ViewBag.Doctors = await _db.Doctors.AsNoTracking().OrderBy(x => x.FullName).ToListAsync();
            return View();
        }

        // POST: /Appointments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DateTime date, string timeFrame, int doctorId)
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
                ViewBag.Doctors = await _db.Doctors.AsNoTracking().OrderBy(x => x.FullName).ToListAsync();
                ViewBag.SelectedDoctorId = doctorId;
                ViewBag.Date = date.ToString("yyyy-MM-dd");
                ViewBag.TimeFrame = timeFrame;
                return View();
            }

            // --------- Đảm bảo có Patient gắn với user & có FullName hợp lệ ----------
            var patientId = await EnsurePatientProfileAsync(user);

            // --------- Chặn trùng lịch theo Bác sĩ + Ngày + Khung giờ (khác Hủy) ----------
            var tf = timeFrame.Trim();
            bool exists = await _db.AppointmentSchedules.AnyAsync(x =>
                x.DoctorId == doctorId
                && x.Date.Date == date.Date
                && x.TimeFrame == tf
                && x.Status != AppointmentStatus.Cancelled);

            if (exists)
            {
                ModelState.AddModelError("", "Khung giờ này đã kín. Vui lòng chọn khung khác.");
                ViewBag.Doctors = await _db.Doctors.AsNoTracking().OrderBy(x => x.FullName).ToListAsync();
                ViewBag.SelectedDoctorId = doctorId;
                ViewBag.Date = date.ToString("yyyy-MM-dd");
                ViewBag.TimeFrame = timeFrame;
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

            // Nếu chưa có hồ sơ thì trả danh sách rỗng
            var data = await _db.AppointmentSchedules
                .Include(x => x.Doctor).ThenInclude(d => d.Position)
                .Where(x => x.PatientId == patientId)
                .OrderByDescending(x => x.Date)
                .AsNoTracking()
                .ToListAsync();

            return View(data);
        }

        // ================= Helpers =================

        /// <summary>
        /// Lấy (hoặc tạo) Patient theo User hiện tại và đảm bảo luôn có FullName (tránh lỗi DB NOT NULL).
        /// </summary>
        private async Task<int> EnsurePatientProfileAsync(ApplicationUser user)
        {
            var patient = await _db.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
            string displayName = !string.IsNullOrWhiteSpace(user.UserName)
                                    ? user.UserName!
                                    : (!string.IsNullOrWhiteSpace(user.Email) ? user.Email! : "Khách");

            if (patient == null)
            {
                patient = new Patient
                {
                    UserId = user.Id,
                    FullName = displayName
                };
                _db.Patients.Add(patient);
                await _db.SaveChangesAsync();
            }
            else if (string.IsNullOrWhiteSpace(patient.FullName))
            {
                patient.FullName = displayName;
                await _db.SaveChangesAsync();
            }

            return patient.Id;
        }
    }
}
