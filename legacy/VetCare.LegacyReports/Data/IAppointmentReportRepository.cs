using System;
using System.Collections.Generic;

namespace VetCare.LegacyReports.Data
{
    public interface IAppointmentReportRepository
    {
        IReadOnlyList<AppointmentReport> GetForMonth(Guid tenantId, int year, int month);
    }
}
