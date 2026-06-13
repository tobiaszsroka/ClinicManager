using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManager.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicalProcedures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MedicalRecordId",
                table: "Visits",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MedicalProcedures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BaseCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Discount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VisitId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalProcedures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicalProcedures_Visits_VisitId",
                        column: x => x.VisitId,
                        principalTable: "Visits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Visits_MedicalRecordId",
                table: "Visits",
                column: "MedicalRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalProcedures_VisitId",
                table: "MedicalProcedures",
                column: "VisitId");

            migrationBuilder.AddForeignKey(
                name: "FK_Visits_MedicalRecords_MedicalRecordId",
                table: "Visits",
                column: "MedicalRecordId",
                principalTable: "MedicalRecords",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Visits_MedicalRecords_MedicalRecordId",
                table: "Visits");

            migrationBuilder.DropTable(
                name: "MedicalProcedures");

            migrationBuilder.DropIndex(
                name: "IX_Visits_MedicalRecordId",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "MedicalRecordId",
                table: "Visits");
        }
    }
}
