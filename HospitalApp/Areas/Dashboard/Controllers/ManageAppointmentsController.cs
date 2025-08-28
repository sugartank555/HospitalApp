using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HospitalApp.Data;
using HospitalApp.Models.Enums;

namespace HospitalApp.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Admin,Doctor")]
    public class ManageAppointmentsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public ManageAppointmentsController(ApplicationDbContext db) { _db = db; }

        public async Task<IActionResult> Index()
        {
            var list = await _db.AppointmentSchedules
                .Include(a => a.Patient).Include(a => a.Doctor)
                .OrderByDescending(a => a.Date)
                .ToListAsync();
            return View(list);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, AppointmentStatus status)
        {
            var appt = await _db.AppointmentSchedules.FindAsync(id);
            if (appt == null) return NotFound();
            appt.Status = status;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
