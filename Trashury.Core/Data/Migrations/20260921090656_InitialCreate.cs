using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Trashury.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KategoriSampah",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nama = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    HargaPerKg = table.Column<decimal>(type: "TEXT", nullable: false),
                    FaktorEmisiCO2e = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KategoriSampah", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Nasabah",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nama = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Saldo = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nasabah", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Transaksi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NasabahId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tanggal = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Nominal = table.Column<decimal>(type: "TEXT", nullable: false),
                    JenisTransaksi = table.Column<string>(type: "TEXT", maxLength: 13, nullable: false),
                    JumlahTarik = table.Column<decimal>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transaksi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transaksi_Nasabah_NasabahId",
                        column: x => x.NasabahId,
                        principalTable: "Nasabah",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DetailSetoran",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SetoranSampahId = table.Column<int>(type: "INTEGER", nullable: false),
                    KategoriId = table.Column<int>(type: "INTEGER", nullable: false),
                    BeratKg = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetailSetoran", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetailSetoran_KategoriSampah_KategoriId",
                        column: x => x.KategoriId,
                        principalTable: "KategoriSampah",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DetailSetoran_Transaksi_SetoranSampahId",
                        column: x => x.SetoranSampahId,
                        principalTable: "Transaksi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "KategoriSampah",
                columns: new[] { "Id", "FaktorEmisiCO2e", "HargaPerKg", "Nama" },
                values: new object[,]
                {
                    { 1, 1.5m, 4000m, "Plastik PET" },
                    { 2, 0.9m, 2000m, "Kertas & Kardus" },
                    { 3, 8.1m, 15000m, "Kaleng Aluminium" },
                    { 4, 0.3m, 500m, "Kaca" },
                    { 5, 0.25m, 300m, "Organik" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_DetailSetoran_KategoriId",
                table: "DetailSetoran",
                column: "KategoriId");

            migrationBuilder.CreateIndex(
                name: "IX_DetailSetoran_SetoranSampahId",
                table: "DetailSetoran",
                column: "SetoranSampahId");

            migrationBuilder.CreateIndex(
                name: "IX_KategoriSampah_Nama",
                table: "KategoriSampah",
                column: "Nama",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transaksi_NasabahId",
                table: "Transaksi",
                column: "NasabahId");

            migrationBuilder.CreateIndex(
                name: "IX_Transaksi_Tanggal",
                table: "Transaksi",
                column: "Tanggal");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetailSetoran");

            migrationBuilder.DropTable(
                name: "KategoriSampah");

            migrationBuilder.DropTable(
                name: "Transaksi");

            migrationBuilder.DropTable(
                name: "Nasabah");
        }
    }
}
