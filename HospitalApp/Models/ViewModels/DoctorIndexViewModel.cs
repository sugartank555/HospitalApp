using HospitalApp.Models;

namespace HospitalApp.ViewModels
{
    public class DoctorIndexViewModel
    {
        public IEnumerable<Doctor> Items { get; set; } = new List<Doctor>();
        public PagingInfo PagingInfo { get; set; } = new PagingInfo();
        public string? Search { get; set; }
    }
}
