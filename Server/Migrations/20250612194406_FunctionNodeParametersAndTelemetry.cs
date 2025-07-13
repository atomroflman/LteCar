using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LteCar.Server.Migrations
{
    /// <inheritdoc />
    public partial class FunctionNodeParametersAndTelemetry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SeesionId",
                table: "Cars");

            migrationBuilder.AddColumn<int>(
                name: "TelemetryId",
                table: "UserSetupFlowNodes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserSetupFunctionNodeParameter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserSetupFunctionNodeId = table.Column<int>(type: "INTEGER", nullable: false),
                    ParameterName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ParameterValue = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    NodeId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSetupFunctionNodeParameter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSetupFunctionNodeParameter_UserSetupFlowNodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "UserSetupFlowNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserSetupFunctionNodeParameter_UserSetupFlowNodes_UserSetupFunctionNodeId",
                        column: x => x.UserSetupFunctionNodeId,
                        principalTable: "UserSetupFlowNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserSetupFlowNodes_TelemetryId",
                table: "UserSetupFlowNodes",
                column: "TelemetryId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSetupFunctionNodeParameter_NodeId",
                table: "UserSetupFunctionNodeParameter",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSetupFunctionNodeParameter_UserSetupFunctionNodeId_ParameterName",
                table: "UserSetupFunctionNodeParameter",
                columns: new[] { "UserSetupFunctionNodeId", "ParameterName" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_UserSetupFlowNodes_CarTelemetry_TelemetryId",
                table: "UserSetupFlowNodes",
                column: "TelemetryId",
                principalTable: "CarTelemetry",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserSetupFlowNodes_CarTelemetry_TelemetryId",
                table: "UserSetupFlowNodes");

            migrationBuilder.DropTable(
                name: "UserSetupFunctionNodeParameter");

            migrationBuilder.DropIndex(
                name: "IX_UserSetupFlowNodes_TelemetryId",
                table: "UserSetupFlowNodes");

            migrationBuilder.DropColumn(
                name: "TelemetryId",
                table: "UserSetupFlowNodes");

            migrationBuilder.AddColumn<string>(
                name: "SeesionId",
                table: "Cars",
                type: "TEXT",
                maxLength: 64,
                nullable: true);
        }
    }
}
