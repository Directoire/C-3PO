using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace C_3PO.Data.Migrations
{
    public partial class CategoryIdOptional : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NotificationRoles_Categories_CategoryId",
                table: "NotificationRoles");

            migrationBuilder.AlterColumn<ulong>(
                name: "CategoryId",
                table: "NotificationRoles",
                type: "bigint unsigned",
                nullable: true,
                oldClrType: typeof(ulong),
                oldType: "bigint unsigned");

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationRoles_Categories_CategoryId",
                table: "NotificationRoles",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NotificationRoles_Categories_CategoryId",
                table: "NotificationRoles");

            migrationBuilder.AlterColumn<ulong>(
                name: "CategoryId",
                table: "NotificationRoles",
                type: "bigint unsigned",
                nullable: false,
                defaultValue: 0ul,
                oldClrType: typeof(ulong),
                oldType: "bigint unsigned",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationRoles_Categories_CategoryId",
                table: "NotificationRoles",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
