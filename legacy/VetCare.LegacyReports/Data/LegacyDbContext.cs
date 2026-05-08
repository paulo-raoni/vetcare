using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace VetCare.LegacyReports.Data
{
    public class LegacyDbContext : DbContext
    {
        public const string ConnectionStringName = "DefaultConnection";

        public LegacyDbContext()
            : base("name=" + ConnectionStringName)
        {
            Database.SetInitializer<LegacyDbContext>(null);
        }

        public LegacyDbContext(DbConnection existingConnection, bool contextOwnsConnection)
            : base(existingConnection, contextOwnsConnection)
        {
            Database.SetInitializer<LegacyDbContext>(null);
        }

        public virtual DbSet<AppointmentReport> Appointments { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            modelBuilder.HasDefaultSchema("vetcare");

            var appointment = modelBuilder.Entity<AppointmentReport>();
            appointment.ToTable("appointments", "vetcare");
            appointment.HasKey(a => a.Id);
            appointment.Property(a => a.Id).HasColumnName("Id");
            appointment.Property(a => a.TenantId).HasColumnName("TenantId");
            appointment.Property(a => a.ScheduledAt).HasColumnName("ScheduledAt");
            appointment.Property(a => a.Status).HasColumnName("Status");

            // Joined columns are populated via Database.SqlQuery<AppointmentReport>,
            // so they must be excluded from the EF6 mapping for the base table.
            appointment.Ignore(a => a.PetName);
            appointment.Ignore(a => a.OwnerName);
            appointment.Ignore(a => a.VetName);
            appointment.Ignore(a => a.VaccineName);
        }
    }
}
