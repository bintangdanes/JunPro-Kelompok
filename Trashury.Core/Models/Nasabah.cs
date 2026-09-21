namespace Trashury.Models;

public class Nasabah
{
    private decimal _saldo;

    public int Id { get; set; }
    public string Nama { get; set; }

    /// <summary>
    /// Saldo hanya dapat dibaca dari luar. Perubahan wajib lewat
    /// <see cref="Kredit"/> atau <see cref="Debit"/> agar validasi tidak terlewat.
    /// </summary>
    public decimal Saldo => _saldo;

    public Nasabah(int id, string nama)
    {
        Id = id;
        Nama = nama ?? throw new ArgumentNullException(nameof(nama));
    }

    /// <summary>
    /// Nasabah baru; Id akan diisi otomatis oleh basis data saat disimpan.
    /// </summary>
    public Nasabah(string nama) : this(0, nama)
    {
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
