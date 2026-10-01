namespace Biblioteca.Entidades;

public interface ILibroRepositorio
{
    Task<List<Libro>> ListarAsync(string? filtro);
    Task<Libro?> ObtenerAsync(int id);
    Task<bool> ExisteIsbnAsync(string isbn, int excluirId);
    Task<int> InsertarAsync(Libro libro);
    Task ActualizarAsync(Libro libro);
    Task DesactivarAsync(int id);
    Task<bool> TienePendientesAsync(int id);
}