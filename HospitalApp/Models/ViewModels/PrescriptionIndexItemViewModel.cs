namespace HospitalApp.Models.ViewModels
{
    public class PrescriptionIndexItemViewModel
    {
        public int Id { get; set; }
        public int MedicalRecordId { get; set; }
        public string? PatientName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

}
