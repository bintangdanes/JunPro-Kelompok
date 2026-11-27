using System.Globalization;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Trashury.Services;

public record BarisEksporRiwayat(
    int Id,
    DateTime Tanggal,
    string NamaNasabah,
    string Jenis,
    string Rincian,
    decimal Nominal);

/// <summary>
/// Ekspor daftar riwayat transaksi terfilter ke format CSV dan PDF.
/// </summary>
public static class DokumenRiwayatTransaksi
{
    private static readonly CultureInfo Id = CultureInfo.GetCultureInfo("id-ID");

    static DokumenRiwayatTransaksi()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] BuatCsv(IEnumerable<BarisEksporRiwayat> daftar)
    {
        var sb = new StringBuilder();
        sb.AppendLine("ID;Tanggal;Nama Nasabah;Jenis;Rincian;Nominal (Rp)");

        foreach (var item in daftar)
        {
            string nama = BersihkanCsv(item.NamaNasabah);
            string rincian = BersihkanCsv(item.Rincian);
            sb.AppendLine($"{item.Id};{item.Tanggal:dd/MM/yyyy HH:mm};{nama};{item.Jenis};{rincian};{item.Nominal.ToString("0", Id)}");
        }

        // UTF-8 BOM agar terbaca rapi di Excel Indonesia
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    public static byte[] BuatPdf(IReadOnlyList<BarisEksporRiwayat> daftar, string keteranganFilter)
    {
        decimal totalSetoran = daftar.Where(d => d.Jenis == "Setoran").Sum(d => d.Nominal);
        decimal totalPenarikan = daftar.Where(d => d.Jenis == "Penarikan").Sum(d => d.Nominal);

        return Document.Create(dokumen => dokumen.Page(halaman =>
        {
            halaman.Size(PageSizes.A4);
            halaman.Margin(1.8f, Unit.Centimetre);
            halaman.DefaultTextStyle(gaya => gaya.FontSize(9.5f));

            halaman.Header().Column(kolom =>
            {
                kolom.Item().Text("TRASHURY — Riwayat Transaksi Bank Sampah").FontSize(15).Bold().FontColor(Colors.Green.Darken3);
                kolom.Item().Text($"Filter: {keteranganFilter} | Dicetak: {DateTime.Now.ToString("dd/MM/yyyy HH:mm", Id)}").FontSize(9).FontColor(Colors.Grey.Darken2);
                kolom.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Medium);
            });

            halaman.Content().PaddingTop(10).Column(kolom =>
            {
                kolom.Spacing(10);

                kolom.Item().Row(row =>
                {
                    row.RelativeItem().Text($"Jumlah Transaksi: {daftar.Count}").Bold();
                    row.RelativeItem().Text($"Total Setoran: Rp {totalSetoran.ToString("N0", Id)}").Bold().FontColor(Colors.Green.Darken3);
                    row.RelativeItem().Text($"Total Penarikan: Rp {totalPenarikan.ToString("N0", Id)}").Bold().FontColor(Colors.Red.Darken2);
                });

                kolom.Item().Table(tabel =>
                {
                    tabel.ColumnsDefinition(def =>
                    {
                        def.ConstantColumn(32);
                        def.ConstantColumn(85);
                        def.RelativeColumn(2);
                        def.ConstantColumn(60);
                        def.RelativeColumn(3);
                        def.ConstantColumn(75);
                    });

                    tabel.Header(header =>
                    {
                        header.Cell().Background(Colors.Green.Lighten5).Padding(4).Text("ID").Bold();
                        header.Cell().Background(Colors.Green.Lighten5).Padding(4).Text("Tanggal").Bold();
                        header.Cell().Background(Colors.Green.Lighten5).Padding(4).Text("Nama Nasabah").Bold();
                        header.Cell().Background(Colors.Green.Lighten5).Padding(4).Text("Jenis").Bold();
                        header.Cell().Background(Colors.Green.Lighten5).Padding(4).Text("Rincian").Bold();
                        header.Cell().Background(Colors.Green.Lighten5).Padding(4).AlignRight().Text("Nominal").Bold();
                    });

                    foreach (var item in daftar)
                    {
                        tabel.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(item.Id.ToString());
                        tabel.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(item.Tanggal.ToString("dd/MM/yy HH:mm", Id));
                        tabel.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(item.NamaNasabah);
                        tabel.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(item.Jenis);
                        tabel.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(item.Rincian);
                        tabel.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text($"Rp {item.Nominal.ToString("N0", Id)}");
                    }
                });
            });
        })).GeneratePdf();
    }

    private static string BersihkanCsv(string teks)
    {
        if (string.IsNullOrEmpty(teks)) return string.Empty;
        string bersih = teks.Replace("\"", "\"\"");
        return bersih.Contains(';') || bersih.Contains('\n') ? $"\"{bersih}\"" : bersih;
    }
}
