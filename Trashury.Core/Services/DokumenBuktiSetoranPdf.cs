using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Trashury.Models;

namespace Trashury.Services;

/// <summary>
/// Tata letak cetak bukti transaksi setoran sampah (struk PDF).
/// </summary>
public static class DokumenBuktiSetoranPdf
{
    private static readonly CultureInfo Id = CultureInfo.GetCultureInfo("id-ID");

    static DokumenBuktiSetoranPdf()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Buat(SetoranSampah setoran, string namaNasabah, decimal estimasiCo2e)
    {
        ArgumentNullException.ThrowIfNull(setoran);
        ArgumentException.ThrowIfNullOrWhiteSpace(namaNasabah);

        return Document.Create(dokumen => dokumen.Page(halaman =>
        {
            halaman.Size(PageSizes.A5);
            halaman.Margin(1.5f, Unit.Centimetre);
            halaman.DefaultTextStyle(gaya => gaya.FontSize(10));

            halaman.Header().Column(kolom =>
            {
                kolom.Item().Text("TRASHURY — Bukti Setoran Sampah").FontSize(14).Bold();
                kolom.Item().Text($"No. Transaksi: #{setoran.Id} | Tanggal: {setoran.Tanggal.ToString("dd/MM/yyyy HH:mm", Id)}");
                kolom.Item().Text($"Nasabah: {namaNasabah}");
                kolom.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Medium);
            });

            halaman.Content().PaddingTop(10).Column(kolom =>
            {
                kolom.Spacing(8);
                kolom.Item().Table(tabel =>
                {
                    tabel.ColumnsDefinition(def =>
                    {
                        def.RelativeColumn(3);
                        def.RelativeColumn(1.5f);
                        def.RelativeColumn(2);
                        def.RelativeColumn(2);
                    });

                    tabel.Header(header =>
                    {
                        header.Cell().Text("Kategori").Bold();
                        header.Cell().AlignRight().Text("Berat (kg)").Bold();
                        header.Cell().AlignRight().Text("Harga/kg").Bold();
                        header.Cell().AlignRight().Text("Subtotal").Bold();
                    });

                    foreach (var d in setoran.DetailSetoran)
                    {
                        tabel.Cell().Text(d.Kategori.Nama);
                        tabel.Cell().AlignRight().Text(d.BeratKg.ToString("N2", Id));
                        tabel.Cell().AlignRight().Text($"Rp {d.Kategori.HargaPerKg.ToString("N0", Id)}");
                        tabel.Cell().AlignRight().Text($"Rp {d.Subtotal.ToString("N0", Id)}");
                    }
                });

                kolom.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                kolom.Item().Text($"Total Berat: {setoran.TotalBeratKg.ToString("N2", Id)} kg");
                kolom.Item().Text($"Total Nominal: Rp {setoran.Nominal.ToString("N0", Id)}").Bold().FontSize(12);
                kolom.Item().Text($"Estimasi CO2e Dihindari: {estimasiCo2e.ToString("N2", Id)} kg CO2e").FontColor(Colors.Green.Darken2);
            });
        })).GeneratePdf();
    }
}
