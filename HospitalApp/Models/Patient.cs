using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalApp.Models
{
    public class Patient
    {
        public int Id { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Ethnic { get; set; }

        public string? UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        public ICollection<AppointmentSchedule> Appointments { get; set; } = new List<AppointmentSchedule>();
        public ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
    }
}
