using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace susalem.EasyDemo.Migrations
{
    public partial class AddAutoIncrementToLogId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CabinetInfo",
                columns: table => new
                {
                    CabinetId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChamName = table.Column<string>(type: "TEXT", nullable: false),
                    PNCode = table.Column<string>(type: "TEXT", nullable: true),
                    MachineId = table.Column<string>(type: "TEXT", nullable: true),
                    isTemperaturing = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsNull = table.Column<bool>(type: "INTEGER", nullable: false),
                    TemperatureStartTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TemperatureEndTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ExpirationDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LockAddress = table.Column<string>(type: "TEXT", nullable: false),
                    GreenLightAddress = table.Column<string>(type: "TEXT", nullable: false),
                    RedLightAddress = table.Column<string>(type: "TEXT", nullable: false),
                    DoorAddress = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CabinetInfo", x => x.CabinetId);
                });

            migrationBuilder.CreateTable(
                name: "ChemicalParas",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IsUse = table.Column<bool>(type: "INTEGER", nullable: false),
                    CabinetId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    PNCode = table.Column<string>(type: "TEXT", nullable: false),
                    SerialNum = table.Column<string>(type: "TEXT", nullable: false),
                    MachineId = table.Column<string>(type: "TEXT", nullable: false),
                    ReheatingTime = table.Column<double>(type: "REAL", nullable: false),
                    ExpirationDate = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChemicalParas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "hc_roles",
                columns: table => new
                {
                    role_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    role_name = table.Column<string>(type: "TEXT", nullable: false),
                    level = table.Column<int>(type: "INTEGER", nullable: false),
                    state = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hc_roles", x => x.role_id);
                });

            migrationBuilder.CreateTable(
                name: "hc_users",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    role_id = table.Column<int>(type: "INTEGER", nullable: false),
                    user_name = table.Column<string>(type: "TEXT", nullable: true),
                    password = table.Column<string>(type: "TEXT", nullable: true),
                    user_icon = table.Column<string>(type: "TEXT", nullable: true),
                    real_name = table.Column<string>(type: "TEXT", nullable: true),
                    state = table.Column<int>(type: "INTEGER", nullable: false),
                    card_id = table.Column<string>(type: "TEXT", nullable: true),
                    fingerprint_id = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hc_users", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "Historys",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CabinetId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    PNCode = table.Column<string>(type: "TEXT", nullable: true),
                    SerialNum = table.Column<string>(type: "TEXT", nullable: true),
                    MachineId = table.Column<string>(type: "TEXT", nullable: true),
                    OpenCabinetTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Operater = table.Column<string>(type: "TEXT", nullable: true),
                    Message = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Historys", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "hc_roles",
                columns: new[] { "role_id", "level", "role_name", "state" },
                values: new object[] { 1, 1, "Admin", 0 });

            migrationBuilder.InsertData(
                table: "hc_roles",
                columns: new[] { "role_id", "level", "role_name", "state" },
                values: new object[] { 2, 2, "User", 0 });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CabinetInfo");

            migrationBuilder.DropTable(
                name: "ChemicalParas");

            migrationBuilder.DropTable(
                name: "hc_roles");

            migrationBuilder.DropTable(
                name: "hc_users");

            migrationBuilder.DropTable(
                name: "Historys");
        }
    }
}
