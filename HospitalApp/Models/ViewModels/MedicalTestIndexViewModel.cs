using HospitalApp.Models;

namespace HospitalApp.ViewModels
{
    public class MedicalTestIndexViewModel
    {
        public IEnumerable<MedicalTest> Items { get; set; } = new List<MedicalTest>();
        public PagingInfo PagingInfo { get; set; } = new PagingInfo();
        public string? Search { get; set; }
        public string? SortOrder { get; set; }
    }
}
