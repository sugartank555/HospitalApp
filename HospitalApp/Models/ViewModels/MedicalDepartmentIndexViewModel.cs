using HospitalApp.Models;

namespace HospitalApp.ViewModels
{
    public class MedicalDepartmentIndexViewModel
    {
        public IEnumerable<MedicalDepartment> Items { get; set; } = Enumerable.Empty<MedicalDepartment>();

        public PagingInfo PagingInfo { get; set; } = new PagingInfo();

        public string? Search { get; set; }
    }
}
