namespace Trashury.Models;

public abstract class Transaksi
{
    public int Id { get; set; }
    public DateTime Tanggal { get; set; } = DateTime.Now;
    public decimal Nominal { get; protected set; }

    public abstract void Terapkan(Nasabah n);
}
