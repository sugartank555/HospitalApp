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

        public async Task<IActionResult> Index()
            => View(await _context.Expertises.AsNoTracking().ToListAsync());

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
