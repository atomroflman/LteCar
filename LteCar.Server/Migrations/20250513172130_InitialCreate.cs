using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LteCar.Server.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "Vehicles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    CarId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CarSecret = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ConfigHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SeesionId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    LastSeen = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.Id);
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
                        name: "FK_Users_Vehicles_ActiveVehicleId",
                        column: x => x.ActiveVehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VehicleFunctions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    ChannelName = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiresAxis = table.Column<bool>(type: "INTEGER", nullable: false),
                    VehicleId = table.Column<int>(type: "INTEGER", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "UserVehicleSetup",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    VehicleId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserVehicleSetup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserVehicleSetup_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserVehicleSetup_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
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
                        name: "FK_UserSetupChannel_UserVehicleSetup_UserSetupId",
                        column: x => x.UserSetupId,
                        principalTable: "UserVehicleSetup",
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
                        name: "FK_UserSetupFilter_UserVehicleSetup_UserSetupId",
                        column: x => x.UserSetupId,
                        principalTable: "UserVehicleSetup",
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
                    table.ForeignKey(
                        name: "FK_UserSetupLink_UserVehicleSetup_UserSetupId",
                        column: x => x.UserSetupId,
                        principalTable: "UserVehicleSetup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserSetupLink_VehicleFunctions_VehicleFunctionTargetId",
                        column: x => x.VehicleFunctionTargetId,
                        principalTable: "VehicleFunctions",
                        principalColumn: "Id");
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_UserVehicleSetup_UserId",
                table: "UserVehicleSetup",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserVehicleSetup_VehicleId",
                table: "UserVehicleSetup",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleFunctions_VehicleId",
                table: "VehicleFunctions",
                column: "VehicleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSetupLink");

            migrationBuilder.DropTable(
                name: "UserSetupChannel");

            migrationBuilder.DropTable(
                name: "UserSetupFilter");

            migrationBuilder.DropTable(
                name: "VehicleFunctions");

            migrationBuilder.DropTable(
                name: "SetupFilterType");

            migrationBuilder.DropTable(
                name: "UserVehicleSetup");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Vehicles");
        }
    }
}
