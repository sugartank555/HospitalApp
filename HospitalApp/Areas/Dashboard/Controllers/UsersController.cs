// Areas/Dashboard/Controllers/UsersController.cs
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HospitalApp.Models;

namespace HospitalApp.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: /Dashboard/Users
        // GET: /Dashboard/Users
        public async Task<IActionResult> Index(
            string? search,
            string? role,
            string? sortOrder,
            int page = 1,
            int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var users = _userManager.Users.AsNoTracking().AsQueryable();

            // --- Search ---
            if (!string.IsNullOrWhiteSpace(search))
            {
                var k = search.Trim();
                users = users.Where(u =>
                    (u.UserName != null && u.UserName.Contains(k)) ||
                    (u.Email != null && u.Email.Contains(k)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(k)));
            }

            // --- Filter by role ---
            if (!string.IsNullOrWhiteSpace(role))
            {
                var inRole = await _userManager.GetUsersInRoleAsync(role);
                var ids = inRole.Select(u => u.Id).ToHashSet();
                users = users.Where(u => ids.Contains(u.Id));
            }

            // --- Sort toggles for view ---
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSort"] = sortOrder == "name" ? "name_desc" : "name";
            ViewData["EmailSort"] = sortOrder == "email" ? "email_desc" : "email";
            ViewData["DateSort"] = sortOrder == "date" ? "date_desc" : "date";

            // --- Apply sort ---
            users = sortOrder switch
            {
                "name" => users.OrderBy(u => u.UserName!),
                "name_desc" => users.OrderByDescending(u => u.UserName!),
                "email" => users.OrderBy(u => u.Email!),
                "email_desc" => users.OrderByDescending(u => u.Email!),
                "date" => users.OrderBy(u => u.LockoutEnd),
                "date_desc" => users.OrderByDescending(u => u.LockoutEnd),
                _ => users.OrderBy(u => u.UserName!)
            };

            // --- Pagination (đếm trước khi Skip/Take) ---
            var totalItems = await users.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var pageUsers = await users
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // --- Nạp roles chỉ cho trang hiện tại ---
            var items = new List<UserRowVm>();
            foreach (var u in pageUsers)
            {
                var rolesOfUser = await _userManager.GetRolesAsync(u);
                items.Add(new UserRowVm
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    Email = u.Email,
                    Phone = u.PhoneNumber,
                    LockoutEnd = u.LockoutEnd,
                    Roles = rolesOfUser.ToList()
                });
            }

            var vm = new UsersIndexVm
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                Search = search,
                Role = role,
                SortOrder = sortOrder ?? string.Empty,
                AllRoles = await _roleManager.Roles.OrderBy(r => r.Name!).Select(r => r.Name!).ToListAsync()
            };

            return View(vm);
        }


        // GET: /Dashboard/Users/EditRoles/{id}
        public async Task<IActionResult> EditRoles(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var allRoles = await _roleManager.Roles.Select(r => r.Name!).OrderBy(n => n).ToListAsync();
            var userRoles = await _userManager.GetRolesAsync(user);

            var vm = new EditRolesVm
            {
                UserId = user.Id,
                UserName = user.UserName ?? user.Email ?? user.Id,
                Roles = allRoles.Select(r => new RoleCheckbox { Name = r, Selected = userRoles.Contains(r) }).ToList()
            };
            return View(vm);
        }

        // POST: /Dashboard/Users/EditRoles
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRoles(EditRolesVm vm)
        {
            var user = await _userManager.FindByIdAsync(vm.UserId);
            if (user == null) return NotFound();

            var current = await _userManager.GetRolesAsync(user);
            var target = vm.Roles.Where(x => x.Selected).Select(x => x.Name).ToArray();

            // remove old roles not in target
            var toRemove = current.Where(r => !target.Contains(r)).ToArray();
            if (toRemove.Length > 0) await _userManager.RemoveFromRolesAsync(user, toRemove);

            // add new roles
            var toAdd = target.Where(r => !current.Contains(r)).ToArray();
            if (toAdd.Length > 0) await _userManager.AddToRolesAsync(user, toAdd);

            TempData["Msg"] = "Cập nhật vai trò thành công.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Dashboard/Users/Lock
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Lock(string id, int days = 30)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value == id)
            {
                TempData["Error"] = "Bạn không thể khoá chính tài khoản đang đăng nhập.";
                return RedirectToAction(nameof(Index));
            }

            user.LockoutEnd = DateTimeOffset.UtcNow.AddDays(days);
            await _userManager.UpdateAsync(user);
            TempData["Msg"] = $"Đã khoá tài khoản đến {user.LockoutEnd:yyyy-MM-dd HH:mm} (UTC).";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Dashboard/Users/Unlock
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.LockoutEnd = null;
            await _userManager.UpdateAsync(user);
            TempData["Msg"] = "Đã mở khoá tài khoản.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Dashboard/Users/ResetPassword
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string id, string newPassword = "Password@123")
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var res = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (!res.Succeeded)
            {
                TempData["Error"] = string.Join("; ", res.Errors.Select(e => e.Description));
            }
            else
            {
                TempData["Msg"] = $"Đã đặt lại mật khẩu về: {newPassword}";
            }

            return RedirectToAction(nameof(Index));
        }

        // (Tuỳ chọn) Tạo nhanh user
        public IActionResult Create() => View(new CreateUserVm());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserVm vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var user = new ApplicationUser
            {
                UserName = vm.UserName?.Trim(),
                Email = vm.Email?.Trim(),
                PhoneNumber = vm.Phone
            };

            var create = await _userManager.CreateAsync(user, vm.Password);
            if (!create.Succeeded)
            {
                foreach (var e in create.Errors) ModelState.AddModelError(string.Empty, e.Description);
                return View(vm);
            }

            if (vm.Roles?.Any() == true)
            {
                var validRoles = vm.Roles.Where(r => _roleManager.RoleExistsAsync(r).Result).ToArray();
                if (validRoles.Length > 0) await _userManager.AddToRolesAsync(user, validRoles);
            }

            TempData["Msg"] = "Đã tạo tài khoản.";
            return RedirectToAction(nameof(Index));
        }

        // (Tuỳ chọn) Xoá user
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value == id)
            {
                TempData["Error"] = "Không thể xoá tài khoản đang đăng nhập.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                var res = await _userManager.DeleteAsync(user);
                TempData[res.Succeeded ? "Msg" : "Error"] =
                    res.Succeeded ? "Đã xoá tài khoản." : string.Join("; ", res.Errors.Select(e => e.Description));
            }
            return RedirectToAction(nameof(Index));
        }
    }

    // ===== ViewModels =====
    public class UserRowVm
    {
        public string Id { get; set; } = default!;
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public List<string> Roles { get; set; } = new();
    }

    public class EditRolesVm
    {
        public string UserId { get; set; } = default!;
        public string UserName { get; set; } = default!;
        public List<RoleCheckbox> Roles { get; set; } = new();
    }

    public class RoleCheckbox
    {
        public string Name { get; set; } = default!;
        public bool Selected { get; set; }
    }

    public class CreateUserVm
    {
        [Required, StringLength(64)] public string UserName { get; set; } = default!;
        [EmailAddress] public string? Email { get; set; }
        [Phone] public string? Phone { get; set; }
        [Required, StringLength(64, MinimumLength = 6)]
        public string Password { get; set; } = "Password@123";
        // nhập role bằng dấu phẩy, hoặc tạo UI checkbox riêng
        public List<string>? Roles { get; set; }
    }
    public class UsersIndexVm
    {
        public List<UserRowVm> Items { get; set; } = new();

        // Paging
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }

        // Filters / Sort
        public string? Search { get; set; }
        public string? Role { get; set; }
        public string SortOrder { get; set; } = string.Empty;

        // For dropdown
        public List<string> AllRoles { get; set; } = new();
    }

}
