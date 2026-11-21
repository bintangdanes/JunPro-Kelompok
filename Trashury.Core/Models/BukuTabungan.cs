namespace Trashury.Models;

/// <summary>
/// Satu baris buku tabungan. Tepat salah satu dari <see cref="Kredit"/>
/// (setoran masuk) atau <see cref="Debit"/> (penarikan keluar) bernilai
/// lebih dari nol; <see cref="Saldo"/> adalah saldo setelah transaksi ini.
/// </summary>
public sealed record BarisBukuTabungan(
    int TransaksiId,
    DateTime Tanggal,
    string Keterangan,
    decimal Kredit,
    decimal Debit,
    decimal Saldo);

/// <summary>
/// Riwayat transaksi satu nasabah lengkap dengan saldo berjalan, urut dari
/// transaksi terlama. Dipakai untuk tampilan dan cetak buku tabungan.
/// </summary>
public sealed record BukuTabungan(
    int NasabahId,
    string NamaNasabah,
    decimal TotalKredit,
    decimal TotalDebit,
    decimal SaldoAkhir,
    IReadOnlyList<BarisBukuTabungan> Baris);
