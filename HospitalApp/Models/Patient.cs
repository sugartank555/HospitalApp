using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace HospitalApp.Models
{
    public class Patient
    {
        public int Id { get; set; }

        // >>> Thêm thuộc tính này để các View/Controller dùng được
        [Required, StringLength(120)]
        public string FullName { get; set; } = default!;

        public DateTime? DateOfBirth { get; set; }

        [StringLength(50)]
        public string? Ethnic { get; set; }

        public string? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        [ValidateNever]                   // tránh MVC validate navigation
        public ApplicationUser? User { get; set; }

        [ValidateNever]
        public ICollection<AppointmentSchedule> Appointments { get; set; } = new List<AppointmentSchedule>();

        [ValidateNever]
        public ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
    }
}
