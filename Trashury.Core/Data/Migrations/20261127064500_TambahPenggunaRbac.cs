using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trashury.Data.Migrations
{
    /// <inheritdoc />
    public partial class TambahPenggunaRbac : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pengguna",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    NamaLengkap = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Peran = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pengguna", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pengguna_Username",
                table: "Pengguna",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Pengguna");
        }
    }
}
