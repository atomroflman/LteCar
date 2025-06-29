using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LteCar.Server.Migrations
{
    /// <inheritdoc />
    public partial class MadeUserControlDeviceNameUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserChannelDevice_UserId",
                table: "UserChannelDevice");

            migrationBuilder.CreateIndex(
                name: "IX_UserChannelDevice_UserId_DeviceName",
                table: "UserChannelDevice",
                columns: new[] { "UserId", "DeviceName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserChannelDevice_UserId_DeviceName",
                table: "UserChannelDevice");

            migrationBuilder.CreateIndex(
                name: "IX_UserChannelDevice_UserId",
                table: "UserChannelDevice",
                column: "UserId");
        }
    }
}
