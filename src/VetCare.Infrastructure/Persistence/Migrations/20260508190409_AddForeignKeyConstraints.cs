using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VetCare.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddForeignKeyConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_vaccinations_PetId",
                schema: "vetcare",
                table: "vaccinations",
                column: "PetId");

            migrationBuilder.CreateIndex(
                name: "IX_pets_OwnerId",
                schema: "vetcare",
                table: "pets",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_appointments_PetId",
                schema: "vetcare",
                table: "appointments",
                column: "PetId");

            migrationBuilder.CreateIndex(
                name: "IX_appointments_VetUserId",
                schema: "vetcare",
                table: "appointments",
                column: "VetUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_appointments_pets_PetId",
                schema: "vetcare",
                table: "appointments",
                column: "PetId",
                principalSchema: "vetcare",
                principalTable: "pets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_appointments_users_VetUserId",
                schema: "vetcare",
                table: "appointments",
                column: "VetUserId",
                principalSchema: "vetcare",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_owners_tenants_TenantId",
                schema: "vetcare",
                table: "owners",
                column: "TenantId",
                principalSchema: "vetcare",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_pets_owners_OwnerId",
                schema: "vetcare",
                table: "pets",
                column: "OwnerId",
                principalSchema: "vetcare",
                principalTable: "owners",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_pets_tenants_TenantId",
                schema: "vetcare",
                table: "pets",
                column: "TenantId",
                principalSchema: "vetcare",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_users_tenants_TenantId",
                schema: "vetcare",
                table: "users",
                column: "TenantId",
                principalSchema: "vetcare",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_vaccinations_pets_PetId",
                schema: "vetcare",
                table: "vaccinations",
                column: "PetId",
                principalSchema: "vetcare",
                principalTable: "pets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_appointments_pets_PetId",
                schema: "vetcare",
                table: "appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_appointments_users_VetUserId",
                schema: "vetcare",
                table: "appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_owners_tenants_TenantId",
                schema: "vetcare",
                table: "owners");

            migrationBuilder.DropForeignKey(
                name: "FK_pets_owners_OwnerId",
                schema: "vetcare",
                table: "pets");

            migrationBuilder.DropForeignKey(
                name: "FK_pets_tenants_TenantId",
                schema: "vetcare",
                table: "pets");

            migrationBuilder.DropForeignKey(
                name: "FK_users_tenants_TenantId",
                schema: "vetcare",
                table: "users");

            migrationBuilder.DropForeignKey(
                name: "FK_vaccinations_pets_PetId",
                schema: "vetcare",
                table: "vaccinations");

            migrationBuilder.DropIndex(
                name: "IX_vaccinations_PetId",
                schema: "vetcare",
                table: "vaccinations");

            migrationBuilder.DropIndex(
                name: "IX_pets_OwnerId",
                schema: "vetcare",
                table: "pets");

            migrationBuilder.DropIndex(
                name: "IX_appointments_PetId",
                schema: "vetcare",
                table: "appointments");

            migrationBuilder.DropIndex(
                name: "IX_appointments_VetUserId",
                schema: "vetcare",
                table: "appointments");
        }
    }
}
