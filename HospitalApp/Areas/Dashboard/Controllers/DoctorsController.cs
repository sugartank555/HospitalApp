using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HospitalApp.Data;
using HospitalApp.Models;
using HospitalApp.Models.Enums;

[Area("Dashboard")]
[Authorize(Roles = "Admin,Doctor")]
public class DoctorsController : Controller
{
    private readonly ApplicationDbContext _context;
    public DoctorsController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var q = _context.Doctors.Include(d => d.Position).Include(d => d.Expertise).AsNoTracking();
        return View(await q.ToListAsync());
    }

    // GET: Create
    public IActionResult Create()
    {
        LoadDropdowns();
        return View();
    }

    // POST: Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("FullName,PhoneNumber,PositionId,ExpertiseId,UserId")] Doctor doctor)
    {
        doctor.StaffType = StaffType.Doctor;

        if (!ModelState.IsValid)
        {
            LoadDropdowns(doctor.PositionId, doctor.ExpertiseId);
            return View(doctor);
        }

        try
        {
            _context.Add(doctor);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex)
        {
            // Hiển thị thông báo lỗi DB (FK, null, …) ra màn hình
            ModelState.AddModelError(string.Empty, ex.InnerException?.Message ?? ex.Message);
            LoadDropdowns(doctor.PositionId, doctor.ExpertiseId);
            return View(doctor);
        }
    }

    // GET: Edit
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var doctor = await _context.Doctors.FindAsync(id);
        if (doctor == null) return NotFound();

        LoadDropdowns(doctor.PositionId, doctor.ExpertiseId);
        return View(doctor);
    }

    // POST: Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,FullName,PhoneNumber,PositionId,ExpertiseId,UserId,StaffType")] Doctor doctor)
    {
        if (id != doctor.Id) return NotFound();
        doctor.StaffType = StaffType.Doctor;

        if (!ModelState.IsValid)
        {
            LoadDropdowns(doctor.PositionId, doctor.ExpertiseId);
            return View(doctor);
        }

        try
        {
            _context.Update(doctor);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex)
        {
            ModelState.AddModelError(string.Empty, ex.InnerException?.Message ?? ex.Message);
            LoadDropdowns(doctor.PositionId, doctor.ExpertiseId);
            return View(doctor);
        }
    }

    private void LoadDropdowns(int? positionId = null, int? expertiseId = null)
    {
        ViewData["PositionId"] = new SelectList(_context.Positions.AsNoTracking(), "Id", "Name", positionId);
        ViewData["ExpertiseId"] = new SelectList(_context.Expertises.AsNoTracking(), "Id", "Name", expertiseId);
    }
}
