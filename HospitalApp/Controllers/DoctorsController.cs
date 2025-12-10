using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HospitalApp.Data;

namespace HospitalApp.Controllers
{
    [Route("doctors")]
    public class DoctorsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public DoctorsController(ApplicationDbContext db) => _db = db;

        // GET /doctors
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var doctors = await _db.Doctors
                .Include(d => d.Position)
                .Include(d => d.Expertise)
                .AsNoTracking()
                .OrderByDescending(d => d.Id)
                .ToListAsync();

            return View(doctors);
        }

        // GET /doctors/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var d = await _db.Doctors
                .Include(x => x.Position)
                .Include(x => x.Expertise)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (d == null) return NotFound();

            return View(d);
        }
    }
}
