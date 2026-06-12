using ClinicManager.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ClinicManager.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Patient> Patients { get; set; }
        public DbSet<Visit> Visits { get; set; }
        public DbSet<MedicalRecord> MedicalRecords { get; set; }
        public DbSet<MedicalDocument> MedicalDocuments { get; set; }
        public DbSet<ClinicalNote> ClinicalNotes { get; set; }
        public DbSet<PrescribedMedication> PrescribedMedications { get; set; }
        public DbSet<Medication> Medications { get; set; }
        public DbSet<ProcedurePerformed> ProceduresPerformed { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<Medication>()
                .Property(m => m.UnitPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<ProcedurePerformed>()
                .Property(p => p.ServiceCost)
                .HasPrecision(18, 2);
        }
    }
}
