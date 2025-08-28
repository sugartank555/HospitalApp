using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;   // <-- thêm
using HospitalApp.Models.Enums;

namespace HospitalApp.Models
{
    public class Staff
    {
        public int Id { get; set; }

        public StaffType StaffType { get; set; } = StaffType.Staff;

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [StringLength(100)]
        public string FullName { get; set; } = default!;

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn chức danh")]
        public int PositionId { get; set; }

        [ValidateNever]                               // <-- thêm
        public Position? Position { get; set; }       // có thể để nullable cho an toàn

        public int? ExpertiseId { get; set; }

        [ValidateNever]                               // <-- thêm
        public Expertise? Expertise { get; set; }

        public string? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        [ValidateNever]                               // <-- thêm
        public ApplicationUser? User { get; set; }

        [ValidateNever]                               // <-- thêm
        public ICollection<AppointmentSchedule> AppointmentsAsDoctor { get; set; } = new List<AppointmentSchedule>();

        [ValidateNever]                               // <-- thêm
        public ICollection<MedicalRecord> MedicalRecordsAsDoctor { get; set; } = new List<MedicalRecord>();
    }

    public class Doctor : Staff { }
    public class Nursing : Staff { }
}
