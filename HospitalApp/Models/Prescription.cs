namespace HospitalApp.Models
{
    public class Prescription
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int MedicalRecordId { get; set; }
        public MedicalRecord MedicalRecord { get; set; } = default!;
        public ICollection<MedicineOfPrescription> Medicines { get; set; } = new List<MedicineOfPrescription>();
    }

    public class Medicine
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string? Unit { get; set; }
        public string? UseManual { get; set; }
        public string? DeclaringUnit { get; set; }
        public string? ActiveElement { get; set; }
        public string? Packing { get; set; }
        public string? ProductionUnit { get; set; }
        public string? Using { get; set; }

        public ICollection<MedicineOfPrescription> MedicineOfPrescriptions { get; set; } = new List<MedicineOfPrescription>();
    }

    public class MedicineOfPrescription
    {
        public int Id { get; set; }
        public int Quantity { get; set; } = 1;

        public int MedicineId { get; set; }
        public Medicine Medicine { get; set; } = default!;
        public int PrescriptionId { get; set; }
        public Prescription Prescription { get; set; } = default!;
        public string? Note { get; set; }
    }
}
