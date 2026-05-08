using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VetCare.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompositeIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_vaccinations_TenantId_AdministeredAt",
                schema: "vetcare",
                table: "vaccinations",
                columns: new[] { "TenantId", "AdministeredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_pets_TenantId_Name",
                schema: "vetcare",
                table: "pets",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_owners_TenantId_FullName",
                schema: "vetcare",
                table: "owners",
                columns: new[] { "TenantId", "FullName" });

            migrationBuilder.CreateIndex(
                name: "IX_appointments_TenantId_Status",
                schema: "vetcare",
                table: "appointments",
                columns: new[] { "TenantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_vaccinations_TenantId_AdministeredAt",
                schema: "vetcare",
                table: "vaccinations");

            migrationBuilder.DropIndex(
                name: "IX_pets_TenantId_Name",
                schema: "vetcare",
                table: "pets");

            migrationBuilder.DropIndex(
                name: "IX_owners_TenantId_FullName",
                schema: "vetcare",
                table: "owners");

            migrationBuilder.DropIndex(
                name: "IX_appointments_TenantId_Status",
                schema: "vetcare",
                table: "appointments");
        }
    }
}
