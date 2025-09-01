using HospitalApp.Data;
using HospitalApp.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Area("Dashboard")]
[Authorize(Roles = "Admin,Doctor")]
public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;
    public HomeController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;
        var from14 = today.AddDays(-13);
        var from30 = today.AddDays(-29);

        // KPI
        ViewBag.Today = await _db.AppointmentSchedules.CountAsync(a => a.Date.Date == today);
        ViewBag.Pending = await _db.AppointmentSchedules.CountAsync(a => a.Status == AppointmentStatus.Pending);
        ViewBag.Cancelled7d = await _db.AppointmentSchedules.CountAsync(a => a.Status == AppointmentStatus.Cancelled
                                                                            && a.Date.Date >= today.AddDays(-6)
                                                                            && a.Date.Date <= today);
        ViewBag.TotalPatients = await _db.Patients.CountAsync();
        ViewBag.TotalDoctors = await _db.Doctors.CountAsync();
        ViewBag.TotalServices = await _db.Services.CountAsync();

        // Series 14 ngày (labels + values)
        var raw14 = await _db.AppointmentSchedules
            .Where(a => a.Date.Date >= from14 && a.Date.Date <= today)
            .GroupBy(a => a.Date.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        var labels14 = Enumerable.Range(0, 14)
            .Select(i => from14.AddDays(i).ToString("dd/MM"))
            .ToArray();
        var values14 = Enumerable.Range(0, 14)
            .Select(i => raw14.FirstOrDefault(x => x.Date == from14.AddDays(i))?.Count ?? 0)
            .ToArray();

        ViewBag.Labels14 = labels14;
        ViewBag.Values14 = values14;

        // Phân bố trạng thái
        var status = await _db.AppointmentSchedules
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        ViewBag.StatusLabels = status.Select(x => x.Status.ToString()).ToArray();
        ViewBag.StatusValues = status.Select(x => x.Count).ToArray();

        // Lịch sắp tới (10)
        ViewBag.Upcoming10 = await _db.AppointmentSchedules
            .Include(a => a.Doctor)
            .Include(a => a.Patient)
            .Where(a => a.Date.Date >= today)
            .OrderBy(a => a.Date).ThenBy(a => a.TimeFrame)
            .Take(10)
            .AsNoTracking()
            .ToListAsync();

        // Top bác sĩ 30 ngày
        ViewBag.TopDoctors = await _db.AppointmentSchedules
            .Where(a => a.Date.Date >= from30 && a.Date.Date <= today)
            .GroupBy(a => new { a.DoctorId, a.Doctor.FullName })
            .Select(g => new { g.Key.DoctorId, Name = g.Key.FullName, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync();

        // Top chuyên môn 30 ngày
        ViewBag.TopExpertises = await _db.AppointmentSchedules
            .Where(a => a.Date.Date >= from30 && a.Date.Date <= today)
            .GroupBy(a => new { a.Doctor.ExpertiseId, Name = a.Doctor.Expertise!.Name })
            .Select(g => new { Id = g.Key.ExpertiseId, Name = (string)(g.Key.Name ?? "(Chưa gán)"), Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync();

        return View();
    }
}
