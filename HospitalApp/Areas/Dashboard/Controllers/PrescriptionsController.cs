using HospitalApp.Data;
using HospitalApp.Models;
using HospitalApp.Models.ViewModels.HospitalApp.ViewModels;
using HospitalApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HospitalApp.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Admin,Doctor")]
    public class PrescriptionsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public PrescriptionsController(ApplicationDbContext db) => _db = db;

        // ============================
        // INDEX
        // ============================
        public async Task<IActionResult> Index(string? search, string? sortOrder, int page = 1, int pageSize = 10)
        {
            var q = _db.Prescriptions
                .Include(p => p.MedicalRecord).ThenInclude(m => m.Patient)
                .AsNoTracking()
                .AsQueryable();

            // TÌM KIẾM
            if (!string.IsNullOrWhiteSpace(search))
            {
                var k = search.Trim();

                if (DateTime.TryParse(k, out var dt))
                    q = q.Where(p => p.CreatedAt.Date == dt.Date);
                else if (int.TryParse(k, out var num))
                    q = q.Where(p => p.Id == num || p.MedicalRecordId == num);

                ViewData["Search"] = k;
            }

            // SẮP XẾP
            ViewData["CurrentSort"] = sortOrder;
            ViewData["IdSort"] = sortOrder == "id" ? "id_desc" : "id";
            ViewData["DateSort"] = sortOrder == "date" ? "date_desc" : "date";
            ViewData["MrSort"] = sortOrder == "mr" ? "mr_desc" : "mr";

            q = sortOrder switch
            {
                "id" => q.OrderBy(p => p.Id),
                "id_desc" => q.OrderByDescending(p => p.Id),
                "date" => q.OrderBy(p => p.CreatedAt),
                "date_desc" => q.OrderByDescending(p => p.CreatedAt),
                "mr" => q.OrderBy(p => p.MedicalRecordId),
                "mr_desc" => q.OrderByDescending(p => p.MedicalRecordId),
                _ => q.OrderByDescending(p => p.CreatedAt)
            };

            // PHÂN TRANG
            var totalItems = await q.CountAsync();

            var items = await q
                .Select(p => new PrescriptionIndexItem
                {
                    Id = p.Id,
                    MedicalRecordId = p.MedicalRecordId,
                    PatientName = p.MedicalRecord.Patient.FullName,
                    CreatedAt = p.CreatedAt
                })
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vm = new PrescriptionIndexViewModel
            {
                Items = items,
                PagingInfo = new PagingInfo
                {
                    PageIndex = page,
                    PageSize = pageSize,
                    TotalItems = totalItems,
                    TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
                },
                Search = search,
                SortOrder = sortOrder
            };

            return View(vm);
        }

        // ============================
        // PRINT INVOICE
        // ============================
        public async Task<IActionResult> PrintInvoice(int id)
        {
            var pres = await _db.Prescriptions
                .Include(p => p.MedicalRecord).ThenInclude(m => m.Patient)
                .Include(p => p.MedicalRecord).ThenInclude(m => m.Doctor)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pres == null) return NotFound();

            var items = await _db.MedicineOfPrescriptions
                .Include(i => i.Medicine)
                .Where(i => i.PrescriptionId == id)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.Items = items;

            return View("PrintInvoice", pres);
        }

        // ============================
        // DETAILS
        // ============================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var pres = await _db.Prescriptions
                .Include(p => p.MedicalRecord)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (pres == null) return NotFound();

            var items = await _db.MedicineOfPrescriptions
                .Include(i => i.Medicine)
                .Where(i => i.PrescriptionId == id)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.Items = items;

            return View(pres);
        }

        // ============================
        // CREATE
        // ============================
        public IActionResult Create()
        {
            LoadMR();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Prescription model)
        {
            RemoveNav("MedicalRecord");

            if (!ModelState.IsValid)
            {
                LoadMR(model.MedicalRecordId);
                return View(model);
            }

            _db.Prescriptions.Add(model);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Items), new { id = model.Id });
        }

        // ============================
        // EDIT
        // ============================
        public async Task<IActionResult> Edit(int id)
        {
            var pres = await _db.Prescriptions.FindAsync(id);
            if (pres == null) return NotFound();

            LoadMR(pres.MedicalRecordId);
            return View(pres);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Prescription model)
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

        // ============================
        // DELETE
        // ============================
        public async Task<IActionResult> Delete(int id)
        {
            var pres = await _db.Prescriptions
                .Include(p => p.MedicalRecord)
                .FirstOrDefaultAsync(p => p.Id == id);

            return pres == null ? NotFound() : View(pres);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var items = await _db.MedicineOfPrescriptions
                .Where(x => x.PrescriptionId == id)
                .ToListAsync();

            if (items.Any())
                _db.MedicineOfPrescriptions.RemoveRange(items);

            var pres = await _db.Prescriptions.FindAsync(id);
            if (pres != null)
            {
                _db.Prescriptions.Remove(pres);
                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // ============================
        // ITEMS (THUỐC)
        // ============================
        public async Task<IActionResult> Items(int id)
        {
            var pres = await _db.Prescriptions
                .Include(p => p.MedicalRecord)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pres == null) return NotFound();

            var items = await _db.MedicineOfPrescriptions
                .Include(i => i.Medicine)
                .Where(i => i.PrescriptionId == id)
                .ToListAsync();

            ViewBag.Prescription = pres;
            ViewBag.Items = items;

            LoadMedicines();

            return View();
        }

        // ADD ITEM
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddItem(int id, int medicineId, int quantity = 1, string? note = null)
        {
            if (quantity < 1) quantity = 1;

            var exist = await _db.MedicineOfPrescriptions
                .FirstOrDefaultAsync(x => x.PrescriptionId == id && x.MedicineId == medicineId);

            if (exist != null)
            {
                exist.Quantity += quantity;

                if (!string.IsNullOrWhiteSpace(note))
                    exist.Note = note;

                _db.Update(exist);
            }
            else
            {
                _db.MedicineOfPrescriptions.Add(new MedicineOfPrescription
                {
                    PrescriptionId = id,
                    MedicineId = medicineId,
                    Quantity = quantity,
                    Note = note
                });
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Items), new { id });
        }

        // UPDATE ITEM
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateItem(int id, int medicineId, int quantity, string? note)
        {
            var item = await _db.MedicineOfPrescriptions
                .FirstOrDefaultAsync(x => x.PrescriptionId == id && x.MedicineId == medicineId);

            if (item != null)
            {
                item.Quantity = quantity < 1 ? 1 : quantity;
                item.Note = note;

                _db.Update(item);
                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Items), new { id });
        }

        // REMOVE ITEM
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveItem(int id, int medicineId)
        {
            var item = await _db.MedicineOfPrescriptions
                .FirstOrDefaultAsync(x => x.PrescriptionId == id && x.MedicineId == medicineId);

            if (item != null)
            {
                _db.MedicineOfPrescriptions.Remove(item);
                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Items), new { id });
        }

        // ============================
        // CREATE FROM MEDICAL TEST
        // ============================
        public async Task<IActionResult> CreateFromTest(int testId)
        {
            var test = await _db.MedicalTests
                .Include(t => t.MedicalRecord)
                .FirstOrDefaultAsync(t => t.Id == testId);

            if (test == null) return NotFound();

            if (test.PrescriptionId != null)
                return RedirectToAction(nameof(Items), new { id = test.PrescriptionId });

            var pres = new Prescription
            {
                MedicalRecordId = test.MedicalRecordId,
                CreatedAt = DateTime.UtcNow
            };

            _db.Prescriptions.Add(pres);
            await _db.SaveChangesAsync();

            test.PrescriptionId = pres.Id;
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Items), new { id = pres.Id });
        }

        // ============================
        // HELPERS
        // ============================
        private void LoadMR(int? selected = null)
        {
            var list = _db.MedicalRecords
                .Include(m => m.Patient)
                .Select(m => new { m.Id, Name = m.Patient.FullName })
                .ToList();

            ViewData["MedicalRecordId"] =
                new SelectList(list, "Id", "Name", selected);
        }

        private void LoadMedicines(int? selected = null)
        {
            ViewData["MedicineId"] = new SelectList(
                _db.Medicines, "Id", "Name", selected);
        }

        private void RemoveNav(string key)
        {
            foreach (var k in ModelState.Keys.Where(k => k.StartsWith(key)))
                ModelState.Remove(k);
        }
        public async Task<IActionResult> PrintItems(int id)
        {
            var pres = await _db.Prescriptions
                .Include(p => p.MedicalRecord).ThenInclude(m => m.Patient)
                .Include(p => p.MedicalRecord).ThenInclude(m => m.Doctor)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pres == null) return NotFound();

            var items = await _db.MedicineOfPrescriptions
                .Include(i => i.Medicine)
                .Where(i => i.PrescriptionId == id)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.Items = items;

            return View("PrintItems", pres);
        }

    }
}
