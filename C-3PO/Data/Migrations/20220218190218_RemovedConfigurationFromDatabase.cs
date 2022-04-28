using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace C_3PO.Data.Migrations
{
    public partial class RemovedConfigurationFromDatabase : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Configurations");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Configurations",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Civilian = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Conduct = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Ejected = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Hangar = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    LoadingBay = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Lockdown = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Logs = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Onboarding = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    OuterRim = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Rules = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Unidentified = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configurations", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
