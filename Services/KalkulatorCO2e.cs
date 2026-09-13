using System.Linq;
using Trashury.Interfaces;
using Trashury.Models;

namespace Trashury.Services;

public class KalkulatorCO2e : IKalkulatorDampak
{
    public decimal HitungCO2e(SetoranSampah s)
    {
        ArgumentNullException.ThrowIfNull(s);
        return s.DetailSetoran.Sum(detail => detail.Kategori.HitungCO2e(detail.BeratKg));
    }
}
