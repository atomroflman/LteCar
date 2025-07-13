using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LteCar.Server.Migrations
{
    /// <inheritdoc />
    public partial class FixFunctionNodeParameters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserSetupFunctionNodeParameter_UserSetupFlowNodes_UserSetupFunctionNodeId",
                table: "UserSetupFunctionNodeParameter");

            migrationBuilder.DropIndex(
                name: "IX_UserSetupFunctionNodeParameter_NodeId",
                table: "UserSetupFunctionNodeParameter");

            migrationBuilder.DropIndex(
                name: "IX_UserSetupFunctionNodeParameter_UserSetupFunctionNodeId_ParameterName",
                table: "UserSetupFunctionNodeParameter");

            migrationBuilder.DropColumn(
                name: "UserSetupFunctionNodeId",
                table: "UserSetupFunctionNodeParameter");

            migrationBuilder.CreateIndex(
                name: "IX_UserSetupFunctionNodeParameter_NodeId_ParameterName",
                table: "UserSetupFunctionNodeParameter",
                columns: new[] { "NodeId", "ParameterName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserSetupFunctionNodeParameter_NodeId_ParameterName",
                table: "UserSetupFunctionNodeParameter");

            migrationBuilder.AddColumn<int>(
                name: "UserSetupFunctionNodeId",
                table: "UserSetupFunctionNodeParameter",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

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
                name: "FK_UserSetupFunctionNodeParameter_UserSetupFlowNodes_UserSetupFunctionNodeId",
                table: "UserSetupFunctionNodeParameter",
                column: "UserSetupFunctionNodeId",
                principalTable: "UserSetupFlowNodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
