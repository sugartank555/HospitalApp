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
    public class PrescriptionsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public PrescriptionsController(ApplicationDbContext db) => _db = db;

        // ===== CRUD cơ bản =====
        public async Task<IActionResult> Index()
            => View(await _db.Prescriptions.Include(p => p.MedicalRecord).AsNoTracking().ToListAsync());

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var m = await _db.Prescriptions.Include(p => p.MedicalRecord).AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == id);
            if (m == null) return NotFound();

            // Lấy items để hiển thị kèm
            var items = await _db.MedicineOfPrescriptions
                .Where(i => i.PrescriptionId == m.Id)
                .Include(i => i.Medicine)
                .AsNoTracking()
                .ToListAsync();
            ViewBag.Items = items;
            return View(m);
        }

        public IActionResult Create()
        {
            LoadMR();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Prescription model)
        {
            RemoveNav("MedicalRecord");
            if (!ModelState.IsValid) { LoadMR(model.MedicalRecordId); return View(model); }
            _db.Prescriptions.Add(model);
            await _db.SaveChangesAsync();

            // Sau khi tạo toa -> chuyển sang quản lý thuốc
            return RedirectToAction(nameof(Items), new { id = model.Id });
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var m = await _db.Prescriptions.FindAsync(id);
            if (m == null) return NotFound();
            LoadMR(m.MedicalRecordId);
            return View(m);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Prescription model)
        {
            if (id != model.Id) return NotFound();
            RemoveNav("MedicalRecord");
            if (!ModelState.IsValid) { LoadMR(model.MedicalRecordId); return View(model); }
            _db.Update(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var m = await _db.Prescriptions.Include(p => p.MedicalRecord)
                        .AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return m == null ? NotFound() : View(m);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Xoá items trước (nếu không cascade)
            var items = await _db.MedicineOfPrescriptions
                .Where(x => x.PrescriptionId == id).ToListAsync();
            if (items.Count > 0) _db.MedicineOfPrescriptions.RemoveRange(items);

            var m = await _db.Prescriptions.FindAsync(id);
            if (m != null) { _db.Prescriptions.Remove(m); await _db.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }

        // ===== Quản lý thuốc trong toa =====

        // Trang quản lý thuốc
        public async Task<IActionResult> Items(int id)
        {
            var pres = await _db.Prescriptions
                .Include(p => p.MedicalRecord)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);
            if (pres == null) return NotFound();

            var items = await _db.MedicineOfPrescriptions
                .Where(i => i.PrescriptionId == id)
                .Include(i => i.Medicine)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.Prescription = pres;
            ViewBag.Items = items;
            LoadMedicines(); // dropdown thuốc
            return View();
        }

        // Thêm thuốc vào toa (upsert theo unique (PrescriptionId, MedicineId))
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddItem(int id, int medicineId, int quantity = 1)
        {
            if (quantity < 1) quantity = 1;

            var pres = await _db.Prescriptions.FindAsync(id);
            if (pres == null) return NotFound();

            var exist = await _db.MedicineOfPrescriptions
                .FirstOrDefaultAsync(x => x.PrescriptionId == id && x.MedicineId == medicineId);

            if (exist != null)
            {
                // tăng số lượng
                // (yêu cầu entity MedicineOfPrescription có thuộc tính Quantity kiểu int)
                exist.Quantity += quantity;
                _db.Update(exist);
            }
            else
            {
                var item = new MedicineOfPrescription
                {
                    PrescriptionId = id,
                    MedicineId = medicineId,
                    Quantity = quantity   // nếu model của bạn dùng tên khác, đổi ở đây
                };
                _db.MedicineOfPrescriptions.Add(item);
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Items), new { id });
        }

        // Cập nhật số lượng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateItem(int id, int medicineId, int quantity)
        {
            if (quantity < 1) quantity = 1;
            var item = await _db.MedicineOfPrescriptions
                .FirstOrDefaultAsync(x => x.PrescriptionId == id && x.MedicineId == medicineId);
            if (item == null) return RedirectToAction(nameof(Items), new { id });

            item.Quantity = quantity;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Items), new { id });
        }

        // Xoá 1 dòng thuốc
        [HttpPost]
        [ValidateAntiForgeryToken]
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

        // ===== Helpers =====

        private void LoadMR(int? selected = null)
            => ViewData["MedicalRecordId"] = new SelectList(_db.MedicalRecords.AsNoTracking(), "Id", "Id", selected);

        private void LoadMedicines(int? selected = null)
            => ViewData["MedicineId"] = new SelectList(_db.Medicines.AsNoTracking(), "Id", "Name", selected);

        private void RemoveNav(string key)
        {
            foreach (var k in ModelState.Keys.Where(k => k.Equals(key) || k.StartsWith(key + ".", StringComparison.OrdinalIgnoreCase)).ToList())
                ModelState.Remove(k);
        }
    }
}
