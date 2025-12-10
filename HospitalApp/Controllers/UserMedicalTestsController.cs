using HospitalApp.Data;
using HospitalApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalApp.Controllers
{
    [Authorize]
    public class UserMedicalTestsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserMedicalTestsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }


        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var patient = await _db.Patients.FirstOrDefaultAsync(x => x.UserId == user.Id);
            if (patient == null) return Unauthorized();

            var tests = await _db.MedicalTests
                .Include(t => t.MedicalRecord)
                .Where(t => t.MedicalRecord.PatientId == patient.Id)
                .OrderByDescending(t => t.TestTime)
                .AsNoTracking()
                .ToListAsync();

            return View(tests);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var m = await _db.MedicalTests

                // Load hồ sơ khám
                .Include(t => t.MedicalRecord)
                    .ThenInclude(m => m.Patient)
                .Include(t => t.MedicalRecord)
                    .ThenInclude(m => m.Doctor)

                // ⭐ Load ĐƠN THUỐC của xét nghiệm
                .Include(t => t.Prescription)
                    .ThenInclude(p => p.Medicines)
                        .ThenInclude(mp => mp.Medicine)

                // Load dịch vụ
                .Include(t => t.Services)
                    .ThenInclude(s => s.Service)

                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            return m == null ? NotFound() : View(m);
        }

    }
}
