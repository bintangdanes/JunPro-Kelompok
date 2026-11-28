using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trashury.Data.Migrations.Postgres
{
    /// <summary>
    /// Supabase membuka seluruh tabel di schema public lewat REST API yang dapat
    /// diakses dengan kunci publik. Row Level Security tanpa policy menutup akses
    /// itu sepenuhnya. Aplikasi tetap berjalan karena terhubung sebagai pemilik
    /// tabel (role postgres), yang tidak terkena RLS.
    /// </summary>
    public partial class AktifkanRowLevelSecurity : Migration
    {
        private static readonly string[] Tabel =
            ["Nasabah", "KategoriSampah", "Transaksi", "DetailSetoran", "__EFMigrationsHistory"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var tabel in Tabel)
            {
                migrationBuilder.Sql($"""ALTER TABLE "{tabel}" ENABLE ROW LEVEL SECURITY;""");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var tabel in Tabel)
            {
                migrationBuilder.Sql($"""ALTER TABLE "{tabel}" DISABLE ROW LEVEL SECURITY;""");
            }
        }
    }
}
