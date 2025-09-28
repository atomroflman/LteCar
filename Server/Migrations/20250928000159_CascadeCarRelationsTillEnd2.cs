using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LteCar.Server.Migrations
{
    /// <inheritdoc />
    public partial class CascadeCarRelationsTillEnd2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserChannelDevice_Users_UserId",
                table: "UserChannelDevice");

            migrationBuilder.AddForeignKey(
                name: "FK_UserChannelDevice_Users_UserId",
                table: "UserChannelDevice",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserChannelDevice_Users_UserId",
                table: "UserChannelDevice");

            migrationBuilder.AddForeignKey(
                name: "FK_UserChannelDevice_Users_UserId",
                table: "UserChannelDevice",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
