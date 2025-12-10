using HospitalApp.Models;
using HospitalApp.Models.ViewModels.HospitalApp.ViewModels;

namespace HospitalApp.ViewModels
{
    public class PrescriptionIndexViewModel
    {
        public IEnumerable<PrescriptionIndexItem> Items { get; set; } = new List<PrescriptionIndexItem>();

        public PagingInfo PagingInfo { get; set; } = new PagingInfo();

        public string? Search { get; set; }
        public string? SortOrder { get; set; }
    }
}
