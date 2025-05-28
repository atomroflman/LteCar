using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LteCar.Server.Migrations
{
    /// <inheritdoc />
    public partial class RestructureDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PositionX",
                table: "UserChannel");

            migrationBuilder.DropColumn(
                name: "PositionY",
                table: "UserChannel");

            migrationBuilder.RenameColumn(
                name: "UserDeviceName",
                table: "UserChannelDevice",
                newName: "DeviceName");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "UserChannelDevice",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<float>(
                name: "CalibrationMin",
                table: "UserChannel",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(float),
                oldType: "REAL");

            migrationBuilder.AlterColumn<float>(
                name: "CalibrationMax",
                table: "UserChannel",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(float),
                oldType: "REAL");

            migrationBuilder.CreateIndex(
                name: "IX_UserChannelDevice_UserId",
                table: "UserChannelDevice",
                column: "UserId");

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

            migrationBuilder.DropIndex(
                name: "IX_UserChannelDevice_UserId",
                table: "UserChannelDevice");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "UserChannelDevice");

            migrationBuilder.RenameColumn(
                name: "DeviceName",
                table: "UserChannelDevice",
                newName: "UserDeviceName");

            migrationBuilder.AlterColumn<float>(
                name: "CalibrationMin",
                table: "UserChannel",
                type: "REAL",
                nullable: false,
                defaultValue: 0f,
                oldClrType: typeof(float),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AlterColumn<float>(
                name: "CalibrationMax",
                table: "UserChannel",
                type: "REAL",
                nullable: false,
                defaultValue: 0f,
                oldClrType: typeof(float),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AddColumn<float>(
                name: "PositionX",
                table: "UserChannel",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "PositionY",
                table: "UserChannel",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);
        }
    }
}
