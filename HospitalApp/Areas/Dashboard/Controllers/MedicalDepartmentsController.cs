using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HospitalApp.Data;
using HospitalApp.Models;
using HospitalApp.ViewModels;

namespace HospitalApp.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Admin,Doctor")]
    public class MedicalDepartmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public MedicalDepartmentsController(ApplicationDbContext context) => _context = context;

        // GET: Dashboard/MedicalDepartments
        public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 10)
        {
            var q = _context.MedicalDepartments
                .AsNoTracking()
                .AsQueryable();

            // ===== SEARCH =====
            if (!string.IsNullOrWhiteSpace(search))
            {
                var k = search.Trim();

                q = q.Where(d =>
                    (d.Name != null && d.Name.Contains(k)) ||
                    (d.Description != null && d.Description.Contains(k))
                );

                ViewData["Search"] = k;
            }

            // ===== PAGING =====
            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var totalItems = await q.CountAsync();

            var items = await q
                .OrderBy(d => d.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vm = new MedicalDepartmentIndexViewModel
            {
                Items = items,
                PagingInfo = new PagingInfo
                {
                    PageIndex = page,
                    PageSize = pageSize,
                    TotalItems = totalItems,
                    TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
                },
                Search = search
            };

            return View(vm);
        }

        public IActionResult Create() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description")] MedicalDepartment md)
        {
            if (!ModelState.IsValid) return View(md);
            _context.Add(md);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var md = await _context.MedicalDepartments.FindAsync(id);
            return md == null ? NotFound() : View(md);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description")] MedicalDepartment md)
        {
            if (id != md.Id) return NotFound();
            if (!ModelState.IsValid) return View(md);
            _context.Update(md);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var md = await _context.MedicalDepartments
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            return md == null ? NotFound() : View(md);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var md = await _context.MedicalDepartments
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            return md == null ? NotFound() : View(md);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var md = await _context.MedicalDepartments.FindAsync(id);

            if (md != null)
            {
                _context.MedicalDepartments.Remove(md);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
