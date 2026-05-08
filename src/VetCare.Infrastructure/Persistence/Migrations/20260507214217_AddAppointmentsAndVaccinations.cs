using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VetCare.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentsAndVaccinations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "appointments",
                schema: "vetcare",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PetId = table.Column<Guid>(type: "uuid", nullable: false),
                    VetUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appointments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "vaccinations",
                schema: "vetcare",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PetId = table.Column<Guid>(type: "uuid", nullable: false),
                    VaccineName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    AdministeredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NextDueAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BatchNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vaccinations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_appointments_TenantId_PetId",
                schema: "vetcare",
                table: "appointments",
                columns: new[] { "TenantId", "PetId" });

            migrationBuilder.CreateIndex(
                name: "IX_appointments_TenantId_ScheduledAt",
                schema: "vetcare",
                table: "appointments",
                columns: new[] { "TenantId", "ScheduledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_vaccinations_TenantId_PetId",
                schema: "vetcare",
                table: "vaccinations",
                columns: new[] { "TenantId", "PetId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "appointments",
                schema: "vetcare");

            migrationBuilder.DropTable(
                name: "vaccinations",
                schema: "vetcare");
        }
    }
}
