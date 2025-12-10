using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HospitalApp.Data;
using HospitalApp.Models;

namespace HospitalApp.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Admin,Doctor")]
    public class ExpertisesController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ExpertisesController(ApplicationDbContext context) => _context = context;

        // ================== INDEX + SEARCH + PAGINATION ==================
        public async Task<IActionResult> Index(string? search, int page = 1)
        {
            int pageSize = 10; // số dòng mỗi trang

            var q = _context.Expertises
                .AsNoTracking()
                .AsQueryable();

            // ===== TÌM KIẾM =====
            if (!string.IsNullOrWhiteSpace(search))
            {
                var k = search.Trim();

                if (int.TryParse(k, out var id))
                    q = q.Where(e => e.Id == id || e.Name.Contains(k));
                else
                    q = q.Where(e => e.Name.Contains(k));

                ViewData["Search"] = k;
            }

            // ===== PHÂN TRANG =====
            int totalItems = await q.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var data = await q
                .OrderBy(e => e.Name) // luôn sắp xếp để tránh nhảy trang
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
        public async Task<IActionResult> Create([Bind("Name,Id")] Expertise expertise)
        {
            if (!ModelState.IsValid) return View(expertise);
            _context.Add(expertise);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var expertise = await _context.Expertises.FindAsync(id);
            return expertise == null ? NotFound() : View(expertise);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] Expertise expertise)
        {
            if (id != expertise.Id) return NotFound();
            if (!ModelState.IsValid) return View(expertise);
            _context.Update(expertise);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var expertise = await _context.Expertises.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return expertise == null ? NotFound() : View(expertise);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var expertise = await _context.Expertises.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return expertise == null ? NotFound() : View(expertise);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var expertise = await _context.Expertises.FindAsync(id);
            if (expertise != null)
            {
                _context.Expertises.Remove(expertise);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
