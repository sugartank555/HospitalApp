namespace HospitalApp.Models
{
    public class MedicalDepartment
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public ICollection<Service> Services { get; set; } = new List<Service>();
    }
}
