namespace HospitalApp.Models.ViewModels
{
    namespace HospitalApp.ViewModels
    {
        public class PrescriptionIndexItem
        {
            public int Id { get; set; }
            public int MedicalRecordId { get; set; }
            public string? PatientName { get; set; }
            public DateTime CreatedAt { get; set; }
        }
    }

}
