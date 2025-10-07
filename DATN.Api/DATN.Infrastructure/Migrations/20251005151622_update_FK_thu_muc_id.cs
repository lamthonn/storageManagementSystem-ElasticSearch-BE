using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DATN.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class update_FK_thu_muc_id : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tai_lieu_thu_muc_thu_mucid",
                table: "tai_lieu");

            migrationBuilder.DropIndex(
                name: "IX_tai_lieu_thu_mucid",
                table: "tai_lieu");

            migrationBuilder.DropColumn(
                name: "thu_mucid",
                table: "tai_lieu");

            migrationBuilder.CreateIndex(
                name: "IX_tai_lieu_thu_muc_id",
                table: "tai_lieu",
                column: "thu_muc_id");

            migrationBuilder.AddForeignKey(
                name: "FK_tai_lieu_thu_muc_thu_muc_id",
                table: "tai_lieu",
                column: "thu_muc_id",
                principalTable: "thu_muc",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tai_lieu_thu_muc_thu_muc_id",
                table: "tai_lieu");

            migrationBuilder.DropIndex(
                name: "IX_tai_lieu_thu_muc_id",
                table: "tai_lieu");

            migrationBuilder.AddColumn<Guid>(
                name: "thu_mucid",
                table: "tai_lieu",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

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
    }
}
