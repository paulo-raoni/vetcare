using System;

namespace VetCare.LegacyReports.Data
{
    public sealed class AppointmentReport
    {
        public Guid Id { get; set; }

        public Guid TenantId { get; set; }

        public string PetName { get; set; } = string.Empty;

        public string OwnerName { get; set; } = string.Empty;

        public string VetName { get; set; } = string.Empty;

        public DateTime ScheduledAt { get; set; }

        public int Status { get; set; }

        public string VaccineName { get; set; }
    }
}
