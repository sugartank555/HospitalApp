using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HospitalApp.Data;
using HospitalApp.Models;

namespace HospitalApp.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Admin,Doctor")]
    public class MedicalTestsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public MedicalTestsController(ApplicationDbContext db) => _db = db;

        // GET: /Dashboard/MedicalTests
        // GET: /Dashboard/MedicalTests
        public async Task<IActionResult> Index(string? search, string? sortOrder, int page = 1)
        {
            const int pageSize = 10;

            var q = _db.MedicalTests
                .Include(t => t.MedicalRecord)
                .AsNoTracking()
                .AsQueryable();

            // ===== TÌM KIẾM =====
            if (!string.IsNullOrWhiteSpace(search))
            {
                var k = search.Trim();

                if (DateTime.TryParse(k, out var dt))
                {
                    var d = dt.Date;
                    q = q.Where(t =>
                        (t.TestTime.HasValue && t.TestTime.Value.Date == d) ||
                        t.Name.Contains(k) ||
                        t.MedicalRecordId.ToString() == k);
                }
                else if (decimal.TryParse(k, out var price))
                {
                    q = q.Where(t =>
                        t.TotalPrice == price ||
                        t.Name.Contains(k) ||
                        t.MedicalRecordId.ToString() == k);
                }
                else if (int.TryParse(k, out var num))
                {
                    q = q.Where(t =>
                        t.Id == num ||
                        t.MedicalRecordId == num ||
                        t.Name.Contains(k));
                }
                else
                {
                    q = q.Where(t => t.Name.Contains(k));
                }

                ViewData["Search"] = k; // giữ giá trị ô tìm kiếm
            }

            // Tổng trước khi trang
            var totalItems = await q.CountAsync();

            // ===== SẮP XẾP =====
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSort"] = sortOrder == "name" ? "name_desc" : "name";
            ViewData["TimeSort"] = sortOrder == "time" ? "time_desc" : "time";
            ViewData["PriceSort"] = sortOrder == "price" ? "price_desc" : "price";
            ViewData["RecordIdSort"] = sortOrder == "record" ? "record_desc" : "record";

            q = sortOrder switch
            {
                "name" => q.OrderBy(t => t.Name),
                "name_desc" => q.OrderByDescending(t => t.Name),
                "time" => q.OrderBy(t => t.TestTime),
                "time_desc" => q.OrderByDescending(t => t.TestTime),
                "price" => q.OrderBy(t => t.TotalPrice),
                "price_desc" => q.OrderByDescending(t => t.TotalPrice),
                "record" => q.OrderBy(t => t.MedicalRecordId),
                "record_desc" => q.OrderByDescending(t => t.MedicalRecordId),
                _ => q.OrderByDescending(t => t.TestTime).ThenBy(t => t.Name)
            };

            // ===== PHÂN TRANG =====
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var data = await q
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewData["Page"] = page;
            ViewData["TotalPages"] = totalPages;
            ViewData["PageSize"] = pageSize;
            ViewData["TotalItems"] = totalItems;

            return View(data);
        }



        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var m = await _db.MedicalTests.Include(t => t.MedicalRecord)
                        .AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return m == null ? NotFound() : View(m);
        }

        public IActionResult Create()
        {
            LoadMR();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MedicalTest model)
        {
            RemoveNav("MedicalRecord");
            if (!ModelState.IsValid)
            {
                LoadMR(model.MedicalRecordId);
                return View(model);
            }
            _db.MedicalTests.Add(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var m = await _db.MedicalTests.FindAsync(id);
            if (m == null) return NotFound();
            LoadMR(m.MedicalRecordId);
            return View(m);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MedicalTest model)
        {
            if (id != model.Id) return NotFound();
            RemoveNav("MedicalRecord");
            if (!ModelState.IsValid)
            {
                LoadMR(model.MedicalRecordId);
                return View(model);
            }
            _db.Update(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var m = await _db.MedicalTests.Include(t => t.MedicalRecord)
                        .AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return m == null ? NotFound() : View(m);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var m = await _db.MedicalTests.FindAsync(id);
            if (m != null) { _db.MedicalTests.Remove(m); await _db.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }

        private void LoadMR(int? selected = null)
            => ViewData["MedicalRecordId"] = new SelectList(_db.MedicalRecords.AsNoTracking(), "Id", "Id", selected);

        private void RemoveNav(string key)
        {
            foreach (var k in ModelState.Keys.Where(k => k.Equals(key) || k.StartsWith(key + ".", StringComparison.OrdinalIgnoreCase)).ToList())
                ModelState.Remove(k);
        }
    }
}
