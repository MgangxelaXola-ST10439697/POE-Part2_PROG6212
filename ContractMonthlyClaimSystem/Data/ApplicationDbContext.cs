using ContractMonthlyClaimSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace ContractMonthlyClaimSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Claim> Claims { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Claim>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Required fields
                entity.Property(e => e.LecturerName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.LecturerEmail)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.HoursWorked)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.HourlyRate)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)");

                // Optional fields - NOT REQUIRED
                entity.Property(e => e.Notes)
                    .HasMaxLength(500);

                entity.Property(e => e.FileName)
                    .HasMaxLength(255);

                entity.Property(e => e.FilePath)
                    .HasMaxLength(500);

                // Default values
                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasDefaultValue("Pending")
                    .HasMaxLength(50);

                entity.Property(e => e.DateSubmitted)
                    .IsRequired()
                    .HasDefaultValueSql("GETDATE()");
            });
        }
    }
}