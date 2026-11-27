using Trashury.Models;
using Trashury.Repositories;
using Trashury.Services;
using Xunit;

namespace Trashury.Tests;

public class AutentikasiDanBatalTransaksiTests
{
    [Fact]
    public void Akun_bawaan_dapat_login_dengan_peran_rbac_yang_benar()
    {
        using var basisData = new BasisDataSementara();
        using var db = basisData.BukaSesi();
        var auth = new LayananAutentikasi(db);
        auth.PastikanAkunBawaan();

        var pengurus = auth.Login("pengurus", "pengurus123");
        var op = auth.Login("operator", "operator123");
        var gagal = auth.Login("pengurus", "salah");

        Assert.NotNull(pengurus);
        Assert.True(pengurus.IsPengurus);
        Assert.Equal(Pengguna.PeranPengurus, pengurus.Peran);

        Assert.NotNull(op);
        Assert.False(op.IsPengurus);
        Assert.Equal(Pengguna.PeranOperator, op.Peran);

        Assert.Null(gagal);
    }

    [Fact]
    public void Tambah_pengguna_baru_menyimpan_hash_bukan_plaintext()
    {
        using var basisData = new BasisDataSementara();
        using var db = basisData.BukaSesi();
        var auth = new LayananAutentikasi(db);

        var baru = auth.TambahPengguna("siti", "Siti Operator", "rahasia123", Pengguna.PeranOperator);

        Assert.NotEqual("rahasia123", baru.PasswordHash);
        Assert.NotNull(auth.Login("siti", "rahasia123"));
        Assert.Null(auth.Login("siti", "bukanrahasia"));

        // Ubah nama & reset password
        auth.UbahPengguna(baru.Id, "Siti Senior", Pengguna.PeranOperator, "baru12345");
        Assert.Null(auth.Login("siti", "rahasia123"));
        var loginBaru = auth.Login("siti", "baru12345");
        Assert.NotNull(loginBaru);
        Assert.Equal("Siti Senior", loginBaru.NamaLengkap);

        // Tidak boleh hapus diri sendiri; boleh hapus oleh akun lain
        Assert.Throws<InvalidOperationException>(() => auth.HapusPengguna(baru.Id, idPenggunaAktif: baru.Id));
        auth.HapusPengguna(baru.Id);
        Assert.Null(auth.Login("siti", "baru12345"));
    }

    [Fact]
    public void Batalkan_setoran_dan_penarikan_mengembalikan_saldo_nasabah()
    {
        using var basisData = new BasisDataSementara();
        using var db = basisData.BukaSesi();

        var nasabahRepo = new NasabahRepository(db);
        var kategoriRepo = new KategoriSampahRepository(db);
        var transaksiRepo = new TransaksiRepository(db);
        var layanan = new LayananTransaksi(nasabahRepo, transaksiRepo);

        var nasabah = new Nasabah("Budi");
        nasabahRepo.Tambah(nasabah);
        nasabahRepo.SimpanPerubahan();

        var pet = kategoriRepo.GetByNama("Plastik PET"); // Rp 4.000/kg
        var setoran = layanan.CatatSetoran(nasabah.Id, [new DetailSetoran(pet, 3m)]); // +12.000
        var penarikan = layanan.ProsesPenarikan(nasabah.Id, 5000m); // -5.000 -> saldo 7.000
        Assert.Equal(7000m, nasabahRepo.GetById(nasabah.Id).Saldo);

        // Batalkan penarikan -> saldo kembali 12.000
        layanan.BatalkanTransaksi(penarikan.Id);
        Assert.Equal(12000m, nasabahRepo.GetById(nasabah.Id).Saldo);

        // Batalkan setoran -> saldo kembali 0
        layanan.BatalkanTransaksi(setoran.Id);
        Assert.Equal(0m, nasabahRepo.GetById(nasabah.Id).Saldo);
        Assert.Empty(transaksiRepo.GetAll());
    }
}
