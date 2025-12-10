using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class updatemedicaltest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PrescriptionId",
                table: "MedicalTests",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicalTests_PrescriptionId",
                table: "MedicalTests",
                column: "PrescriptionId");

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalTests_Prescriptions_PrescriptionId",
                table: "MedicalTests",
                column: "PrescriptionId",
                principalTable: "Prescriptions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicalTests_Prescriptions_PrescriptionId",
                table: "MedicalTests");

            migrationBuilder.DropIndex(
                name: "IX_MedicalTests_PrescriptionId",
                table: "MedicalTests");

            migrationBuilder.DropColumn(
                name: "PrescriptionId",
                table: "MedicalTests");
        }
    }
}
