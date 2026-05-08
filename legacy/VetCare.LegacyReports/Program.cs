using System;
using System.Globalization;
using VetCare.LegacyReports.Data;
using VetCare.LegacyReports.Reports;

namespace VetCare.LegacyReports
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                var (year, month) = ParseArgs(args);

                using (var context = new LegacyDbContext())
                {
                    var repository = new EfAppointmentReportRepository(context);
                    var generator = new MonthlyReportGenerator(repository);
                    var path = generator.Generate(year, month);
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

        internal static (int Year, int Month) ParseArgs(string[] args)
        {
            int? year = null;
            int? month = null;

            for (var i = 0; i < args.Length; i++)
            {
                if (i + 1 >= args.Length)
                {
                    break;
                }

                if (string.Equals(args[i], "--year", StringComparison.OrdinalIgnoreCase))
                {
                    year = int.Parse(args[++i], CultureInfo.InvariantCulture);
                }
                else if (string.Equals(args[i], "--month", StringComparison.OrdinalIgnoreCase))
                {
                    month = int.Parse(args[++i], CultureInfo.InvariantCulture);
                }
            }

            if (year.HasValue && month.HasValue)
            {
                return (year.Value, month.Value);
            }

            var previousMonth = DateTime.UtcNow.AddMonths(-1);
            return (year ?? previousMonth.Year, month ?? previousMonth.Month);
        }
    }
}
