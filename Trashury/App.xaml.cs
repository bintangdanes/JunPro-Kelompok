using System.IO;
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
        MuatKonfigurasiLingkungan();

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

    private static void MuatKonfigurasiLingkungan()
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(TrashuryDbContextFactory.VariabelKoneksiCloud)))
            return;

        string?[] kandidatFolder =
        [
            Path.GetDirectoryName(Environment.ProcessPath),
            AppContext.BaseDirectory,
            Directory.GetCurrentDirectory()
        ];

        foreach (string? folder in kandidatFolder)
        {
            if (string.IsNullOrWhiteSpace(folder))
                continue;

            string path = Path.Combine(folder, "trashury.env");
            if (!File.Exists(path))
                continue;

            foreach (string rawLine in File.ReadAllLines(path))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith('#'))
                    continue;

                int sep = line.IndexOf('=');
                if (sep <= 0)
                    continue;

                string key = line[..sep].Trim();
                string val = line[(sep + 1)..].Trim().Trim('"');
                if (key == TrashuryDbContextFactory.VariabelKoneksiCloud && !string.IsNullOrWhiteSpace(val))
                {
                    Environment.SetEnvironmentVariable(key, val);
                    return;
                }
            }
        }
    }
}
