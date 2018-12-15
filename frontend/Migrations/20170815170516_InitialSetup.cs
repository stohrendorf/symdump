using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Migrations;

namespace frontend.Migrations
{
    [UsedImplicitly]
    public partial class InitialSetup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                "BinaryFile",
                table => new
                {
                    Id = table.Column<int>()
                        .Annotation("Sqlite:Autoincrement", true),
                    Data = table.Column<byte[]>(),
                    Name = table.Column<string>()
                },
                constraints: table => { table.PrimaryKey("PK_BinaryFile", x => x.Id); });

            migrationBuilder.CreateTable(
                "Projects",
                table => new
                {
                    Id = table.Column<int>()
                        .Annotation("Sqlite:Autoincrement", true),
                    ExeId = table.Column<int>(nullable: true),
                    SymId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.ForeignKey(
                        "FK_Projects_BinaryFile_ExeId",
                        x => x.ExeId,
                        "BinaryFile",
                        "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        "FK_Projects_BinaryFile_SymId",
                        x => x.SymId,
                        "BinaryFile",
                        "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                "IX_Projects_ExeId",
                "Projects",
                "ExeId");

            migrationBuilder.CreateIndex(
                "IX_Projects_SymId",
                "Projects",
                "SymId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                "Projects");

            migrationBuilder.DropTable(
                "BinaryFile");
        }
    }
}