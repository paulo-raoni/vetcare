using System;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using VetCare.LegacyReports.Data;
using Xunit;

namespace VetCare.LegacyReports.Tests
{
    /// <summary>
    /// Unit-level guards for the legacy EF6 repository. The actual SqlQuery call requires a
    /// live Postgres host so it is exercised end-to-end manually, but the tenant-scoping
    /// contract (parameter required + SQL fragment present) is asserted here.
    /// </summary>
    public sealed class EfAppointmentReportRepositoryTests
    {
        [Fact]
        public void Sql_filters_by_tenant_id_parameter()
        {
            var sqlField = typeof(EfAppointmentReportRepository).GetField(
                "Sql",
                BindingFlags.NonPublic | BindingFlags.Static);
            sqlField.Should().NotBeNull("the repository must expose a parameterised SQL constant");

            var sql = (string)sqlField.GetRawConstantValue();
            sql.Should().Contain("a.\"TenantId\" = @tenantId",
                "every query must be scoped to the supplied tenant");
            sql.Should().Contain("@year");
            sql.Should().Contain("@month");
        }

        [Fact]
        public void GetForMonth_throws_when_tenant_id_is_empty()
        {
            var repository = new EfAppointmentReportRepository(new LegacyDbContext());

            Action act = () => repository.GetForMonth(Guid.Empty, 2026, 5);

            act.Should()
                .Throw<ArgumentException>()
                .WithMessage("Tenant id must not be empty.*")
                .And.ParamName.Should().Be("tenantId");
        }

        [Fact]
        public void GetForMonth_throws_when_month_is_out_of_range()
        {
            var repository = new EfAppointmentReportRepository(new LegacyDbContext());
            var tenant = Guid.Parse("33333333-3333-3333-3333-333333333333");

            Action tooLow = () => repository.GetForMonth(tenant, 2026, 0);
            Action tooHigh = () => repository.GetForMonth(tenant, 2026, 13);

            tooLow.Should().Throw<ArgumentOutOfRangeException>().And.ParamName.Should().Be("month");
            tooHigh.Should().Throw<ArgumentOutOfRangeException>().And.ParamName.Should().Be("month");
        }
    }
}
