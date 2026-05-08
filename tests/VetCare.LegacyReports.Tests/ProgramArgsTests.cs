using System;
using System.Reflection;
using FluentAssertions;
using VetCare.LegacyReports.Data;
using Xunit;

namespace VetCare.LegacyReports.Tests
{
    public sealed class ProgramArgsTests
    {
        private static readonly MethodInfo TryParseArgsMethod = typeof(EfAppointmentReportRepository)
            .Assembly
            .GetType("VetCare.LegacyReports.Program", throwOnError: true)
            .GetMethod("TryParseArgs", BindingFlags.NonPublic | BindingFlags.Static);

        private static bool TryParseArgs(string[] args, out Guid tenantId, out int year, out int month, out string error)
        {
            var parameters = new object[] { args, Guid.Empty, 0, 0, null };
            var ok = (bool)TryParseArgsMethod.Invoke(null, parameters);
            tenantId = (Guid)parameters[1];
            year = (int)parameters[2];
            month = (int)parameters[3];
            error = (string)parameters[4];
            return ok;
        }

        [Fact]
        public void Returns_false_when_tenant_id_is_missing()
        {
            var ok = TryParseArgs(new[] { "--year", "2026", "--month", "5" }, out _, out _, out _, out var error);

            ok.Should().BeFalse();
            error.Should().Contain("--tenant-id");
        }

        [Fact]
        public void Returns_false_when_tenant_id_is_not_a_guid()
        {
            var ok = TryParseArgs(new[] { "--tenant-id", "not-a-guid" }, out _, out _, out _, out var error);

            ok.Should().BeFalse();
            error.Should().Contain("--tenant-id");
        }

        [Fact]
        public void Returns_false_when_tenant_id_is_empty_guid()
        {
            var ok = TryParseArgs(
                new[] { "--tenant-id", Guid.Empty.ToString() },
                out _, out _, out _, out var error);

            ok.Should().BeFalse();
            error.Should().Contain("--tenant-id");
        }

        [Fact]
        public void Returns_true_with_valid_tenant_year_and_month()
        {
            var tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");

            var ok = TryParseArgs(
                new[] { "--tenant-id", tenant.ToString(), "--year", "2026", "--month", "3" },
                out var tenantId, out var year, out var month, out var error);

            ok.Should().BeTrue();
            error.Should().BeNull();
            tenantId.Should().Be(tenant);
            year.Should().Be(2026);
            month.Should().Be(3);
        }

        [Fact]
        public void Defaults_to_previous_month_when_year_and_month_are_missing()
        {
            var tenant = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var expected = DateTime.UtcNow.AddMonths(-1);

            var ok = TryParseArgs(
                new[] { "--tenant-id", tenant.ToString() },
                out var tenantId, out var year, out var month, out var error);

            ok.Should().BeTrue();
            error.Should().BeNull();
            tenantId.Should().Be(tenant);
            year.Should().Be(expected.Year);
            month.Should().Be(expected.Month);
        }
    }
}
