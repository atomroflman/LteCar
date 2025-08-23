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
                name: "SetupFilterTypes",
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
                    table.PrimaryKey("PK_SetupFilterTypes", x => x.Id);
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
                name: "CarTelemetry",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChannelName = table.Column<string>(type: "TEXT", nullable: false),
                    CarId = table.Column<int>(type: "INTEGER", nullable: false),
                    ReadIntervalTicks = table.Column<int>(type: "INTEGER", nullable: false),
                    TelemetryType = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarTelemetry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarTelemetry_Cars_CarId",
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
                    SessionToken = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
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
                name: "UserChannelDevice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DeviceName = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserChannelDevice", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserChannelDevice_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSetups",
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
                    table.PrimaryKey("PK_UserSetups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSetups_Cars_CarId",
                        column: x => x.CarId,
                        principalTable: "Cars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserSetups_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserChannel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserChannelDeviceId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    ChannelId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsAxis = table.Column<bool>(type: "INTEGER", nullable: false),
                    Accuracy = table.Column<int>(type: "INTEGER", nullable: false),
                    CalibrationMin = table.Column<float>(type: "REAL", nullable: true),
                    CalibrationMax = table.Column<float>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserChannel", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserChannel_UserChannelDevice_UserChannelDeviceId",
                        column: x => x.UserChannelDeviceId,
                        principalTable: "UserChannelDevice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSetupTelemetries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CarTelemetryId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserSetupId = table.Column<int>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    OverrideTicks = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSetupTelemetries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSetupTelemetries_CarTelemetry_CarTelemetryId",
                        column: x => x.CarTelemetryId,
                        principalTable: "CarTelemetry",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserSetupTelemetries_UserSetups_UserSetupId",
                        column: x => x.UserSetupId,
                        principalTable: "UserSetups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSetupFlowNodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserSetupId = table.Column<int>(type: "INTEGER", nullable: false),
                    PositionX = table.Column<float>(type: "REAL", nullable: false),
                    PositionY = table.Column<float>(type: "REAL", nullable: false),
                    NodeType = table.Column<string>(type: "TEXT", maxLength: 34, nullable: false),
                    CarChannelId = table.Column<int>(type: "INTEGER", nullable: true),
                    SetupFunctionName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    TelemetryId = table.Column<int>(type: "INTEGER", nullable: true),
                    UserChannelId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSetupFlowNodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSetupFlowNodes_CarChannels_CarChannelId",
                        column: x => x.CarChannelId,
                        principalTable: "CarChannels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserSetupFlowNodes_CarTelemetry_TelemetryId",
                        column: x => x.TelemetryId,
                        principalTable: "CarTelemetry",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserSetupFlowNodes_UserChannel_UserChannelId",
                        column: x => x.UserChannelId,
                        principalTable: "UserChannel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserSetupFlowNodes_UserSetups_UserSetupId",
                        column: x => x.UserSetupId,
                        principalTable: "UserSetups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSetupFunctionNodeParameter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ParameterName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ParameterValue = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    NodeId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSetupFunctionNodeParameter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSetupFunctionNodeParameter_UserSetupFlowNodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "UserSetupFlowNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSetupLink",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserSetupFromNodeId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserSetupToNodeId = table.Column<int>(type: "INTEGER", nullable: false),
                    SourcePort = table.Column<string>(type: "TEXT", nullable: true),
                    TargetPort = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSetupLink", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSetupLink_UserSetupFlowNodes_UserSetupFromNodeId",
                        column: x => x.UserSetupFromNodeId,
                        principalTable: "UserSetupFlowNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserSetupLink_UserSetupFlowNodes_UserSetupToNodeId",
                        column: x => x.UserSetupToNodeId,
                        principalTable: "UserSetupFlowNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CarChannels_CarId",
                table: "CarChannels",
                column: "CarId");

            migrationBuilder.CreateIndex(
                name: "IX_CarTelemetry_CarId",
                table: "CarTelemetry",
                column: "CarId");

            migrationBuilder.CreateIndex(
                name: "IX_UserChannel_UserChannelDeviceId_IsAxis_ChannelId",
                table: "UserChannel",
                columns: new[] { "UserChannelDeviceId", "IsAxis", "ChannelId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserChannelDevice_UserId_DeviceName",
                table: "UserChannelDevice",
                columns: new[] { "UserId", "DeviceName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_ActiveVehicleId",
                table: "Users",
                column: "ActiveVehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_SessionToken",
                table: "Users",
                column: "SessionToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSetupFlowNodes_CarChannelId",
                table: "UserSetupFlowNodes",
                column: "CarChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSetupFlowNodes_TelemetryId",
                table: "UserSetupFlowNodes",
                column: "TelemetryId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSetupFlowNodes_UserChannelId",
                table: "UserSetupFlowNodes",
                column: "UserChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSetupFlowNodes_UserSetupId",
                table: "UserSetupFlowNodes",
                column: "UserSetupId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSetupFunctionNodeParameter_NodeId_ParameterName",
                table: "UserSetupFunctionNodeParameter",
                columns: new[] { "NodeId", "ParameterName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSetupLink_UserSetupFromNodeId",
                table: "UserSetupLink",
                column: "UserSetupFromNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSetupLink_UserSetupToNodeId",
                table: "UserSetupLink",
                column: "UserSetupToNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSetups_CarId",
                table: "UserSetups",
                column: "CarId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSetups_UserId",
                table: "UserSetups",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSetupTelemetries_CarTelemetryId",
                table: "UserSetupTelemetries",
                column: "CarTelemetryId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSetupTelemetries_UserSetupId",
                table: "UserSetupTelemetries",
                column: "UserSetupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SetupFilterTypes");

            migrationBuilder.DropTable(
                name: "UserSetupFunctionNodeParameter");

            migrationBuilder.DropTable(
                name: "UserSetupLink");

            migrationBuilder.DropTable(
                name: "UserSetupTelemetries");

            migrationBuilder.DropTable(
                name: "UserSetupFlowNodes");

            migrationBuilder.DropTable(
                name: "CarChannels");

            migrationBuilder.DropTable(
                name: "CarTelemetry");

            migrationBuilder.DropTable(
                name: "UserChannel");

            migrationBuilder.DropTable(
                name: "UserSetups");

            migrationBuilder.DropTable(
                name: "UserChannelDevice");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Cars");
        }
    }
}
