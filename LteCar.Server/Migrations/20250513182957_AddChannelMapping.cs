using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LteCar.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddChannelMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Vehicles_ActiveVehicleId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSetupLink_UserVehicleSetup_UserSetupId",
                table: "UserSetupLink");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSetupLink_VehicleFunctions_VehicleFunctionTargetId",
                table: "UserSetupLink");

            migrationBuilder.DropForeignKey(
                name: "FK_UserVehicleSetup_Vehicles_VehicleId",
                table: "UserVehicleSetup");

            migrationBuilder.DropTable(
                name: "VehicleFunctions");

            migrationBuilder.DropTable(
                name: "Vehicles");

            migrationBuilder.CreateTable(
                name: "Cars",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    CarId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CarSecret = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ChannelMapHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SeesionId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    VideoStreamPort = table.Column<int>(type: "INTEGER", nullable: true),
                    LastSeen = table.Column<DateTime>(type: "TEXT", nullable: false),
                    VideoWidth = table.Column<int>(type: "INTEGER", nullable: true),
                    VideoHeight = table.Column<int>(type: "INTEGER", nullable: true),
                    VideoFramerate = table.Column<int>(type: "INTEGER", nullable: true),
                    VideoBrightness = table.Column<float>(type: "REAL", nullable: true),
                    VideoBitrate = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cars", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CarChannels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    ChannelName = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiresAxis = table.Column<bool>(type: "INTEGER", nullable: false),
                    CarId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarChannels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarChannels_Cars_CarId",
                        column: x => x.CarId,
                        principalTable: "Cars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CarChannels_CarId",
                table: "CarChannels",
                column: "CarId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Cars_ActiveVehicleId",
                table: "Users",
                column: "ActiveVehicleId",
                principalTable: "Cars",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserSetupLink_CarChannels_VehicleFunctionTargetId",
                table: "UserSetupLink",
                column: "VehicleFunctionTargetId",
                principalTable: "CarChannels",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserSetupLink_UserVehicleSetup_UserSetupId",
                table: "UserSetupLink",
                column: "UserSetupId",
                principalTable: "UserVehicleSetup",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserVehicleSetup_Cars_VehicleId",
                table: "UserVehicleSetup",
                column: "VehicleId",
                principalTable: "Cars",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Cars_ActiveVehicleId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSetupLink_CarChannels_VehicleFunctionTargetId",
                table: "UserSetupLink");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSetupLink_UserVehicleSetup_UserSetupId",
                table: "UserSetupLink");

            migrationBuilder.DropForeignKey(
                name: "FK_UserVehicleSetup_Cars_VehicleId",
                table: "UserVehicleSetup");

            migrationBuilder.DropTable(
                name: "CarChannels");

            migrationBuilder.DropTable(
                name: "Cars");

            migrationBuilder.CreateTable(
                name: "Vehicles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CarId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CarSecret = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ConfigHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    LastSeen = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    SeesionId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VehicleFunctions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VehicleId = table.Column<int>(type: "INTEGER", nullable: false),
                    ChannelName = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiresAxis = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleFunctions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleFunctions_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleFunctions_VehicleId",
                table: "VehicleFunctions",
                column: "VehicleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Vehicles_ActiveVehicleId",
                table: "Users",
                column: "ActiveVehicleId",
                principalTable: "Vehicles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserSetupLink_UserVehicleSetup_UserSetupId",
                table: "UserSetupLink",
                column: "UserSetupId",
                principalTable: "UserVehicleSetup",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserSetupLink_VehicleFunctions_VehicleFunctionTargetId",
                table: "UserSetupLink",
                column: "VehicleFunctionTargetId",
                principalTable: "VehicleFunctions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserVehicleSetup_Vehicles_VehicleId",
                table: "UserVehicleSetup",
                column: "VehicleId",
                principalTable: "Vehicles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
