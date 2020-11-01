using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WikipediaReferences.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Sources",
                columns: table => new
                {
                    Code = table.Column<string>(maxLength: 35, nullable: false),
                    Name = table.Column<string>(maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sources", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "References",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<string>(maxLength: 35, nullable: false),
                    SourceCode = table.Column<string>(maxLength: 35, nullable: false),
                    ArticleTitle = table.Column<string>(maxLength: 255, nullable: false),
                    LastNameSubject = table.Column<string>(nullable: true),
                    Author1 = table.Column<string>(nullable: true),
                    Authorlink1 = table.Column<string>(nullable: true),
                    Title = table.Column<string>(nullable: true),
                    Url = table.Column<string>(nullable: true),
                    UrlAccess = table.Column<string>(nullable: true),
                    Work = table.Column<string>(nullable: true),
                    AccessDate = table.Column<DateTime>(nullable: false),
                    Date = table.Column<DateTime>(nullable: false),
                    Page = table.Column<string>(nullable: true),
                    DeathDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_References", x => x.Id);
                    table.ForeignKey(
                        name: "FK_References_Sources_SourceCode",
                        column: x => x.SourceCode,
                        principalTable: "Sources",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_References_SourceCode",
                table: "References",
                column: "SourceCode");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "References");

            migrationBuilder.DropTable(
                name: "Sources");
        }
    }
}
