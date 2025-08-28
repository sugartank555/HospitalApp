using HospitalApp.Models.Enums;

namespace HospitalApp.Models
{
    public class AppointmentSchedule
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string? TimeFrame { get; set; }
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

        public int PatientId { get; set; }
        public Patient Patient { get; set; } = default!;

        public int DoctorId { get; set; }
        public Doctor Doctor { get; set; } = default!;
    }
}
