using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace C_3PO.Data.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Onboardings",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Channel = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    ActionMessage = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    OfferMessage = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    RulesMessage = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    CategoriesMessage = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    NotificationsMessage = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Onboardings", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Onboardings");
        }
    }
}
