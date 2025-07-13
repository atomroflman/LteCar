using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LteCar.Server.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cars",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    CarId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ChannelMapHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SeesionId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
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
                name: "SetupFilterType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TypeName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SetupFilterType", x => x.Id);
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

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    ActiveVehicleId = table.Column<int>(type: "INTEGER", nullable: true),
                    SessionToken = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    LastSeen = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Cars_ActiveVehicleId",
                        column: x => x.ActiveVehicleId,
                        principalTable: "Cars",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserCarSetup",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    CarId = table.Column<int>(type: "INTEGER", nullable: false),
                    CarSecret = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCarSetup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserCarSetup_Cars_CarId",
                        column: x => x.CarId,
                        principalTable: "Cars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserCarSetup_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSetupChannel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserSetupId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    ChannelId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsAxis = table.Column<bool>(type: "INTEGER", nullable: false),
                    CalibrationMin = table.Column<float>(type: "REAL", nullable: false),
                    CalibrationMax = table.Column<float>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSetupChannel", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSetupChannel_UserCarSetup_UserSetupId",
                        column: x => x.UserSetupId,
                        principalTable: "UserCarSetup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSetupFilter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserSetupId = table.Column<int>(type: "INTEGER", nullable: false),
                    SetupFilterTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Paramerters = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSetupFilter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSetupFilter_SetupFilterType_SetupFilterTypeId",
                        column: x => x.SetupFilterTypeId,
                        principalTable: "SetupFilterType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserSetupFilter_UserCarSetup_UserSetupId",
                        column: x => x.UserSetupId,
                        principalTable: "UserCarSetup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSetupLink",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserSetupId = table.Column<int>(type: "INTEGER", nullable: false),
                    ChannelSourceId = table.Column<int>(type: "INTEGER", nullable: true),
                    FilterSourceId = table.Column<int>(type: "INTEGER", nullable: true),
                    FilterTargetId = table.Column<int>(type: "INTEGER", nullable: true),
                    VehicleFunctionTargetId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSetupLink", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSetupLink_CarChannels_VehicleFunctionTargetId",
                        column: x => x.VehicleFunctionTargetId,
                        principalTable: "CarChannels",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserSetupLink_UserCarSetup_UserSetupId",
                        column: x => x.UserSetupId,
                        principalTable: "UserCarSetup",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserSetupLink_UserSetupChannel_ChannelSourceId",
                        column: x => x.ChannelSourceId,
                        principalTable: "UserSetupChannel",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserSetupLink_UserSetupFilter_FilterSourceId",
                        column: x => x.FilterSourceId,
                        principalTable: "UserSetupFilter",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserSetupLink_UserSetupFilter_FilterTargetId",
                        column: x => x.FilterTargetId,
                        principalTable: "UserSetupFilter",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CarChannels_CarId",
                table: "CarChannels",
                column: "CarId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCarSetup_CarId",
                table: "UserCarSetup",
                column: "CarId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCarSetup_UserId",
                table: "UserCarSetup",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ActiveVehicleId",
                table: "Users",
                column: "ActiveVehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSetupChannel_UserSetupId",
                table: "UserSetupChannel",
                column: "UserSetupId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSetupFilter_SetupFilterTypeId",
                table: "UserSetupFilter",
                column: "SetupFilterTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSetupFilter_UserSetupId",
                table: "UserSetupFilter",
                column: "UserSetupId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSetupLink_ChannelSourceId",
                table: "UserSetupLink",
                column: "ChannelSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSetupLink_FilterSourceId",
                table: "UserSetupLink",
                column: "FilterSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSetupLink_FilterTargetId",
                table: "UserSetupLink",
                column: "FilterTargetId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSetupLink_UserSetupId",
                table: "UserSetupLink",
                column: "UserSetupId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSetupLink_VehicleFunctionTargetId",
                table: "UserSetupLink",
                column: "VehicleFunctionTargetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSetupLink");

            migrationBuilder.DropTable(
                name: "CarChannels");

            migrationBuilder.DropTable(
                name: "UserSetupChannel");

            migrationBuilder.DropTable(
                name: "UserSetupFilter");

            migrationBuilder.DropTable(
                name: "SetupFilterType");

            migrationBuilder.DropTable(
                name: "UserCarSetup");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Cars");
        }
    }
}
