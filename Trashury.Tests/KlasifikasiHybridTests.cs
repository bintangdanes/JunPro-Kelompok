using Trashury.Interfaces;
using Trashury.Models;
using Trashury.Services;
using Xunit;

namespace Trashury.Tests;

public class KlasifikasiHybridTests
{
    private sealed class StubKlasifikasi : IKlasifikasiSampah
    {
        private readonly Func<byte[], HasilKlasifikasi> _fn;
        public int JumlahPanggilan { get; private set; }

        public StubKlasifikasi(Func<byte[], HasilKlasifikasi> fn) => _fn = fn;

        public HasilKlasifikasi Klasifikasi(byte[] gambar)
        {
            JumlahPanggilan++;
            return _fn(gambar);
        }
    }

    [Fact]
    public void Menggunakan_hasil_cloud_ketika_cloud_berhasil()
    {
        var cloud = new StubKlasifikasi(_ => new HasilKlasifikasi("Plastik PET", 0.96f)
        {
            SumberModel = "Cloud SigLIP-SO400M"
        });
        var edge = new StubKlasifikasi(_ => new HasilKlasifikasi("Kaca", 0.50f));

        using var hybrid = new KlasifikasiHybrid(cloud, edge, gunakanCloud: true);
        var hasil = hybrid.Klasifikasi([1, 2, 3]);

        Assert.Equal("Plastik PET", hasil.KategoriTerdeteksi);
        Assert.Equal("Cloud SigLIP-SO400M", hasil.SumberModel);
        Assert.Equal(1, cloud.JumlahPanggilan);
        Assert.Equal(0, edge.JumlahPanggilan);
    }

    [Fact]
    public void Otomatis_fallback_ke_edge_ketika_cloud_gagal_atau_offline()
    {
        var cloud = new StubKlasifikasi(_ => throw new HttpRequestException("Offline"));
        var edge = new StubKlasifikasi(_ => new HasilKlasifikasi("Kaleng Aluminium", 0.85f));

        using var hybrid = new KlasifikasiHybrid(cloud, edge, gunakanCloud: true);
        var hasil = hybrid.Klasifikasi([1, 2, 3]);

        Assert.Equal("Kaleng Aluminium", hasil.KategoriTerdeteksi);
        Assert.Equal("Edge ONNX (Offline Fallback)", hasil.SumberModel);
        Assert.Equal(1, cloud.JumlahPanggilan);
        Assert.Equal(1, edge.JumlahPanggilan);
    }

    [Fact]
    public void Langsung_memakai_edge_ketika_mode_cloud_dimatikan()
    {
        var cloud = new StubKlasifikasi(_ => new HasilKlasifikasi("Plastik PET", 0.99f));
        var edge = new StubKlasifikasi(_ => new HasilKlasifikasi("Kertas & Kardus", 0.88f));

        using var hybrid = new KlasifikasiHybrid(cloud, edge, gunakanCloud: false);
        var hasil = hybrid.Klasifikasi([1, 2, 3]);

        Assert.Equal("Kertas & Kardus", hasil.KategoriTerdeteksi);
        Assert.Equal("Edge ONNX (EfficientNet-B2)", hasil.SumberModel);
        Assert.Equal(0, cloud.JumlahPanggilan);
        Assert.Equal(1, edge.JumlahPanggilan);
    }
}
