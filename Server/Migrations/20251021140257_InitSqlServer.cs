using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LteCar.Server.Migrations
{
    /// <inheritdoc />
    public partial class InitSqlServer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cars",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CarIdentityKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ChannelMapHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    VideoStreamPort = table.Column<int>(type: "int", nullable: true),
                    LastSeen = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VideoWidth = table.Column<int>(type: "int", nullable: true),
                    VideoHeight = table.Column<int>(type: "int", nullable: true),
                    VideoFramerate = table.Column<int>(type: "int", nullable: true),
                    VideoBrightness = table.Column<float>(type: "real", nullable: true),
                    VideoBitrate = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cars", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SetupFilterTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SetupFilterTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CarChannels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisplayName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ChannelName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    RequiresAxis = table.Column<bool>(type: "bit", nullable: false),
                    CarId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarChannels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarChannels_Cars_CarId",
                        column: x => x.CarId,
                        principalTable: "Cars",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CarTelemetry",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChannelName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CarId = table.Column<int>(type: "int", nullable: false),
                    ReadIntervalTicks = table.Column<int>(type: "int", nullable: false),
                    TelemetryType = table.Column<string>(type: "nvarchar(max)", nullable: false)
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
                name: "CarVideoStreams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StreamId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CarId = table.Column<int>(type: "int", nullable: false),
                    Protocol = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Port = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ProcessArguments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StreamPurpose = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    LastStatusUpdate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarVideoStreams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarVideoStreams_Cars_CarId",
                        column: x => x.CarId,
                        principalTable: "Cars",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ActiveVehicleId = table.Column<int>(type: "int", nullable: true),
                    SessionToken = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    LastSeen = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TransferCode = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: true),
                    TransferCodeExpiresAt = table.Column<string>(type: "TEXT", nullable: true)
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
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserChannelDevice", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserChannelDevice_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserSetups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CarId = table.Column<int>(type: "int", nullable: false),
                    CarSecret = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSetups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSetups_Cars_CarId",
                        column: x => x.CarId,
                        principalTable: "Cars",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserSetups_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserChannel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserChannelDeviceId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ChannelId = table.Column<int>(type: "int", nullable: false),
                    IsAxis = table.Column<bool>(type: "bit", nullable: false),
                    Accuracy = table.Column<int>(type: "int", nullable: false),
                    CalibrationMin = table.Column<float>(type: "real", nullable: true),
                    CalibrationMax = table.Column<float>(type: "real", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserChannel", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserChannel_UserChannelDevice_UserChannelDeviceId",
                        column: x => x.UserChannelDeviceId,
                        principalTable: "UserChannelDevice",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserSetupTelemetries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CarTelemetryId = table.Column<int>(type: "int", nullable: false),
                    UserSetupId = table.Column<int>(type: "int", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    OverrideTicks = table.Column<int>(type: "int", nullable: true)
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
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserSetupId = table.Column<int>(type: "int", nullable: false),
                    PositionX = table.Column<float>(type: "real", nullable: false),
                    PositionY = table.Column<float>(type: "real", nullable: false),
                    NodeType = table.Column<string>(type: "nvarchar(34)", maxLength: 34, nullable: false),
                    CarChannelId = table.Column<int>(type: "int", nullable: true),
                    SetupFunctionName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TelemetryId = table.Column<int>(type: "int", nullable: true),
                    UserChannelId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSetupFlowNodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSetupFlowNodes_CarChannels_CarChannelId",
                        column: x => x.CarChannelId,
                        principalTable: "CarChannels",
                        principalColumn: "Id");
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
                        principalColumn: "Id");
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
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParameterName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ParameterValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NodeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSetupFunctionNodeParameter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSetupFunctionNodeParameter_UserSetupFlowNodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "UserSetupFlowNodes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserSetupLink",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserSetupFromNodeId = table.Column<int>(type: "int", nullable: false),
                    UserSetupToNodeId = table.Column<int>(type: "int", nullable: false),
                    SourcePort = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TargetPort = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSetupLink", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSetupLink_UserSetupFlowNodes_UserSetupFromNodeId",
                        column: x => x.UserSetupFromNodeId,
                        principalTable: "UserSetupFlowNodes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserSetupLink_UserSetupFlowNodes_UserSetupToNodeId",
                        column: x => x.UserSetupToNodeId,
                        principalTable: "UserSetupFlowNodes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CarChannels_CarId",
                table: "CarChannels",
                column: "CarId");

            migrationBuilder.CreateIndex(
                name: "IX_Cars_CarIdentityKey",
                table: "Cars",
                column: "CarIdentityKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CarTelemetry_CarId",
                table: "CarTelemetry",
                column: "CarId");

            migrationBuilder.CreateIndex(
                name: "IX_CarVideoStreams_CarId_IsActive",
                table: "CarVideoStreams",
                columns: new[] { "CarId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CarVideoStreams_CarId_StreamId",
                table: "CarVideoStreams",
                columns: new[] { "CarId", "StreamId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CarVideoStreams_Port",
                table: "CarVideoStreams",
                column: "Port");

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
                unique: true,
                filter: "[SessionToken] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_TransferCode",
                table: "Users",
                column: "TransferCode",
                unique: true,
                filter: "[TransferCode] IS NOT NULL");

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
                name: "CarVideoStreams");

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
