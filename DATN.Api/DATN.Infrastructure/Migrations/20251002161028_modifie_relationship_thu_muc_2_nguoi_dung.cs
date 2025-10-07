using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DATN.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class modifie_relationship_thu_muc_2_nguoi_dung : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.AddColumn<Guid>(
            //    name: "nguoi_dung_id",
            //    table: "thu_muc",
            //    type: "char(36)",
            //    nullable: false,
            //    defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
            //    collation: "ascii_general_ci");

            //migrationBuilder.CreateIndex(
            //    name: "IX_thu_muc_nguoi_dung_id",
            //    table: "thu_muc",
            //    column: "nguoi_dung_id");

            //migrationBuilder.AddForeignKey(
            //    name: "FK_thu_muc_nguoi_dung_nguoi_dung_id",
            //    table: "thu_muc",
            //    column: "nguoi_dung_id",
            //    principalTable: "nguoi_dung",
            //    principalColumn: "Id",
            //    onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropForeignKey(
            //    name: "FK_thu_muc_nguoi_dung_nguoi_dung_id",
            //    table: "thu_muc");

            //migrationBuilder.DropIndex(
            //    name: "IX_thu_muc_nguoi_dung_id",
            //    table: "thu_muc");

            //migrationBuilder.DropColumn(
            //    name: "nguoi_dung_id",
            //    table: "thu_muc");
        }
    }
}
