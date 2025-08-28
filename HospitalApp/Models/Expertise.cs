namespace HospitalApp.Models
{
    public class Expertise
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public ICollection<Staff> Staffs { get; set; } = new List<Staff>();
    }
}
