namespace Trashury.Models;

public class PenarikanSaldo : Transaksi
{
    public decimal JumlahTarik { get; set; }

    // Dipakai Entity Framework saat memuat data dari basis data.
    private PenarikanSaldo()
    {
    }

    public PenarikanSaldo(decimal jumlahTarik)
    {
        JumlahTarik = jumlahTarik;
        Nominal = jumlahTarik;
    }

    public override void Terapkan(Nasabah n)
    {
        ArgumentNullException.ThrowIfNull(n);
        n.Debit(JumlahTarik);
        Nominal = JumlahTarik;
        NasabahId = n.Id;
    }
}
