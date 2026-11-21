using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Trashury.Models;

namespace Trashury.Services;

/// <summary>
/// Tata letak laporan bulanan dalam bentuk PDF untuk diserahkan ke DLH.
/// Angka ditampilkan dengan format Indonesia (Rp 19.500; 3,5 kg) karena
/// berkas ini dibaca manusia, berbeda dengan CSV yang dibaca mesin.
/// </summary>
internal static class DokumenLaporanPdf
{
    private static readonly CultureInfo Id = CultureInfo.GetCultureInfo("id-ID");

    static DokumenLaporanPdf()
    {
        // Lisensi Community: gratis untuk proyek akademik dan organisasi kecil.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Buat(RekapBulanan rekap, DateTime dibuatPada)
    {
        ArgumentNullException.ThrowIfNull(rekap);

        var namaBulan = Id.DateTimeFormat.GetMonthName(rekap.Bulan);
        var judulPeriode = $"{namaBulan} {rekap.Tahun}";

        return Document.Create(dokumen => dokumen.Page(halaman =>
        {
            halaman.Size(PageSizes.A4);
            halaman.Margin(2, Unit.Centimetre);
            halaman.DefaultTextStyle(gaya => gaya.FontSize(10));

            halaman.Header().Column(kolom =>
            {
                kolom.Item().Text("TRASHURY — Laporan Bulanan Bank Sampah").FontSize(16).Bold();
                kolom.Item().Text($"Periode: {judulPeriode}").FontSize(12);
                kolom.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Medium);
            });

            halaman.Content().PaddingTop(12).Column(kolom =>
            {
                kolom.Spacing(14);

                kolom.Item().Text("Ringkasan").FontSize(12).Bold();
                kolom.Item().Table(tabel =>
                {
                    tabel.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(2);
                        c.RelativeColumn(1);
                    });

                    BarisRingkasan(tabel, "Jumlah setoran", $"{rekap.JumlahSetoran.ToString("N0", Id)} transaksi");
                    BarisRingkasan(tabel, "Jumlah penarikan", $"{rekap.JumlahPenarikan.ToString("N0", Id)} transaksi");
                    BarisRingkasan(tabel, "Nasabah aktif", $"{rekap.JumlahNasabahAktif.ToString("N0", Id)} orang");
                    BarisRingkasan(tabel, "Total berat sampah", Kg(rekap.TotalBeratKg));
                    BarisRingkasan(tabel, "Total nilai setoran", Rupiah(rekap.TotalNilaiSetoran));
                    BarisRingkasan(tabel, "Total penarikan saldo", Rupiah(rekap.TotalPenarikan));
                    BarisRingkasan(tabel, "Estimasi CO2e yang dihindari", Kg(rekap.TotalCO2eKg));
                });

                kolom.Item().Text("Rincian per Kategori Sampah").FontSize(12).Bold();

                if (rekap.PerKategori.Count == 0)
                {
                    kolom.Item().Text("Tidak ada setoran sampah pada periode ini.").Italic();
                }
                else
                {
                    kolom.Item().Table(tabel => TabelKategori(tabel, rekap));
                }

                kolom.Item().PaddingTop(6).Text(
                    "Estimasi CO2e dihitung dari berat setoran dikali faktor emisi yang dihindari " +
                    "per kategori sampah.").FontSize(8).FontColor(Colors.Grey.Darken1);
            });

            halaman.Footer().Row(baris =>
            {
                baris.RelativeItem()
                    .Text($"Dibuat {dibuatPada.ToString("d MMMM yyyy HH.mm", Id)}")
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

    private static void BarisRingkasan(TableDescriptor tabel, string label, string nilai)
    {
        tabel.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).Text(label);
        tabel.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).AlignRight().Text(nilai);
    }

    private static void TabelKategori(TableDescriptor tabel, RekapBulanan rekap)
    {
        tabel.ColumnsDefinition(c =>
        {
            c.RelativeColumn(3);
            c.RelativeColumn(2);
            c.RelativeColumn(2);
            c.RelativeColumn(2);
        });

        tabel.Header(kepala =>
        {
            SelKepala(kepala.Cell(), "Kategori", rataKanan: false);
            SelKepala(kepala.Cell(), "Berat", rataKanan: true);
            SelKepala(kepala.Cell(), "Nilai", rataKanan: true);
            SelKepala(kepala.Cell(), "CO2e Dihindari", rataKanan: true);
        });

        foreach (var k in rekap.PerKategori)
        {
            SelIsi(tabel.Cell(), k.NamaKategori, rataKanan: false);
            SelIsi(tabel.Cell(), Kg(k.TotalBeratKg), rataKanan: true);
            SelIsi(tabel.Cell(), Rupiah(k.TotalNilai), rataKanan: true);
            SelIsi(tabel.Cell(), Kg(k.TotalCO2eKg), rataKanan: true);
        }

        SelTotal(tabel.Cell(), "TOTAL", rataKanan: false);
        SelTotal(tabel.Cell(), Kg(rekap.TotalBeratKg), rataKanan: true);
        SelTotal(tabel.Cell(), Rupiah(rekap.TotalNilaiSetoran), rataKanan: true);
        SelTotal(tabel.Cell(), Kg(rekap.TotalCO2eKg), rataKanan: true);
    }

    private static void SelKepala(IContainer sel, string teks, bool rataKanan) =>
        Rata(sel.Background(Colors.Grey.Lighten3).Padding(4), rataKanan).Text(teks).Bold();

    private static void SelIsi(IContainer sel, string teks, bool rataKanan) =>
        Rata(sel.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4), rataKanan).Text(teks);

    private static void SelTotal(IContainer sel, string teks, bool rataKanan) =>
        Rata(sel.BorderTop(1).Padding(4), rataKanan).Text(teks).Bold();

    private static IContainer Rata(IContainer sel, bool rataKanan) => rataKanan ? sel.AlignRight() : sel;

    private static string Rupiah(decimal nilai) => "Rp " + nilai.ToString("N0", Id);

    private static string Kg(decimal nilai) => nilai.ToString("#,0.##", Id) + " kg";
}
