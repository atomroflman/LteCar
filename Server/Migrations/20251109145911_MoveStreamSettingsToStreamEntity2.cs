using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LteCar.Server.Migrations
{
    /// <inheritdoc />
    public partial class MoveStreamSettingsToStreamEntity2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CarVideoStreams_Cars_CarId",
                table: "CarVideoStreams");

            migrationBuilder.DropIndex(
                name: "IX_CarVideoStreams_CarId_IsActive",
                table: "CarVideoStreams");

            migrationBuilder.DropColumn(
                name: "VideoBitrate",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "VideoBrightness",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "VideoFramerate",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "VideoHeight",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "VideoStreamPort",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "VideoWidth",
                table: "Cars");

            migrationBuilder.AddColumn<int>(
                name: "BitrateKbps",
                table: "CarVideoStreams",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<float>(
                name: "Brightness",
                table: "CarVideoStreams",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<int>(
                name: "Framerate",
                table: "CarVideoStreams",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Height",
                table: "CarVideoStreams",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Width",
                table: "CarVideoStreams",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_CarVideoStreams_Cars_CarId",
                table: "CarVideoStreams",
                column: "CarId",
                principalTable: "Cars",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CarVideoStreams_Cars_CarId",
                table: "CarVideoStreams");

            migrationBuilder.DropColumn(
                name: "BitrateKbps",
                table: "CarVideoStreams");

            migrationBuilder.DropColumn(
                name: "Brightness",
                table: "CarVideoStreams");

            migrationBuilder.DropColumn(
                name: "Framerate",
                table: "CarVideoStreams");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "CarVideoStreams");

            migrationBuilder.DropColumn(
                name: "Width",
                table: "CarVideoStreams");

            migrationBuilder.AddColumn<int>(
                name: "VideoBitrate",
                table: "Cars",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "VideoBrightness",
                table: "Cars",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VideoFramerate",
                table: "Cars",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VideoHeight",
                table: "Cars",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VideoStreamPort",
                table: "Cars",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VideoWidth",
                table: "Cars",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CarVideoStreams_CarId_IsActive",
                table: "CarVideoStreams",
                columns: new[] { "CarId", "IsActive" });

            migrationBuilder.AddForeignKey(
                name: "FK_CarVideoStreams_Cars_CarId",
                table: "CarVideoStreams",
                column: "CarId",
                principalTable: "Cars",
                principalColumn: "Id");
        }
    }
}
