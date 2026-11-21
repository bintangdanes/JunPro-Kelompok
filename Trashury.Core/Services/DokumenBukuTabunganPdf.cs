using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Trashury.Models;

namespace Trashury.Services;

/// <summary>
/// Tata letak cetak buku tabungan satu nasabah: tabel tanggal, keterangan,
/// kredit, debit, dan saldo berjalan.
/// </summary>
internal static class DokumenBukuTabunganPdf
{
    private static readonly CultureInfo Id = CultureInfo.GetCultureInfo("id-ID");

    static DokumenBukuTabunganPdf()
    {
        // Lisensi Community: gratis untuk proyek akademik dan organisasi kecil.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Buat(BukuTabungan buku, DateTime dibuatPada)
    {
        ArgumentNullException.ThrowIfNull(buku);

        return Document.Create(dokumen => dokumen.Page(halaman =>
        {
            halaman.Size(PageSizes.A4);
            halaman.Margin(2, Unit.Centimetre);
            halaman.DefaultTextStyle(gaya => gaya.FontSize(10));

            halaman.Header().Column(kolom =>
            {
                kolom.Item().Text("TRASHURY — Buku Tabungan Bank Sampah").FontSize(16).Bold();
                kolom.Item().Text($"Nasabah: {buku.NamaNasabah} (No. {buku.NasabahId})").FontSize(12);
                kolom.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Medium);
            });

            halaman.Content().PaddingTop(12).Column(kolom =>
            {
                kolom.Spacing(10);

                if (buku.Baris.Count == 0)
                {
                    kolom.Item().Text("Belum ada transaksi.").Italic();
                    return;
                }

                kolom.Item().Table(tabel =>
                {
                    tabel.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(2);   // Tanggal
                        c.RelativeColumn(4);   // Keterangan
                        c.RelativeColumn(2);   // Kredit
                        c.RelativeColumn(2);   // Debit
                        c.RelativeColumn(2);   // Saldo
                    });

                    tabel.Header(kepala =>
                    {
                        Kepala(kepala.Cell(), "Tanggal", false);
                        Kepala(kepala.Cell(), "Keterangan", false);
                        Kepala(kepala.Cell(), "Masuk", true);
                        Kepala(kepala.Cell(), "Keluar", true);
                        Kepala(kepala.Cell(), "Saldo", true);
                    });

                    foreach (var b in buku.Baris)
                    {
                        Isi(tabel.Cell(), b.Tanggal.ToString("dd/MM/yyyy", Id), false);
                        Isi(tabel.Cell(), b.Keterangan, false);
                        Isi(tabel.Cell(), b.Kredit > 0 ? Rupiah(b.Kredit) : "", true);
                        Isi(tabel.Cell(), b.Debit > 0 ? Rupiah(b.Debit) : "", true);
                        Isi(tabel.Cell(), Rupiah(b.Saldo), true);
                    }

                    Total(tabel.Cell().ColumnSpan(2), "TOTAL", false);
                    Total(tabel.Cell(), Rupiah(buku.TotalKredit), true);
                    Total(tabel.Cell(), Rupiah(buku.TotalDebit), true);
                    Total(tabel.Cell(), Rupiah(buku.SaldoAkhir), true);
                });
            });

            halaman.Footer().Row(baris =>
            {
                baris.RelativeItem()
                    .Text($"Dicetak {dibuatPada.ToString("d MMMM yyyy HH.mm", Id)}")
                    .FontSize(8).FontColor(Colors.Grey.Darken1);

                baris.RelativeItem().AlignRight().Text(teks =>
                {
                    teks.DefaultTextStyle(gaya => gaya.FontSize(8).FontColor(Colors.Grey.Darken1));
                    teks.Span("Halaman ");
                    teks.CurrentPageNumber();
                    teks.Span(" dari ");
                    teks.TotalPages();
                });
            });
        })).GeneratePdf();
    }

    private static void Kepala(IContainer sel, string teks, bool kanan) =>
        Rata(sel.Background(Colors.Grey.Lighten3).Padding(4), kanan).Text(teks).Bold();

    private static void Isi(IContainer sel, string teks, bool kanan) =>
        Rata(sel.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4), kanan).Text(teks);

    private static void Total(IContainer sel, string teks, bool kanan) =>
        Rata(sel.BorderTop(1).Padding(4), kanan).Text(teks).Bold();

    private static IContainer Rata(IContainer sel, bool kanan) => kanan ? sel.AlignRight() : sel;

    private static string Rupiah(decimal nilai) => "Rp " + nilai.ToString("N0", Id);
}
