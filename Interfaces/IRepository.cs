namespace Trashury.Interfaces;

public interface IRepository<T>
{
    T GetById(int id);
    IEnumerable<T> GetAll();
    void Tambah(T entitas);
    int SimpanPerubahan();
}
