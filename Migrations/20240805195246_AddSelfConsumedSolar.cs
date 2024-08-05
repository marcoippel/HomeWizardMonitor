using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeWizardMonitor.Migrations
{
    /// <inheritdoc />
    public partial class AddSelfConsumedSolar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "SelfConsumedSolarKwh",
                table: "EnergyData",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SelfConsumedSolarKwh",
                table: "EnergyData");
        }
    }
}
