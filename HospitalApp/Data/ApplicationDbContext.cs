using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using HospitalApp.Models;

namespace HospitalApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Patient> Patients => Set<Patient>();
        public DbSet<Position> Positions => Set<Position>();
        public DbSet<Expertise> Expertises => Set<Expertise>();
        public DbSet<Staff> Staffs => Set<Staff>();
        public DbSet<Doctor> Doctors => Set<Doctor>();
        public DbSet<Nursing> Nursings => Set<Nursing>();
        public DbSet<MedicalDepartment> MedicalDepartments => Set<MedicalDepartment>();
        public DbSet<Service> Services => Set<Service>();
        public DbSet<AppointmentSchedule> AppointmentSchedules => Set<AppointmentSchedule>();
        public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();
        public DbSet<MedicalRecordInformation> MedicalRecordInformations => Set<MedicalRecordInformation>();
        public DbSet<MedicalTest> MedicalTests => Set<MedicalTest>();
        public DbSet<ServiceOfMedicalTest> ServiceOfMedicalTests => Set<ServiceOfMedicalTest>();
        public DbSet<Prescription> Prescriptions => Set<Prescription>();
        public DbSet<Medicine> Medicines => Set<Medicine>();
        public DbSet<MedicineOfPrescription> MedicineOfPrescriptions => Set<MedicineOfPrescription>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            // Kế thừa kiểu TPT
            b.Entity<Staff>().ToTable("Staffs");
            b.Entity<Doctor>().ToTable("Doctors");
            b.Entity<Nursing>().ToTable("Nursings");

            // Quan hệ với Identity (1-1 optional)
            b.Entity<Patient>()
                .HasOne(p => p.User)
                .WithOne(u => u.PatientProfile)
                .HasForeignKey<Patient>(p => p.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            b.Entity<Staff>()
                .HasOne(s => s.User)
                .WithOne(u => u.StaffProfile)
                .HasForeignKey<Staff>(s => s.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Staff bắt buộc Position, optional Expertise
            b.Entity<Staff>()
                .HasOne(s => s.Position)
                .WithMany(p => p.Staffs)
                .HasForeignKey(s => s.PositionId)
                .OnDelete(DeleteBehavior.Restrict);

            b.Entity<Staff>()
                .HasOne(s => s.Expertise)
                .WithMany(e => e.Staffs)
                .HasForeignKey(s => s.ExpertiseId)
                .OnDelete(DeleteBehavior.SetNull);

            // AppointmentSchedule: tránh multiple cascade
            b.Entity<AppointmentSchedule>()
                .HasOne(a => a.Patient)
                .WithMany(p => p.Appointments)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            b.Entity<AppointmentSchedule>()
                .HasOne(a => a.Doctor)
                .WithMany(d => d.AppointmentsAsDoctor)
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            // MedicalRecord
            b.Entity<MedicalRecord>()
                .HasOne(m => m.Patient)
                .WithMany(p => p.MedicalRecords)
                .HasForeignKey(m => m.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            b.Entity<MedicalRecord>()
                .HasOne(m => m.Doctor)
                .WithMany(d => d.MedicalRecordsAsDoctor)
                .HasForeignKey(m => m.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            // 1-1 Info
            b.Entity<MedicalRecordInformation>()
                .HasOne(i => i.MedicalRecord)
                .WithOne(m => m.Information)
                .HasForeignKey<MedicalRecordInformation>(i => i.MedicalRecordId)
                .OnDelete(DeleteBehavior.Cascade);

            // MedicalTest
            b.Entity<MedicalTest>()
                .HasOne(t => t.MedicalRecord)
                .WithMany(m => m.MedicalTests)
                .HasForeignKey(t => t.MedicalRecordId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique pairs
            b.Entity<ServiceOfMedicalTest>()
                .HasIndex(x => new { x.MedicalTestId, x.ServiceId }).IsUnique();

            b.Entity<MedicineOfPrescription>()
                .HasIndex(x => new { x.PrescriptionId, x.MedicineId }).IsUnique();

            // Decimal precision
            b.Entity<Service>().Property(p => p.Price).HasColumnType("decimal(18,2)");
            b.Entity<MedicalTest>().Property(p => p.TotalPrice).HasColumnType("decimal(18,2)");
            b.Entity<Medicine>().Property(p => p.Price).HasColumnType("decimal(18,2)");
        }
    }
}
