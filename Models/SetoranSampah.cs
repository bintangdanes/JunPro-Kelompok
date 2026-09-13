using System.Linq;

namespace Trashury.Models;

public class SetoranSampah : Transaksi
{
    public decimal TotalBeratKg => DetailSetoran.Sum(detail => detail.BeratKg);
    public List<DetailSetoran> DetailSetoran { get; } = new();

    public SetoranSampah()
    {
    }

    public SetoranSampah(IEnumerable<DetailSetoran> detailSetoran)
    {
        DetailSetoran.AddRange(detailSetoran ?? throw new ArgumentNullException(nameof(detailSetoran)));
        Nominal = DetailSetoran.Sum(detail => detail.Subtotal);
    }

    public override void Terapkan(Nasabah n)
    {
        ArgumentNullException.ThrowIfNull(n);
        Nominal = DetailSetoran.Sum(detail => detail.Subtotal);
        n.Kredit(Nominal);
    }
}
