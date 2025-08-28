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
            // Lấy nhanh 6 bác sĩ & 6 dịch vụ nổi bật (theo Id mới nhất)
            ViewBag.Doctors = await _db.Doctors.AsNoTracking().OrderByDescending(d => d.Id).Take(6).ToListAsync();
            ViewBag.Services = await _db.Services.Include(s => s.MedicalDepartment).AsNoTracking()
                                   .OrderByDescending(s => s.Id).Take(6).ToListAsync();
            return View();
        }
    }
}
