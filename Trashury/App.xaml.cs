using System.Windows;
using Microsoft.EntityFrameworkCore;
using Trashury.Data;

namespace Trashury;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        // Otomatis membuat database SQLite dan menjalankan migrasi saat aplikasi dibuka
        using var db = TrashuryDbContextFactory.Buat();
        db.Database.Migrate();
    }
}