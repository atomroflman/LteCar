using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LteCar.Server.Migrations
{
    /// <inheritdoc />
    public partial class SetupFunctionNameInsteadId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SetupFunctionId",
                table: "UserSetupFlowNodes");

            migrationBuilder.AddColumn<string>(
                name: "SetupFunctionName",
                table: "UserSetupFlowNodes",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SetupFunctionName",
                table: "UserSetupFlowNodes");

            migrationBuilder.AddColumn<int>(
                name: "SetupFunctionId",
                table: "UserSetupFlowNodes",
                type: "INTEGER",
                nullable: true);
        }
    }
}
