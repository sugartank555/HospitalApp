using HospitalApp.Data;
using HospitalApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalApp.Controllers
{
    [Authorize]
    public class MedicalRecordsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public MedicalRecordsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // ===================== DANH SÁCH HỒ SƠ =======================
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var patient = await _db.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (patient == null) return NotFound("Không tìm thấy thông tin bệnh nhân.");

            var records = await _db.MedicalRecords
                .Where(r => r.PatientId == patient.Id)
                .OrderByDescending(r => r.Time)
                .ToListAsync();

            return View(records);
        }

        // ===================== CHI TIẾT HỒ SƠ =======================
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var patient = await _db.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (patient == null) return NotFound("Không tìm thấy thông tin bệnh nhân.");

            var record = await _db.MedicalRecords
                .Include(r => r.Information)
                .Include(r => r.MedicalTests)
                    .ThenInclude(t => t.Services)
                        .ThenInclude(s => s.Service)
                .Include(r => r.Prescriptions)
                    .ThenInclude(p => p.Medicines)
                        .ThenInclude(m => m.Medicine)
                .Include(r => r.Doctor)
                .FirstOrDefaultAsync(r => r.Id == id && r.PatientId == patient.Id);

            if (record == null) return NotFound("Hồ sơ không tồn tại.");

            // ---------- TÍNH TỔNG XÉT NGHIỆM ----------
            decimal totalTest = record.MedicalTests?
                .Sum(t => t.TotalPrice) ?? 0m;

            // ---------- TÍNH TỔNG THUỐC ----------
            decimal totalMedicine = 0m;

            foreach (var pre in record.Prescriptions)
            {
                foreach (var item in pre.Medicines)
                {
                    if (item.Medicine == null) continue;           // FIX NULL
                    var price = item.Medicine.Price;
                    totalMedicine += price * item.Quantity;
                }
            }

            ViewBag.TotalTest = totalTest;
            ViewBag.TotalMedicine = totalMedicine;
            ViewBag.GrandTotal = totalTest + totalMedicine;

            return View(record);
        }

    }
}
