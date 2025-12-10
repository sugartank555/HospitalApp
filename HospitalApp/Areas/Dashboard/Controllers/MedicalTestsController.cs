using HospitalApp.Data;
using HospitalApp.Models;
using HospitalApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HospitalApp.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Admin,Doctor")]
    public class MedicalTestsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public MedicalTestsController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index(string? search, string? sortOrder, int page = 1, int pageSize = 10)
        {
            var q = _db.MedicalTests
                .Include(t => t.MedicalRecord).ThenInclude(m => m.Patient)
                .Include(t => t.MedicalRecord).ThenInclude(m => m.Doctor)
                .AsNoTracking()
                .AsQueryable();

            // ===== SEARCH =====
            if (!string.IsNullOrWhiteSpace(search))
            {
                var k = search.Trim();

                if (DateTime.TryParse(k, out var dt))
                {
                    var d = dt.Date;
                    q = q.Where(t =>
                        (t.TestTime.HasValue && t.TestTime.Value.Date == d) ||
                        t.Name.Contains(k));
                }
                else if (decimal.TryParse(k, out var price))
                {
                    q = q.Where(t =>
                        t.TotalPrice == price ||
                        t.Name.Contains(k));
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

                ViewData["Search"] = k;
            }

            // ===== SORT =====
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSort"] = sortOrder == "name" ? "name_desc" : "name";
            ViewData["TimeSort"] = sortOrder == "time" ? "time_desc" : "time";
            ViewData["PriceSort"] = sortOrder == "price" ? "price_desc" : "price";
            ViewData["RecordSort"] = sortOrder == "record" ? "record_desc" : "record";

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

            // ===== PAGING =====
            var totalItems = await q.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            if (totalPages == 0) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var items = await q
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vm = new MedicalTestIndexViewModel
            {
                Items = items,
                PagingInfo = new PagingInfo
                {
                    PageIndex = page,
                    PageSize = pageSize,
                    TotalItems = totalItems,
                    TotalPages = totalPages
                },
                Search = search,
                SortOrder = sortOrder
            };

            return View(vm);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var m = await _db.MedicalTests
                .Include(t => t.MedicalRecord)
                .ThenInclude(m => m.Patient)
                .Include(t => t.MedicalRecord)
                .ThenInclude(m => m.Doctor)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            return m == null ? NotFound() : View(m);
        }

        public IActionResult Create()
        {
            var vm = new CreateMedicalTestVM
            {
                MedicalRecordList = _db.MedicalRecords
                    .Include(m => m.Patient)
                    .Select(m => new SelectListItem
                    {
                        Value = m.Id.ToString(),
                        Text = $"#{m.Id} – {m.Patient.FullName} ({m.Time:dd/MM/yyyy})"
                    }),

                ServiceList = _db.Services
                    .Select(s => new SelectListItem
                    {
                        Value = s.Id.ToString(),
                        Text = $"{s.Name} – {s.Price:N0}đ"
                    })
            };

            return View(vm);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
      
        public async Task<IActionResult> Create(CreateMedicalTestVM vm)
        {
            if (!ModelState.IsValid)
            {
                vm.MedicalRecordList = _db.MedicalRecords
                    .Include(m => m.Patient)
                    .Select(m => new SelectListItem
                    {
                        Value = m.Id.ToString(),
                        Text = $"#{m.Id} – {m.Patient.FullName}"
                    });

                vm.ServiceList = _db.Services
                    .Select(s => new SelectListItem
                    {
                        Value = s.Id.ToString(),
                        Text = $"{s.Name} – {s.Price:N0}đ"
                    });

                return View(vm);
            }

            // 1️⃣ Tạo test trước
            var test = new MedicalTest
            {
                Name = vm.Name,
                TestTime = vm.TestTime ?? DateTime.Now,
                MedicalRecordId = vm.MedicalRecordId,
                TotalPrice = 0m
            };

            _db.MedicalTests.Add(test);
            await _db.SaveChangesAsync();  // BẮT BUỘC — để có TestId

            // 2️⃣ Thêm dịch vụ vào test
            decimal total = 0m;

            foreach (var sid in vm.ServiceIds)
            {
                var service = await _db.Services.FindAsync(sid);
                if (service == null) continue;

                total += service.Price;

                _db.ServiceOfMedicalTests.Add(new ServiceOfMedicalTest
                {
                    MedicalTestId = test.Id,   // DÙNG ID SAU KHI SAVE
                    ServiceId = sid,
                    Quantity = 1
                });
            }

            // 3️⃣ Cập nhật tổng tiền
            test.TotalPrice = total;
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
            var m = await _db.MedicalTests
                .Include(t => t.MedicalRecord)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            return m == null ? NotFound() : View(m);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var m = await _db.MedicalTests.FindAsync(id);
            if (m != null)
            {
                _db.MedicalTests.Remove(m);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // 🔥 HIỂN THỊ HỒ SƠ KHÁM ĐẦY ĐỦ (BN + BS + Chức danh + Ngày khám)
        // ============================================================
        private void LoadMR(int? selected = null)
        {
            var list = _db.MedicalRecords
                .Include(m => m.Patient)
                .Include(m => m.Doctor).ThenInclude(d => d.Position)
                .AsNoTracking()
                .Select(m => new
                {
                    m.Id,
                    Display =
                        $"#{m.Id} – BN: {m.Patient.FullName} – BS: {m.Doctor.FullName} ({m.Doctor.Position}) – {m.Time:dd/MM/yyyy}"
                })
                .ToList();

            ViewData["MedicalRecordId"] = new SelectList(list, "Id", "Display", selected);
        }

        private void RemoveNav(string key)
        {
            foreach (var k in ModelState.Keys.Where(k => k.Equals(key) || k.StartsWith(key + ".", StringComparison.OrdinalIgnoreCase)).ToList())
                ModelState.Remove(k);
        }
    }
}
