using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HospitalApp.Data;

namespace HospitalApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;
        public HomeController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            try
            {
                // 6 bác sĩ & 6 dịch vụ mới nhất
                ViewBag.Doctors = await _db.Doctors
                    .AsNoTracking()
                    .OrderByDescending(d => d.Id)
                    .Take(6)
                    .ToListAsync();

                ViewBag.Services = await _db.Services
                    .Include(s => s.MedicalDepartment)
                    .AsNoTracking()
                    .OrderByDescending(s => s.Id)
                    .Take(6)
                    .ToListAsync();

                // ==== Tổng số liệu hiển thị ở hero ====
                ViewBag.TotalDoctors = await _db.Doctors.AsNoTracking().CountAsync();
                ViewBag.TotalPatients = await _db.Patients.AsNoTracking().CountAsync();
                ViewBag.TotalAppts = await _db.AppointmentSchedules.AsNoTracking().CountAsync();
            }
            catch (Exception ex)
            {
                TempData["HomeError"] = "Không thể tải dữ liệu trang chủ: " + ex.Message;
                // Phòng rỗng tránh lỗi view
                ViewBag.Doctors = new List<object>();
                ViewBag.Services = new List<object>();
                ViewBag.TotalDoctors = 0;
                ViewBag.TotalPatients = 0;
                ViewBag.TotalAppts = 0;
            }

            return View();
        }
    }
}
