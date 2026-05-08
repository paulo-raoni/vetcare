using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using iTextSharp.text.pdf;
using VetCare.LegacyReports.Data;
using VetCare.LegacyReports.Reports;
using Xunit;
using PdfTextExtractor = iTextSharp.text.pdf.parser.PdfTextExtractor;
using Path = System.IO.Path;

namespace VetCare.LegacyReports.Tests
{
    public sealed class StatusLabelTests : IDisposable
    {
        private static readonly Guid TenantId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        private readonly string _outputDirectory =
            Path.Combine(Path.GetTempPath(), "vetcare-legacy-status-" + Guid.NewGuid().ToString("N"));

        // Domain enum: Scheduled = 1, Confirmed = 2, Cancelled = 3, Completed = 4 (EF stores int).
        [Theory]
        [InlineData(1, "Scheduled")]
        [InlineData(2, "Confirmed")]
        [InlineData(3, "Cancelled")]
        [InlineData(4, "Completed")]
        public void Generated_pdf_shows_label_matching_domain_enum_value(int status, string expectedLabel)
        {
            var repository = new StubRepository(new List<AppointmentReport>
            {
                new AppointmentReport
                {
                    Id = Guid.NewGuid(),
                    TenantId = TenantId,
                    PetName = "Rex",
                    OwnerName = "Alice Doe",
                    VetName = "vet@vetcare.io",
                    ScheduledAt = new DateTime(2026, 5, 3, 9, 0, 0),
                    Status = status,
                    VaccineName = null,
                },
            });

            var generator = new MonthlyReportGenerator(repository);
            var path = generator.Generate(TenantId, 2026, 5, _outputDirectory);

            ExtractText(path).Should().Contain(expectedLabel);
        }

        private static string ExtractText(string path)
        {
            var sb = new StringBuilder();
            using (var reader = new PdfReader(path))
            {
                for (var page = 1; page <= reader.NumberOfPages; page++)
                {
                    sb.AppendLine(PdfTextExtractor.GetTextFromPage(reader, page));
                }
            }
            return sb.ToString();
        }

        public void Dispose()
        {
            if (Directory.Exists(_outputDirectory))
            {
                Directory.Delete(_outputDirectory, recursive: true);
            }
        }

        private sealed class StubRepository : IAppointmentReportRepository
        {
            private readonly IReadOnlyList<AppointmentReport> _rows;

            public StubRepository(IReadOnlyList<AppointmentReport> rows)
            {
                _rows = rows;
            }

            public IReadOnlyList<AppointmentReport> GetForMonth(Guid tenantId, int year, int month)
            {
                return _rows;
            }
        }
    }
}
