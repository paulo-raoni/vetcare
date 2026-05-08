using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;
using VetCare.LegacyReports.Data;

namespace VetCare.LegacyReports.Reports
{
    public sealed class MonthlyReportGenerator
    {
        public const string DefaultOutputDirectory = "reports";

        private static readonly string[] StatusLabels =
        {
            "Unknown",
            "Scheduled",
            "Confirmed",
            "Cancelled",
            "Completed",
        };

        private readonly IAppointmentReportRepository _repository;

        public MonthlyReportGenerator(IAppointmentReportRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public string Generate(Guid tenantId, int year, int month)
        {
            return Generate(tenantId, year, month, DefaultOutputDirectory);
        }

        public string Generate(Guid tenantId, int year, int month, string outputDirectory)
        {
            if (tenantId == Guid.Empty)
            {
                throw new ArgumentException("Tenant id must not be empty.", nameof(tenantId));
            }

            if (month < 1 || month > 12)
            {
                throw new ArgumentOutOfRangeException(nameof(month), month, "Month must be between 1 and 12.");
            }

            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                throw new ArgumentException("Output directory must not be empty.", nameof(outputDirectory));
            }

            Directory.CreateDirectory(outputDirectory);

            var fileName = string.Format(CultureInfo.InvariantCulture, "vetcare-{0:D4}-{1:D2}.pdf", year, month);
            var path = Path.Combine(outputDirectory, fileName);

            var rows = _repository.GetForMonth(tenantId, year, month);

            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                var document = new Document(PageSize.A4, 36f, 36f, 54f, 54f);
                var writer = PdfWriter.GetInstance(document, stream);
                writer.PageEvent = new FooterPageEvent();

                document.Open();
                AddHeader(document, year, month);
                AddTable(document, rows);
                document.Close();
            }

            return path;
        }

        private static void AddHeader(Document document, int year, int month)
        {
            var monthLabel = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month);
            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16f, BaseColor.BLACK);
            var title = new Paragraph(string.Format(CultureInfo.InvariantCulture, "VetCare — Monthly Report {0}/{1}", monthLabel, year), titleFont)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 18f,
            };
            document.Add(title);
        }

        private static void AddTable(Document document, IReadOnlyList<AppointmentReport> rows)
        {
            var table = new PdfPTable(6) { WidthPercentage = 100f };
            table.SetWidths(new[] { 2f, 2f, 2f, 2f, 1.4f, 2f });

            AddHeaderCell(table, "Pet");
            AddHeaderCell(table, "Owner");
            AddHeaderCell(table, "Vet");
            AddHeaderCell(table, "Date");
            AddHeaderCell(table, "Status");
            AddHeaderCell(table, "Vaccine");

            var cellFont = FontFactory.GetFont(FontFactory.HELVETICA, 9f, BaseColor.BLACK);

            if (rows.Count == 0)
            {
                var emptyCell = new PdfPCell(new Phrase("No appointments in this period.", cellFont))
                {
                    Colspan = 6,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    Padding = 8f,
                };
                table.AddCell(emptyCell);
            }
            else
            {
                foreach (var row in rows)
                {
                    AddBodyCell(table, row.PetName, cellFont);
                    AddBodyCell(table, row.OwnerName, cellFont);
                    AddBodyCell(table, row.VetName, cellFont);
                    AddBodyCell(table, row.ScheduledAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture), cellFont);
                    AddBodyCell(table, FormatStatus(row.Status), cellFont);
                    AddBodyCell(table, string.IsNullOrWhiteSpace(row.VaccineName) ? "-" : row.VaccineName, cellFont);
                }
            }

            document.Add(table);
        }

        private static void AddHeaderCell(PdfPTable table, string text)
        {
            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10f, BaseColor.BLACK);
            var cell = new PdfPCell(new Phrase(text, headerFont))
            {
                BackgroundColor = BaseColor.LIGHT_GRAY,
                HorizontalAlignment = Element.ALIGN_CENTER,
                Padding = 6f,
            };
            table.AddCell(cell);
        }

        private static void AddBodyCell(PdfPTable table, string text, Font font)
        {
            var cell = new PdfPCell(new Phrase(text ?? string.Empty, font))
            {
                Padding = 4f,
            };
            table.AddCell(cell);
        }

        private static string FormatStatus(int status)
        {
            return status >= 0 && status < StatusLabels.Length
                ? StatusLabels[status]
                : status.ToString(CultureInfo.InvariantCulture);
        }

        private sealed class FooterPageEvent : PdfPageEventHelper
        {
            private readonly string _generatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);
            private readonly Font _font = FontFactory.GetFont(FontFactory.HELVETICA, 8f, BaseColor.GRAY);

            public override void OnEndPage(PdfWriter writer, Document document)
            {
                var text = string.Format(
                    CultureInfo.InvariantCulture,
                    "Generated at {0}    Page {1}",
                    _generatedAt,
                    writer.PageNumber);

                var phrase = new Phrase(text, _font);
                var x = (document.Right + document.Left) / 2f;
                var y = document.Bottom - 20f;

                ColumnText.ShowTextAligned(writer.DirectContent, Element.ALIGN_CENTER, phrase, x, y, 0f);
            }
        }
    }
}
