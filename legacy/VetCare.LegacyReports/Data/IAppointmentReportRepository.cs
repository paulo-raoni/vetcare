using System.Collections.Generic;

namespace VetCare.LegacyReports.Data
{
    public interface IAppointmentReportRepository
    {
        IReadOnlyList<AppointmentReport> GetForMonth(int year, int month);
    }
}
