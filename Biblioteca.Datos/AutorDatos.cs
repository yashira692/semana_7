using Biblioteca.Entidades;
namespace Biblioteca.Datos;

public class AutorDatos
{
    public Task<List<Autor>> ListarAsync() => Db.ListarAsync(
        "SELECT AutorId, Nombre, Nacionalidad, Activo FROM Autores WHERE Activo = 1 ORDER BY Nombre",
        r => new Autor
        {
            AutorId = r.GetInt32(0),
            Nombre = r.GetString(1),
            Nacionalidad = r.IsDBNull(2) ? null : r.GetString(2),
            Activo = r.GetBoolean(3)
        });
}