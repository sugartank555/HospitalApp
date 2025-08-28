using Microsoft.AspNetCore.Identity;

namespace HospitalApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Address { get; set; }

        public Patient? PatientProfile { get; set; }
        public Staff? StaffProfile { get; set; }
    }
}
