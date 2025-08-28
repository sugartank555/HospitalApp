namespace HospitalApp.Models
{
    public class MedicalTest
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public DateTime? TestTime { get; set; }
        public decimal TotalPrice { get; set; }

        public int MedicalRecordId { get; set; }
        public MedicalRecord MedicalRecord { get; set; } = default!;
        public ICollection<ServiceOfMedicalTest> Services { get; set; } = new List<ServiceOfMedicalTest>();
    }

    public class ServiceOfMedicalTest
    {
        public int Id { get; set; }
        public int Quantity { get; set; } = 1;

        public int MedicalTestId { get; set; }
        public MedicalTest MedicalTest { get; set; } = default!;
        public int ServiceId { get; set; }
        public Service Service { get; set; } = default!;
    }
}
