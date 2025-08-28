using System.ComponentModel.DataAnnotations.Schema;
using HospitalApp.Models.Enums;

namespace HospitalApp.Models
{
    public class Staff
    {
        public int Id { get; set; }
        public StaffType StaffType { get; set; } = StaffType.Staff;

        public string FullName { get; set; } = default!;
        public string? PhoneNumber { get; set; }

        public int PositionId { get; set; }
        public Position Position { get; set; } = default!;

        public int? ExpertiseId { get; set; }
        public Expertise? Expertise { get; set; }

        public string? UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        public ICollection<AppointmentSchedule> AppointmentsAsDoctor { get; set; } = new List<AppointmentSchedule>();
        public ICollection<MedicalRecord> MedicalRecordsAsDoctor { get; set; } = new List<MedicalRecord>();
    }

    public class Doctor : Staff { }
    public class Nursing : Staff { }
}
