using HospitalApp.Models;

namespace HospitalApp.ViewModels
{
    public class PatientIndexViewModel
    {
        public IEnumerable<Patient> Items { get; set; } = new List<Patient>();
        public PagingInfo PagingInfo { get; set; } = new PagingInfo();
        public string? Search { get; set; }
    }
}
