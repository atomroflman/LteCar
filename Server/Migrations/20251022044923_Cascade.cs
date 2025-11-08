using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LteCar.Server.Migrations
{
    /// <inheritdoc />
    public partial class Cascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserSetupFlowNodes_UserChannel_UserChannelId",
                table: "UserSetupFlowNodes");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSetupFunctionNodeParameter_UserSetupFlowNodes_NodeId",
                table: "UserSetupFunctionNodeParameter");

            migrationBuilder.AddForeignKey(
                name: "FK_UserSetupFlowNodes_UserChannel_UserChannelId",
                table: "UserSetupFlowNodes",
                column: "UserChannelId",
                principalTable: "UserChannel",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserSetupFunctionNodeParameter_UserSetupFlowNodes_NodeId",
                table: "UserSetupFunctionNodeParameter",
                column: "NodeId",
                principalTable: "UserSetupFlowNodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserSetupFlowNodes_UserChannel_UserChannelId",
                table: "UserSetupFlowNodes");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSetupFunctionNodeParameter_UserSetupFlowNodes_NodeId",
                table: "UserSetupFunctionNodeParameter");

            migrationBuilder.AddForeignKey(
                name: "FK_UserSetupFlowNodes_UserChannel_UserChannelId",
                table: "UserSetupFlowNodes",
                column: "UserChannelId",
                principalTable: "UserChannel",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserSetupFunctionNodeParameter_UserSetupFlowNodes_NodeId",
                table: "UserSetupFunctionNodeParameter",
                column: "NodeId",
                principalTable: "UserSetupFlowNodes",
                principalColumn: "Id");
        }
    }
}
