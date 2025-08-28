using HospitalApp.Models.Enums;

namespace HospitalApp.Models
{
    public class MedicalRecord
    {
        public int Id { get; set; }
        public DateTime Time { get; set; } = DateTime.UtcNow;
        public MedicalRecordStatus Status { get; set; } = MedicalRecordStatus.Open;
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;

        public int PatientId { get; set; }
        public Patient Patient { get; set; } = default!;

        public int DoctorId { get; set; }
        public Doctor Doctor { get; set; } = default!;

        public MedicalRecordInformation? Information { get; set; }

        public ICollection<MedicalTest> MedicalTests { get; set; } = new List<MedicalTest>();
        public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    }

    public class MedicalRecordInformation
    {
        public int Id { get; set; }
        public float? BloodPressure { get; set; }
        public float? BodyTemperature { get; set; }
        public float? HeartBeat { get; set; }
        public float? Height { get; set; }
        public float? Weight { get; set; }
        public string? Diagnose { get; set; }
        public string? Detail { get; set; }
        public string? Solution { get; set; }

        public int MedicalRecordId { get; set; }
        public MedicalRecord MedicalRecord { get; set; } = default!;
    }
}
