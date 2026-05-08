using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Npgsql;

namespace VetCare.LegacyReports.Data
{
    public sealed class EfAppointmentReportRepository : IAppointmentReportRepository
    {
        private const string Sql = @"
SELECT
  a.""Id""           AS ""Id"",
  a.""TenantId""     AS ""TenantId"",
  p.""Name""         AS ""PetName"",
  o.""FullName""     AS ""OwnerName"",
  u.""Email""        AS ""VetName"",
  a.""ScheduledAt""  AS ""ScheduledAt"",
  a.""Status""       AS ""Status"",
  v.""VaccineName""  AS ""VaccineName""
FROM vetcare.appointments a
INNER JOIN vetcare.pets   p ON p.""Id"" = a.""PetId""
INNER JOIN vetcare.owners o ON o.""Id"" = p.""OwnerId""
INNER JOIN vetcare.users  u ON u.""Id"" = a.""VetUserId""
LEFT  JOIN vetcare.vaccinations v
       ON v.""PetId"" = a.""PetId""
      AND date_trunc('day', v.""AdministeredAt"") = date_trunc('day', a.""ScheduledAt"")
WHERE EXTRACT(YEAR  FROM a.""ScheduledAt"") = @year
  AND EXTRACT(MONTH FROM a.""ScheduledAt"") = @month
ORDER BY a.""ScheduledAt"";";

        private readonly LegacyDbContext _context;

        public EfAppointmentReportRepository(LegacyDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public IReadOnlyList<AppointmentReport> GetForMonth(int year, int month)
        {
            if (month < 1 || month > 12)
            {
                throw new ArgumentOutOfRangeException(nameof(month), month, "Month must be between 1 and 12.");
            }

            var yearParam = new NpgsqlParameter("year", year);
            var monthParam = new NpgsqlParameter("month", month);

            return _context.Database
                .SqlQuery<AppointmentReport>(Sql, yearParam, monthParam)
                .ToList();
        }
    }
}
