using System;
using System.Collections.Generic;
using System.IO;
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
    public sealed class MonthlyReportGeneratorTests : IDisposable
    {
        private static readonly Guid SampleTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        private readonly string _outputDirectory =
            Path.Combine(Path.GetTempPath(), "vetcare-legacy-reports-" + Guid.NewGuid().ToString("N"));

        [Fact]
        public void Generate_with_no_appointments_writes_pdf_at_expected_path()
        {
            var repository = new StubRepository(new List<AppointmentReport>());
            var generator = new MonthlyReportGenerator(repository);

            var path = generator.Generate(SampleTenantId, 2026, 5, _outputDirectory);

            path.Should().Be(Path.Combine(_outputDirectory, "vetcare-2026-05.pdf"));
            File.Exists(path).Should().BeTrue();
            new FileInfo(path).Length.Should().BeGreaterThan(0);
            ReadFileMagic(path, 5).Should().Be("%PDF-");
        }

        [Fact]
        public void Generate_with_three_appointments_writes_pdf_with_each_row()
        {
            var rows = new List<AppointmentReport>
            {
                Sample("Rex", "Alice Doe", "vet1@vetcare.io", new DateTime(2026, 5, 3, 9, 0, 0), status: 1, vaccine: "Rabies"),
                Sample("Mia", "Bob Roe", "vet2@vetcare.io", new DateTime(2026, 5, 5, 10, 30, 0), status: 2, vaccine: null),
                Sample("Bolt", "Carol Poe", "vet3@vetcare.io", new DateTime(2026, 5, 9, 14, 15, 0), status: 3, vaccine: "DHPP"),
            };
            var repository = new StubRepository(rows);
            var generator = new MonthlyReportGenerator(repository);

            var path = generator.Generate(SampleTenantId, 2026, 5, _outputDirectory);

            File.Exists(path).Should().BeTrue();

            var text = ExtractText(path);
            text.Should().Contain("Rex");
            text.Should().Contain("Mia");
            text.Should().Contain("Bolt");
            text.Should().Contain("Alice Doe");
            text.Should().Contain("Bob Roe");
            text.Should().Contain("Carol Poe");
            text.Should().Contain("Rabies");
            text.Should().Contain("DHPP");
            text.Should().Contain("Monthly Report");
            text.Should().Contain("Generated at");
        }

        private static AppointmentReport Sample(string pet, string owner, string vet, DateTime when, int status, string vaccine)
        {
            return new AppointmentReport
            {
                Id = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                PetName = pet,
                OwnerName = owner,
                VetName = vet,
                ScheduledAt = when,
                Status = status,
                VaccineName = vaccine,
            };
        }

        private static string ReadFileMagic(string path, int bytes)
        {
            using (var stream = File.OpenRead(path))
            {
                var buffer = new byte[bytes];
                var read = stream.Read(buffer, 0, buffer.Length);
                return Encoding.ASCII.GetString(buffer, 0, read);
            }
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
