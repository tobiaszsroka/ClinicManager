using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManager.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicationPrescriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PrescribedMedications_Medications_MedicationId",
                table: "PrescribedMedications");

            migrationBuilder.DropForeignKey(
                name: "FK_PrescribedMedications_ProceduresPerformed_ProcedurePerformedId",
                table: "PrescribedMedications");

            migrationBuilder.RenameColumn(
                name: "ProcedurePerformedId",
                table: "PrescribedMedications",
                newName: "VisitId");

            migrationBuilder.RenameIndex(
                name: "IX_PrescribedMedications_ProcedurePerformedId",
                table: "PrescribedMedications",
                newName: "IX_PrescribedMedications_VisitId");

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPriceAtPrescription",
                table: "PrescribedMedications",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql(
                """
                UPDATE prescriptions
                SET prescriptions.VisitId = procedures.VisitId,
                    prescriptions.UnitPriceAtPrescription = medications.UnitPrice
                FROM PrescribedMedications AS prescriptions
                INNER JOIN ProceduresPerformed AS procedures ON procedures.Id = prescriptions.VisitId
                INNER JOIN Medications AS medications ON medications.Id = prescriptions.MedicationId;
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_PrescribedMedications_Medications_MedicationId",
                table: "PrescribedMedications",
                column: "MedicationId",
                principalTable: "Medications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PrescribedMedications_Visits_VisitId",
                table: "PrescribedMedications",
                column: "VisitId",
                principalTable: "Visits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PrescribedMedications_Medications_MedicationId",
                table: "PrescribedMedications");

            migrationBuilder.DropForeignKey(
                name: "FK_PrescribedMedications_Visits_VisitId",
                table: "PrescribedMedications");

            migrationBuilder.DropColumn(
                name: "UnitPriceAtPrescription",
                table: "PrescribedMedications");

            migrationBuilder.RenameColumn(
                name: "VisitId",
                table: "PrescribedMedications",
                newName: "ProcedurePerformedId");

            migrationBuilder.RenameIndex(
                name: "IX_PrescribedMedications_VisitId",
                table: "PrescribedMedications",
                newName: "IX_PrescribedMedications_ProcedurePerformedId");

            migrationBuilder.AddForeignKey(
                name: "FK_PrescribedMedications_Medications_MedicationId",
                table: "PrescribedMedications",
                column: "MedicationId",
                principalTable: "Medications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PrescribedMedications_ProceduresPerformed_ProcedurePerformedId",
                table: "PrescribedMedications",
                column: "ProcedurePerformedId",
                principalTable: "ProceduresPerformed",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
