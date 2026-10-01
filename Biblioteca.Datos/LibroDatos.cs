using Biblioteca.Entidades;
using Microsoft.Data.SqlClient;
namespace Biblioteca.Datos;

public class LibroDatos : ILibroRepositorio
{
    private const string Sel = @"SELECT l.LibroId, l.Titulo, l.ISBN, l.AutorId, a.Nombre, l.Ejemplares, l.Activo
                                 FROM Libros l INNER JOIN Autores a ON a.AutorId = l.AutorId ";

    private static Libro Mapa(SqlDataReader r) => new()
    {
        LibroId = r.GetInt32(0),
        Titulo = r.GetString(1),
        ISBN = r.GetString(2),
        AutorId = r.GetInt32(3),
        AutorNombre = r.GetString(4),
        Ejemplares = r.GetInt32(5),
        Activo = r.GetBoolean(6)
    };

    public Task<List<Libro>> ListarAsync(string? filtro) => Db.ListarAsync(
        Sel + @"WHERE l.Activo = 1 AND (@f IS NULL OR l.Titulo LIKE '%' + @f + '%' OR a.Nombre LIKE '%' + @f + '%')
                ORDER BY l.Titulo",
        Mapa, Db.P("@f", string.IsNullOrWhiteSpace(filtro) ? null : filtro.Trim()));

    public async Task<Libro?> ObtenerAsync(int id) =>
        (await Db.ListarAsync(Sel + "WHERE l.LibroId = @id", Mapa, Db.P("@id", id))).FirstOrDefault();

    public async Task<bool> ExisteIsbnAsync(string isbn, int excluirId) =>
        (int)(await Db.EscalarAsync("SELECT COUNT(*) FROM Libros WHERE ISBN = @i AND LibroId <> @x",
            Db.P("@i", isbn), Db.P("@x", excluirId)))! > 0;

    public async Task<int> InsertarAsync(Libro l) =>
        (int)(await Db.EscalarAsync(
            @"INSERT INTO Libros (Titulo, ISBN, AutorId, Ejemplares) OUTPUT INSERTED.LibroId
              VALUES (@t, @i, @a, @e)",
            Db.P("@t", l.Titulo), Db.P("@i", l.ISBN), Db.P("@a", l.AutorId), Db.P("@e", l.Ejemplares)))!;

    public Task ActualizarAsync(Libro l) => Db.EjecutarAsync(
        "UPDATE Libros SET Titulo=@t, ISBN=@i, AutorId=@a, Ejemplares=@e WHERE LibroId=@id",
        Db.P("@t", l.Titulo), Db.P("@i", l.ISBN), Db.P("@a", l.AutorId),
        Db.P("@e", l.Ejemplares), Db.P("@id", l.LibroId));

    public Task DesactivarAsync(int id) =>
        Db.EjecutarAsync("UPDATE Libros SET Activo = 0 WHERE LibroId = @id", Db.P("@id", id));

    public async Task<bool> TienePendientesAsync(int id) =>
        (int)(await Db.EscalarAsync(
            "SELECT COUNT(*) FROM DetallePrestamo WHERE LibroId = @id AND FechaDevolucion IS NULL",
            Db.P("@id", id)))! > 0;
}
