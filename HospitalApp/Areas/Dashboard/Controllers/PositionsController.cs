using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HospitalApp.Data;
using HospitalApp.Models;

namespace HospitalApp.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Admin,Doctor")]
    public class PositionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public PositionsController(ApplicationDbContext context) => _context = context;

        // ================== INDEX + SEARCH + PAGINATION ==================
        public async Task<IActionResult> Index(string? search, int page = 1)
        {
            int pageSize = 10; // số dòng mỗi trang

            var q = _context.Positions
                .AsNoTracking()
                .AsQueryable();

            // ===== TÌM KIẾM =====
            if (!string.IsNullOrWhiteSpace(search))
            {
                var k = search.Trim();

                if (int.TryParse(k, out var id))
                    q = q.Where(p => p.Id == id || p.Name.Contains(k));
                else
                    q = q.Where(p => p.Name.Contains(k));

                ViewData["Search"] = k;
            }

            // ===== PHÂN TRANG =====
            int totalItems = await q.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var data = await q
                .OrderBy(p => p.Name) // sắp xếp để không bị nhảy trang
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;

            return View(data);
        }

        // ================== CRUD ==================

        public IActionResult Create() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Id")] Position position)
        {
            if (!ModelState.IsValid) return View(position);
            _context.Add(position);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var position = await _context.Positions.FindAsync(id);
            return position == null ? NotFound() : View(position);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] Position position)
        {
            if (id != position.Id) return NotFound();
            if (!ModelState.IsValid) return View(position);
            _context.Update(position);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var position = await _context.Positions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return position == null ? NotFound() : View(position);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var position = await _context.Positions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return position == null ? NotFound() : View(position);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var position = await _context.Positions.FindAsync(id);
            if (position != null)
            {
                try
                {
                    _context.Positions.Remove(position);
                    await _context.SaveChangesAsync();
                    TempData["Msg"] = "Đã xoá chức vụ.";
                }
                catch (DbUpdateException)
                {
                    TempData["Error"] = "Không thể xoá do chức vụ đang được sử dụng bởi nhân viên/bác sĩ.";
                }
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
