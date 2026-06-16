using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManager.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Visits_AssignedDoctorId",
                table: "Visits");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_AssignedDoctorId_ScheduledDate",
                table: "Visits",
                columns: new[] { "AssignedDoctorId", "ScheduledDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Visits_Status_ScheduledDate",
                table: "Visits",
                columns: new[] { "Status", "ScheduledDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Patients_LastName_FirstName",
                table: "Patients",
                columns: new[] { "LastName", "FirstName" });

            migrationBuilder.CreateIndex(
                name: "IX_Patients_Pesel",
                table: "Patients",
                column: "Pesel");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Visits_AssignedDoctorId_ScheduledDate",
                table: "Visits");

            migrationBuilder.DropIndex(
                name: "IX_Visits_Status_ScheduledDate",
                table: "Visits");

            migrationBuilder.DropIndex(
                name: "IX_Patients_LastName_FirstName",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_Patients_Pesel",
                table: "Patients");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_AssignedDoctorId",
                table: "Visits",
                column: "AssignedDoctorId");
        }
    }
}
