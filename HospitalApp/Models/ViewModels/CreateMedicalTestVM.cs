using Microsoft.AspNetCore.Mvc.Rendering;

namespace HospitalApp.ViewModels
{
    public class CreateMedicalTestVM
    {
        public int MedicalRecordId { get; set; }

        public string Name { get; set; } = default!;

        public DateTime? TestTime { get; set; }

        // Danh sách dịch vụ mà người dùng chọn trong form
        public List<int> ServiceIds { get; set; } = new();

        // Hiển thị select list trong View
        public IEnumerable<SelectListItem>? MedicalRecordList { get; set; }
        public IEnumerable<SelectListItem>? ServiceList { get; set; }
    }
}
