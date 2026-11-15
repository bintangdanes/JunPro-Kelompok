using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Trashury.Repositories;
using Trashury.Services;
using Xunit;

namespace Trashury.Tests;

public class KlasifikasiOnnxTests
{
    [Fact]
    public void Seluruh_label_model_terpetakan_ke_kategori_basis_data()
    {
        using var basisData = new BasisDataSementara();
        using var db = basisData.BukaSesi();
        var repo = new KategoriSampahRepository(db);

        string labelsPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Models", "labels.txt");
        Assert.True(File.Exists(labelsPath));

        string[] labels = File.ReadAllLines(labelsPath)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrEmpty(l))
            .ToArray();

        Assert.NotEmpty(labels);
        foreach (string label in labels)
        {
            string kategori = KlasifikasiOnnx.PetakanKategori(label);
            Assert.NotNull(repo.GetByNama(kategori));
        }
    }

    [Fact]
    public void Konstruktor_menolak_path_model_yang_tidak_ada()
    {
        Assert.Throws<FileNotFoundException>(() => new KlasifikasiOnnx("tidak-ada.onnx"));
    }

    [Fact]
    public void Klasifikasi_menolak_input_null_atau_kosong()
    {
        using var layanan = new KlasifikasiOnnx();

        Assert.Throws<ArgumentNullException>(() => layanan.Klasifikasi(null!));
        Assert.Throws<ArgumentException>(() => layanan.Klasifikasi([]));
    }

    [Fact]
    public void Klasifikasi_gambar_valid_mengembalikan_kategori_dan_confidence()
    {
        using var layanan = new KlasifikasiOnnx();
        using var gambar = new Image<Rgb24>(64, 64, new Rgb24(120, 180, 90));
        using var ms = new MemoryStream();
        gambar.SaveAsPng(ms);

        var hasil = layanan.Klasifikasi(ms.ToArray());

        Assert.False(string.IsNullOrWhiteSpace(hasil.KategoriTerdeteksi));
        Assert.InRange(hasil.Confidence, 0f, 1f);
    }
}
