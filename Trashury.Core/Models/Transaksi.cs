namespace Trashury.Models;

public abstract class Transaksi
{
    public int Id { get; set; }

    /// <summary>
    /// Pemilik transaksi. Diisi otomatis oleh <see cref="Terapkan"/>.
    /// </summary>
    public int NasabahId { get; set; }

    public DateTime Tanggal { get; set; } = DateTime.Now;
    public decimal Nominal { get; protected set; }

    public abstract void Terapkan(Nasabah n);
}
