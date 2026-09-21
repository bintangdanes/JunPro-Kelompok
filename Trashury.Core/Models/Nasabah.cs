namespace Trashury.Models;

public class Nasabah
{
    private decimal _saldo;

    public int Id { get; set; }
    public string Nama { get; set; }
    public decimal Saldo => _saldo;

    public Nasabah(int id, string nama)
    {
        Id = id;
        Nama = nama ?? throw new ArgumentNullException(nameof(nama));
    }

    public void Kredit(decimal jumlah)
    {
        if (jumlah < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(jumlah), "Jumlah kredit tidak boleh negatif.");
        }

        _saldo += jumlah;
    }

    public void Debit(decimal jumlah)
    {
        if (jumlah < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(jumlah), "Jumlah debit tidak boleh negatif.");
        }

        if (jumlah > _saldo)
        {
            throw new InvalidOperationException("Saldo tidak mencukupi.");
        }

        _saldo -= jumlah;
    }
}
