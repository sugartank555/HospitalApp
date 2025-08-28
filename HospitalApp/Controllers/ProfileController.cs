using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HospitalApp.Data;
using HospitalApp.Models;

namespace HospitalApp.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileController(ApplicationDbContext db, UserManager<ApplicationUser> um)
        {
            _db = db;
            _userManager = um;
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var uid = _userManager.GetUserId(User);
            var p = await _db.Patients.FirstOrDefaultAsync(x => x.UserId == uid);
            if (p == null)
            {
                p = new Patient { UserId = uid };
                _db.Patients.Add(p);
                await _db.SaveChangesAsync();
            }
            return View(p);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Patient model)
        {
            // chỉ cập nhật các field Patient, không động vào navigation User
            ModelState.Remove("User");
            if (!ModelState.IsValid) return View(model);

            var uid = _userManager.GetUserId(User);
            var p = await _db.Patients.FirstOrDefaultAsync(x => x.UserId == uid);
            if (p == null) return NotFound();

            p.DateOfBirth = model.DateOfBirth;
            p.Ethnic = model.Ethnic;
            await _db.SaveChangesAsync();

            TempData["Msg"] = "Đã lưu hồ sơ.";
            return RedirectToAction(nameof(Edit));
        }
    }
}
