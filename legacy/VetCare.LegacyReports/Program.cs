using System;
using System.Globalization;
using VetCare.LegacyReports.Data;
using VetCare.LegacyReports.Reports;

namespace VetCare.LegacyReports
{
    internal static class Program
    {
        private const string Usage =
            "Usage: VetCare.LegacyReports --tenant-id <guid> [--year <yyyy>] [--month <1-12>]";

        private static int Main(string[] args)
        {
            try
            {
                if (!TryParseArgs(args, out var tenantId, out var year, out var month, out var error))
                {
                    Console.Error.WriteLine("Error: " + error);
                    Console.Error.WriteLine(Usage);
                    return 1;
                }

                using (var context = new LegacyDbContext())
                {
                    var repository = new EfAppointmentReportRepository(context);
                    var generator = new MonthlyReportGenerator(repository);
                    var path = generator.Generate(tenantId, year, month);
                    Console.WriteLine("Report generated: " + path);
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: " + ex.Message);
                return 1;
            }
        }

        internal static bool TryParseArgs(string[] args, out Guid tenantId, out int year, out int month, out string error)
        {
            tenantId = Guid.Empty;
            year = 0;
            month = 0;
            error = null;

            string tenantRaw = null;
            int? yearArg = null;
            int? monthArg = null;

            for (var i = 0; i < args.Length; i++)
            {
                if (i + 1 >= args.Length)
                {
                    break;
                }

                if (string.Equals(args[i], "--tenant-id", StringComparison.OrdinalIgnoreCase))
                {
                    tenantRaw = args[++i];
                }
                else if (string.Equals(args[i], "--year", StringComparison.OrdinalIgnoreCase))
                {
                    yearArg = int.Parse(args[++i], CultureInfo.InvariantCulture);
                }
                else if (string.Equals(args[i], "--month", StringComparison.OrdinalIgnoreCase))
                {
                    monthArg = int.Parse(args[++i], CultureInfo.InvariantCulture);
                }
            }

            if (string.IsNullOrWhiteSpace(tenantRaw))
            {
                error = "--tenant-id is required.";
                return false;
            }

            if (!Guid.TryParse(tenantRaw, out tenantId) || tenantId == Guid.Empty)
            {
                error = "--tenant-id must be a non-empty Guid.";
                return false;
            }

            var previousMonth = DateTime.UtcNow.AddMonths(-1);
            year = yearArg ?? previousMonth.Year;
            month = monthArg ?? previousMonth.Month;
            return true;
        }
    }
}
