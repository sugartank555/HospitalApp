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
            if (uid == null) return Challenge();

            // Lấy user để hiển thị email/phone
            var user = await _userManager.FindByIdAsync(uid);

            // Lấy Patient + tạo nếu chưa có (gán FullName mặc định từ tài khoản)
            var p = await _db.Patients
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.UserId == uid);

            if (p == null)
            {
                var displayName = !string.IsNullOrWhiteSpace(user?.UserName)
                                    ? user!.UserName!
                                    : (!string.IsNullOrWhiteSpace(user?.Email) ? user!.Email! : "Khách");
                p = new Patient
                {
                    UserId = uid,
                    FullName = displayName
                };
                _db.Patients.Add(p);
                await _db.SaveChangesAsync();
            }

            ViewBag.AccountEmail = user?.Email ?? user?.UserName ?? "(không có)";
            ViewBag.PhoneNumber = user?.PhoneNumber ?? "";
            return View(p);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Patient model, string? phoneNumber)
        {
            // chỉ cập nhật các field Patient, không động vào navigation User
            ModelState.Remove("User");

            var uid = _userManager.GetUserId(User);
            if (uid == null) return Challenge();

            if (!ModelState.IsValid) return View(model);

            var p = await _db.Patients.FirstOrDefaultAsync(x => x.UserId == uid);
            if (p == null) return NotFound();

            // Cập nhật Patient (giữ an toàn các trường hiện có)
            p.FullName = string.IsNullOrWhiteSpace(model.FullName) ? p.FullName : model.FullName!.Trim();
            p.DateOfBirth = model.DateOfBirth;
            p.Ethnic = model.Ethnic;

            // Tuỳ chọn: cập nhật PhoneNumber của tài khoản Identity
            if (!string.IsNullOrWhiteSpace(phoneNumber))
            {
                var user = await _userManager.FindByIdAsync(uid);
                if (user != null && user.PhoneNumber != phoneNumber)
                {
                    user.PhoneNumber = phoneNumber.Trim();
                    // Nếu bạn dùng xác minh số ĐT, có thể dùng SetPhoneNumberAsync; ở đây cập nhật trực tiếp
                    await _userManager.UpdateAsync(user);
                }
            }

            await _db.SaveChangesAsync();
            TempData["Msg"] = "Đã lưu hồ sơ.";
            return RedirectToAction(nameof(Edit));
        }
    }
}
