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

        // GET: Dashboard/Positions
        // GET: Dashboard/Positions
        public async Task<IActionResult> Index(string? search)
        {
            var q = _context.Positions
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var k = search.Trim();

                // Nếu gõ số -> cho phép lọc theo ID hoặc theo tên chứa từ khóa
                if (int.TryParse(k, out var id))
                {
                    q = q.Where(p => p.Id == id || p.Name.Contains(k));
                }
                else
                {
                    q = q.Where(p => p.Name.Contains(k));
                    // Hoặc dùng Like nếu muốn:
                    // q = q.Where(p => EF.Functions.Like(p.Name, $"%{k}%"));
                }

                ViewData["Search"] = k; // giữ lại giá trị ô input
            }

            var data = await q.ToListAsync();
            return View(data);
        }


        // GET: Dashboard/Positions/Create
        public IActionResult Create() => View();

        // POST: Dashboard/Positions/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Id")] Position position)
        {
            if (!ModelState.IsValid) return View(position);
            _context.Add(position);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Dashboard/Positions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var position = await _context.Positions.FindAsync(id);
            return position == null ? NotFound() : View(position);
        }

        // POST: Dashboard/Positions/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] Position position)
        {
            if (id != position.Id) return NotFound();
            if (!ModelState.IsValid) return View(position);
            _context.Update(position);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Dashboard/Positions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var position = await _context.Positions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return position == null ? NotFound() : View(position);
        }

        // GET: Dashboard/Positions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var position = await _context.Positions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return position == null ? NotFound() : View(position);
        }

        // POST: Dashboard/Positions/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var position = await _context.Positions.FindAsync(id);
            if (position != null)
            {
                _context.Positions.Remove(position);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
