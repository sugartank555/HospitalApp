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
        public async Task<IActionResult> Index()
            => View(await _context.Positions.AsNoTracking().ToListAsync());

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
