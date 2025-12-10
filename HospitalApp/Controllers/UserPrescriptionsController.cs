using HospitalApp.Data;
using HospitalApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HospitalApp.Controllers
{
    [Authorize]
    public class UserPrescriptionsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public UserPrescriptionsController(ApplicationDbContext db)
        {
            _db = db;
        }

        // ===========================
        // 1. Danh sách đơn thuốc 
        // ===========================
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var patient = await _db.Patients.FirstOrDefaultAsync(p => p.UserId == userId);

            if (patient == null)
                return Unauthorized();

            var prescriptions = await _db.Prescriptions
                .Include(p => p.Medicines)
                .ThenInclude(m => m.Medicine)
                .Include(p => p.MedicalRecord)
                .Where(p => p.MedicalRecord.PatientId == patient.Id)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(prescriptions);
        }

        // ===========================
        // 2. Chi tiết đơn thuốc 
        // ===========================
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var patient = await _db.Patients.FirstOrDefaultAsync(p => p.UserId == userId);

            if (patient == null)
                return Unauthorized();

            var prescription = await _db.Prescriptions
                .Include(p => p.Medicines)
                    .ThenInclude(m => m.Medicine)
                .Include(p => p.MedicalRecord)
                .FirstOrDefaultAsync(p =>
                    p.Id == id &&
                    p.MedicalRecord.PatientId == patient.Id
                );

            if (prescription == null)
                return NotFound();

            return View(prescription);
        }
    }
}
