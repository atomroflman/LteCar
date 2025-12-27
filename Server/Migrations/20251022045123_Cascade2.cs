using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LteCar.Server.Migrations
{
    /// <inheritdoc />
    public partial class Cascade2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserSetupFlowNodes_CarChannels_CarChannelId",
                table: "UserSetupFlowNodes");

            migrationBuilder.AddForeignKey(
                name: "FK_UserSetupFlowNodes_CarChannels_CarChannelId",
                table: "UserSetupFlowNodes",
                column: "CarChannelId",
                principalTable: "CarChannels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserSetupFlowNodes_CarChannels_CarChannelId",
                table: "UserSetupFlowNodes");

            migrationBuilder.AddForeignKey(
                name: "FK_UserSetupFlowNodes_CarChannels_CarChannelId",
                table: "UserSetupFlowNodes",
                column: "CarChannelId",
                principalTable: "CarChannels",
                principalColumn: "Id");
        }
    }
}
