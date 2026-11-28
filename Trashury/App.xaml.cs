using System.Windows;
using Microsoft.EntityFrameworkCore;
using Trashury.Data;
using Trashury.Services;

namespace Trashury;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        using (var db = TrashuryDbContextFactory.BuatSesuaiKonfigurasi())
        {
            db.Database.Migrate();
            new LayananAutentikasi(db).PastikanAkunBawaan();
        }

        var login = new LoginWindow();
        if (login.ShowDialog() != true || login.PenggunaTerautentikasi == null)
        {
            Shutdown();
            return;
        }

        var mainWindow = new MainWindow(login.PenggunaTerautentikasi);
        MainWindow = mainWindow;
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        mainWindow.Show();
    }
}
