using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace C_3PO.Data.Migrations
{
    public partial class AddedConfiguration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Configurations",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OuterRim = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Onboarding = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Hangar = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Rules = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Ejected = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Civilian = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Logs = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Lockdown = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configurations", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Configurations");
        }
    }
}
