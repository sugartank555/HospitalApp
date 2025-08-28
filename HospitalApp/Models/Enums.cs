namespace HospitalApp.Models.Enums
{
    public enum AppointmentStatus { Pending = 0, Confirmed = 1, Completed = 2, Cancelled = 3 }
    public enum MedicalRecordStatus { Open = 0, InProgress = 1, Closed = 2 }
    public enum PaymentStatus { Unpaid = 0, Paid = 1, Refunded = 2 }
    public enum StaffType { Staff = 0, Doctor = 1, Nursing = 2 }
}
