using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DATN.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class modifie_Add_thu_muc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "thu_muc_id",
                table: "tai_lieu",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "thu_mucid",
                table: "tai_lieu",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.CreateTable(
                name: "thu_muc",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ten = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    thu_muc_cha_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    ngay_tao = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    nguoi_tao = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ngay_chinh_sua = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    nguoi_chinh_sua = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_thu_muc", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_tai_lieu_thu_mucid",
                table: "tai_lieu",
                column: "thu_mucid");

            migrationBuilder.AddForeignKey(
                name: "FK_tai_lieu_thu_muc_thu_mucid",
                table: "tai_lieu",
                column: "thu_mucid",
                principalTable: "thu_muc",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tai_lieu_thu_muc_thu_mucid",
                table: "tai_lieu");

            migrationBuilder.DropTable(
                name: "thu_muc");

            migrationBuilder.DropIndex(
                name: "IX_tai_lieu_thu_mucid",
                table: "tai_lieu");

            migrationBuilder.DropColumn(
                name: "thu_muc_id",
                table: "tai_lieu");

            migrationBuilder.DropColumn(
                name: "thu_mucid",
                table: "tai_lieu");
        }
    }
}
