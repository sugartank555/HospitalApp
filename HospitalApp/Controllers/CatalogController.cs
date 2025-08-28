using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HospitalApp.Data;

namespace HospitalApp.Controllers
{
    public class CatalogController : Controller
    {
        private readonly ApplicationDbContext _db;
        public CatalogController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Departments()
        {
            var data = await _db.MedicalDepartments.AsNoTracking().OrderBy(x => x.Name).ToListAsync();
            return View(data);
        }

        public async Task<IActionResult> Services(int? departmentId)
        {
            var q = _db.Services.Include(s => s.MedicalDepartment).AsNoTracking();
            if (departmentId.HasValue)
                q = q.Where(s => s.MedicalDepartmentId == departmentId.Value);

            ViewBag.Departments = await _db.MedicalDepartments
                .AsNoTracking().OrderBy(x => x.Name).ToListAsync();

            ViewBag.SelectedDepartmentId = departmentId; // <— đưa selected id sang View
            return View(await q.OrderBy(x => x.Name).ToListAsync());
        }


        public async Task<IActionResult> Doctors(int? departmentId)
        {
            // nếu có quan hệ Doctor -> Expertise/Department riêng, bạn có thể join để lọc; tạm hiển thị tất cả
            var data = await _db.Doctors.AsNoTracking().OrderBy(d => d.FullName).ToListAsync();
            return View(data);
        }
    }
}
