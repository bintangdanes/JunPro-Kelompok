using Microsoft.EntityFrameworkCore;
using Trashury.Data;
using Trashury.Repositories;
using Xunit;

namespace Trashury.Tests;

public class LokasiBasisDataTests
{
    [Fact]
    public void Basis_data_dapat_dibuat_pada_folder_yang_belum_ada()
    {
        var folder = Path.Combine(Path.GetTempPath(), "trashury-lokasi-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(folder, "sub", "trashury.db");

        try
        {
            using (var db = TrashuryDbContextFactory.Buat(path))
            {
                db.Database.Migrate();
            }

            Assert.True(File.Exists(path));

            using (var db = TrashuryDbContextFactory.Buat(path))
            {
                Assert.Equal(5, new KategoriSampahRepository(db).GetAll().Count());
            }
        }
        finally
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
    }

    [Fact]
    public void Lokasi_bawaan_berada_di_folder_data_aplikasi_pengguna()
    {
        var path = TrashuryDbContextFactory.LokasiBasisDataBawaan();

        Assert.EndsWith("trashury.db", path);
        Assert.Contains("Trashury", path);
        Assert.True(Directory.Exists(Path.GetDirectoryName(path)));
    }
}
