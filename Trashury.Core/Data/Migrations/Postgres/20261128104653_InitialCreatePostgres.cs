using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Trashury.Data.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class InitialCreatePostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KategoriSampah",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nama = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    HargaPerKg = table.Column<decimal>(type: "numeric", nullable: false),
                    FaktorEmisiCO2e = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KategoriSampah", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Nasabah",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nama = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Saldo = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nasabah", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Transaksi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NasabahId = table.Column<int>(type: "integer", nullable: false),
                    Tanggal = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Nominal = table.Column<decimal>(type: "numeric", nullable: false),
                    JenisTransaksi = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    JumlahTarik = table.Column<decimal>(type: "numeric", nullable: true)
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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SetoranSampahId = table.Column<int>(type: "integer", nullable: false),
                    KategoriId = table.Column<int>(type: "integer", nullable: false),
                    BeratKg = table.Column<decimal>(type: "numeric", nullable: false)
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

            // Seed menulis Id 1-5 secara eksplisit, sehingga sequence identity
            // PostgreSQL masih di 1. Tanpa ini, kategori baru pertama akan
            // bentrok dengan Id 1.
            migrationBuilder.Sql(
                """SELECT setval(pg_get_serial_sequence('"KategoriSampah"', 'Id'), (SELECT MAX("Id") FROM "KategoriSampah"));""");

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
