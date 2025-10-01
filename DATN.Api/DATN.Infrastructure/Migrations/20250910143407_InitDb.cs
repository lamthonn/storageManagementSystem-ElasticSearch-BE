using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DATN.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.CreateTable(
            //    name: "dieu_huong_2_command",
            //    columns: table => new
            //    {
            //        Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        dieu_huong_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        command = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        command_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_dieu_huong_2_command", x => x.Id);
            //        table.ForeignKey(
            //            name: "FK_dieu_huong_2_command_dieu_huong_dieu_huong_id",
            //            column: x => x.dieu_huong_id,
            //            principalTable: "dieu_huong",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //        table.ForeignKey(
            //            name: "FK_dieu_huong_2_command_dm_command_command_id",
            //            column: x => x.command_id,
            //            principalTable: "dm_command",
            //            principalColumn: "id");
            //    })
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.CreateTable(
            //    name: "nguoi_dung_2_danh_muc",
            //    columns: table => new
            //    {
            //        Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        nguoi_dung_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        danh_muc_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_nguoi_dung_2_danh_muc", x => x.Id);
            //        table.ForeignKey(
            //            name: "FK_nguoi_dung_2_danh_muc_danh_muc_danh_muc_id",
            //            column: x => x.danh_muc_id,
            //            principalTable: "danh_muc",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //        table.ForeignKey(
            //            name: "FK_nguoi_dung_2_danh_muc_nguoi_dung_nguoi_dung_id",
            //            column: x => x.nguoi_dung_id,
            //            principalTable: "nguoi_dung",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //    })
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.CreateTable(
            //    name: "nguoi_dung_2_nhom_nguoi_dung",
            //    columns: table => new
            //    {
            //        id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        nguoi_dung_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        nhom_nguoi_dung_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        mac_dinh = table.Column<bool>(type: "tinyint(1)", nullable: false)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_nguoi_dung_2_nhom_nguoi_dung", x => x.id);
            //        table.ForeignKey(
            //            name: "FK_nguoi_dung_2_nhom_nguoi_dung_nguoi_dung_nguoi_dung_id",
            //            column: x => x.nguoi_dung_id,
            //            principalTable: "nguoi_dung",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //        table.ForeignKey(
            //            name: "FK_nguoi_dung_2_nhom_nguoi_dung_nhom_nguoi_dung_nhom_nguoi_dung~",
            //            column: x => x.nhom_nguoi_dung_id,
            //            principalTable: "nhom_nguoi_dung",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //    })
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.CreateTable(
            //    name: "nhom_nguoi_dung_2_command",
            //    columns: table => new
            //    {
            //        id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        dieu_huong_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        nhom_nguoi_dung_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        command = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        command_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_nhom_nguoi_dung_2_command", x => x.id);
            //        table.ForeignKey(
            //            name: "FK_nhom_nguoi_dung_2_command_dieu_huong_dieu_huong_id",
            //            column: x => x.dieu_huong_id,
            //            principalTable: "dieu_huong",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //        table.ForeignKey(
            //            name: "FK_nhom_nguoi_dung_2_command_nhom_nguoi_dung_nhom_nguoi_dung_id",
            //            column: x => x.nhom_nguoi_dung_id,
            //            principalTable: "nhom_nguoi_dung",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //    })
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.CreateTable(
            //    name: "nhom_nguoi_dung_2_dieu_huong",
            //    columns: table => new
            //    {
            //        id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        dieu_huong_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        nhom_nguoi_dung_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_nhom_nguoi_dung_2_dieu_huong", x => x.id);
            //        table.ForeignKey(
            //            name: "FK_nhom_nguoi_dung_2_dieu_huong_dieu_huong_dieu_huong_id",
            //            column: x => x.dieu_huong_id,
            //            principalTable: "dieu_huong",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //        table.ForeignKey(
            //            name: "FK_nhom_nguoi_dung_2_dieu_huong_nhom_nguoi_dung_nhom_nguoi_dung~",
            //            column: x => x.nhom_nguoi_dung_id,
            //            principalTable: "nhom_nguoi_dung",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //    })
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.CreateTable(
            //    name: "tai_lieu_2_nguoi_dung",
            //    columns: table => new
            //    {
            //        Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        tai_lieu_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        nguoi_dung_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_tai_lieu_2_nguoi_dung", x => x.Id);
            //        table.ForeignKey(
            //            name: "FK_tai_lieu_2_nguoi_dung_nguoi_dung_nguoi_dung_id",
            //            column: x => x.nguoi_dung_id,
            //            principalTable: "nguoi_dung",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //        table.ForeignKey(
            //            name: "FK_tai_lieu_2_nguoi_dung_tai_lieu_tai_lieu_id",
            //            column: x => x.tai_lieu_id,
            //            principalTable: "tai_lieu",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //    })
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.CreateIndex(
            //    name: "IX_dieu_huong_2_command_command_id",
            //    table: "dieu_huong_2_command",
            //    column: "command_id");

            //migrationBuilder.CreateIndex(
            //    name: "IX_dieu_huong_2_command_dieu_huong_id",
            //    table: "dieu_huong_2_command",
            //    column: "dieu_huong_id");

            //migrationBuilder.CreateIndex(
            //    name: "IX_nguoi_dung_2_danh_muc_danh_muc_id",
            //    table: "nguoi_dung_2_danh_muc",
            //    column: "danh_muc_id");

            //migrationBuilder.CreateIndex(
            //    name: "IX_nguoi_dung_2_danh_muc_nguoi_dung_id",
            //    table: "nguoi_dung_2_danh_muc",
            //    column: "nguoi_dung_id");

            //migrationBuilder.CreateIndex(
            //    name: "IX_nguoi_dung_2_nhom_nguoi_dung_nguoi_dung_id",
            //    table: "nguoi_dung_2_nhom_nguoi_dung",
            //    column: "nguoi_dung_id");

            //migrationBuilder.CreateIndex(
            //    name: "IX_nguoi_dung_2_nhom_nguoi_dung_nhom_nguoi_dung_id",
            //    table: "nguoi_dung_2_nhom_nguoi_dung",
            //    column: "nhom_nguoi_dung_id");

            //migrationBuilder.CreateIndex(
            //    name: "IX_nhat_ky_he_thong_dieu_huong_id",
            //    table: "nhat_ky_he_thong",
            //    column: "dieu_huong_id");

            //migrationBuilder.CreateIndex(
            //    name: "IX_nhom_nguoi_dung_2_command_dieu_huong_id",
            //    table: "nhom_nguoi_dung_2_command",
            //    column: "dieu_huong_id");

            //migrationBuilder.CreateIndex(
            //    name: "IX_nhom_nguoi_dung_2_command_nhom_nguoi_dung_id",
            //    table: "nhom_nguoi_dung_2_command",
            //    column: "nhom_nguoi_dung_id");

            //migrationBuilder.CreateIndex(
            //    name: "IX_nhom_nguoi_dung_2_dieu_huong_dieu_huong_id",
            //    table: "nhom_nguoi_dung_2_dieu_huong",
            //    column: "dieu_huong_id");

            //migrationBuilder.CreateIndex(
            //    name: "IX_nhom_nguoi_dung_2_dieu_huong_nhom_nguoi_dung_id",
            //    table: "nhom_nguoi_dung_2_dieu_huong",
            //    column: "nhom_nguoi_dung_id");

            //migrationBuilder.CreateIndex(
            //    name: "IX_tai_lieu_2_nguoi_dung_nguoi_dung_id",
            //    table: "tai_lieu_2_nguoi_dung",
            //    column: "nguoi_dung_id");

            //migrationBuilder.CreateIndex(
            //    name: "IX_tai_lieu_2_nguoi_dung_tai_lieu_id",
            //    table: "tai_lieu_2_nguoi_dung",
            //    column: "tai_lieu_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropTable(
            //    name: "dieu_huong_2_command");

            //migrationBuilder.DropTable(
            //    name: "nguoi_dung_2_danh_muc");

            //migrationBuilder.DropTable(
            //    name: "nguoi_dung_2_nhom_nguoi_dung");

            //migrationBuilder.DropTable(
            //    name: "nhom_nguoi_dung_2_command");

            //migrationBuilder.DropTable(
            //    name: "nhom_nguoi_dung_2_dieu_huong");

            //migrationBuilder.DropTable(
            //    name: "tai_lieu_2_nguoi_dung");

        }
    }
}
