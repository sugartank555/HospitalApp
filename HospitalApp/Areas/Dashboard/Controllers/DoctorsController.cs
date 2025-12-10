using HospitalApp.Data;
using HospitalApp.Models;
using HospitalApp.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

[Area("Dashboard")]
[Authorize(Roles = "Admin")]
[Route("Dashboard/[controller]")]
[Route("Dashboard/[controller]/[action]")]
public class DoctorsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DoctorsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: /Dashboard/Doctors
    // GET: /Dashboard/Doctors
    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 10)
    {
        var q = _context.Doctors
            .Include(d => d.Position)
            .Include(d => d.Expertise)
            .Include(d => d.User)
            .AsNoTracking()
            .AsQueryable();

        // ===== TÌM KIẾM =====
        if (!string.IsNullOrWhiteSpace(search))
        {
            var k = search.Trim();

            q = q.Where(d =>
                (d.FullName != null && d.FullName.Contains(k)) ||
                (d.PhoneNumber != null && d.PhoneNumber.Contains(k)) ||
                (d.Position != null && d.Position.Name != null && d.Position.Name.Contains(k)) ||
                (d.Expertise != null && d.Expertise.Name != null && d.Expertise.Name.Contains(k)) ||
                (d.User != null && (
                    (d.User.UserName != null && d.User.UserName.Contains(k)) ||
                    (d.User.Email != null && d.User.Email.Contains(k))
                ))
            );

            ViewData["Search"] = k;
        }

        // ===== PHÂN TRANG =====
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        int totalItems = await q.CountAsync();

        var items = await q
            .OrderBy(d => d.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var vm = new HospitalApp.ViewModels.DoctorIndexViewModel
        {
            Items = items,
            Search = search,
            PagingInfo = new HospitalApp.ViewModels.PagingInfo
            {
                PageIndex = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            }
        };

        return View(vm);
    }

    // ===== CREATE =====
    [HttpGet("Create")]
    public IActionResult Create()
    {
        LoadDropdowns();
        return View();
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("FullName,PhoneNumber,PositionId,ExpertiseId")] Doctor doctor)
    {
        doctor.StaffType = StaffType.Doctor;

        if (!ModelState.IsValid)
        {
            LoadDropdowns(doctor.PositionId, doctor.ExpertiseId);
            return View(doctor);
        }

        var username = (doctor.PhoneNumber ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(username))
        {
            ModelState.AddModelError(nameof(doctor.PhoneNumber), "Vui lòng nhập số điện thoại (dùng làm tài khoản).");
            LoadDropdowns(doctor.PositionId, doctor.ExpertiseId);
            return View(doctor);
        }

        var email = $"{username}@hospital.local".ToLowerInvariant();

        if (await _userManager.FindByNameAsync(username) != null)
            ModelState.AddModelError(string.Empty, "Tên đăng nhập (UserName) đã tồn tại.");
        if (await _userManager.FindByEmailAsync(email) != null)
            ModelState.AddModelError(string.Empty, "Email đã tồn tại trong hệ thống.");

        if (!ModelState.IsValid)
        {
            LoadDropdowns(doctor.PositionId, doctor.ExpertiseId);
            return View(doctor);
        }

        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            var user = new ApplicationUser
            {
                UserName = username,
                Email = email,
                PhoneNumber = doctor.PhoneNumber
            };
            const string defaultPassword = "Password@123";
            var createUser = await _userManager.CreateAsync(user, defaultPassword);
            if (!createUser.Succeeded)
            {
                foreach (var e in createUser.Errors) ModelState.AddModelError(string.Empty, e.Description);
                LoadDropdowns(doctor.PositionId, doctor.ExpertiseId);
                return View(doctor);
            }

            var addRole = await _userManager.AddToRoleAsync(user, "Doctor");
            if (!addRole.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                foreach (var e in addRole.Errors) ModelState.AddModelError(string.Empty, e.Description);
                LoadDropdowns(doctor.PositionId, doctor.ExpertiseId);
                return View(doctor);
            }

            doctor.UserId = user.Id;
            _context.Doctors.Add(doctor);
            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            TempData["Success"] = "Tạo bác sĩ thành công. Tài khoản mặc định: SĐT / Password@123";
            return RedirectToAction("Index", "Doctors", new { area = "Dashboard" });
        }
        catch (DbUpdateException ex)
        {
            await tx.RollbackAsync();
            ModelState.AddModelError(string.Empty, "Lỗi cơ sở dữ liệu: " + (ex.InnerException?.Message ?? ex.Message));
            LoadDropdowns(doctor.PositionId, doctor.ExpertiseId);
            return View(doctor);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            ModelState.AddModelError(string.Empty, "Lỗi không xác định: " + ex.Message);
            LoadDropdowns(doctor.PositionId, doctor.ExpertiseId);
            return View(doctor);
        }
    }

    // ===== DETAILS =====
    [HttpGet("Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var doctor = await _context.Doctors
            .Include(d => d.Position)
            .Include(d => d.Expertise)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);

        if (doctor == null) return NotFound();
        return View(doctor);
    }

    // ===== EDIT =====
    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var doctor = await _context.Doctors.FindAsync(id);
        if (doctor == null) return NotFound();

        LoadDropdowns(doctor.PositionId, doctor.ExpertiseId);
        return View(doctor);
    }

    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,FullName,PhoneNumber,PositionId,ExpertiseId,UserId,StaffType")] Doctor doctor)
    {
        if (id != doctor.Id) return NotFound();

        // đảm bảo vẫn là Doctor
        doctor.StaffType = StaffType.Doctor;

        if (!ModelState.IsValid)
        {
            LoadDropdowns(doctor.PositionId, doctor.ExpertiseId);
            return View(doctor);
        }

        try
        {
            _context.Entry(doctor).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật bác sĩ thành công.";
            return RedirectToAction("Index", "Doctors", new { area = "Dashboard" });
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await DoctorExists(doctor.Id)) return NotFound();
            throw;
        }
        catch (DbUpdateException ex)
        {
            ModelState.AddModelError(string.Empty, "Lỗi cơ sở dữ liệu: " + (ex.InnerException?.Message ?? ex.Message));
            LoadDropdowns(doctor.PositionId, doctor.ExpertiseId);
            return View(doctor);
        }
    }

    // ===== DELETE =====
    [HttpGet("Delete/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var doctor = await _context.Doctors
            .Include(d => d.Position)
            .Include(d => d.Expertise)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);

        if (doctor == null) return NotFound();
        return View(doctor);
    }

    [HttpPost("Delete/{id:int}"), ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var doctor = await _context.Doctors.FindAsync(id);
        if (doctor == null) return NotFound();

        try
        {
            // (Tuỳ bạn) Có thể cân nhắc xử lý tài khoản Identity liên quan:
            // var user = await _userManager.FindByIdAsync(doctor.UserId);
            // if (user != null) await _userManager.DeleteAsync(user);

            _context.Doctors.Remove(doctor);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã xoá bác sĩ.";
            return RedirectToAction("Index", "Doctors", new { area = "Dashboard" });
        }
        catch (DbUpdateException ex)
        {
            TempData["Error"] = "Không thể xoá: " + (ex.InnerException?.Message ?? ex.Message);
            return RedirectToAction("Index", "Doctors", new { area = "Dashboard" });
        }
    }

    private void LoadDropdowns(int? positionId = null, int? expertiseId = null)
    {
        ViewData["PositionId"] = new SelectList(_context.Positions.AsNoTracking(), "Id", "Name", positionId);
        ViewData["ExpertiseId"] = new SelectList(_context.Expertises.AsNoTracking(), "Id", "Name", expertiseId);
    }

    private async Task<bool> DoctorExists(int id) =>
        await _context.Doctors.AnyAsync(e => e.Id == id);
}
