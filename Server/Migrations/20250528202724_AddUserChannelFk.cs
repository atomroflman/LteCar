using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LteCar.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddUserChannelFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserChannel_UserChannelDeviceId",
                table: "UserChannel");

            migrationBuilder.CreateIndex(
                name: "IX_UserChannel_UserChannelDeviceId_IsAxis_ChannelId",
                table: "UserChannel",
                columns: new[] { "UserChannelDeviceId", "IsAxis", "ChannelId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserChannel_UserChannelDeviceId_IsAxis_ChannelId",
                table: "UserChannel");

            migrationBuilder.CreateIndex(
                name: "IX_UserChannel_UserChannelDeviceId",
                table: "UserChannel",
                column: "UserChannelDeviceId");
        }
    }
}
