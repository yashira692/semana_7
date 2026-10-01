using Biblioteca.Entidades;
using Microsoft.Data.SqlClient;
namespace Biblioteca.Datos;

public class SocioDatos
{
    private const string Sel = "SELECT SocioId, DNI, Nombre, Email, Activo FROM Socios ";

    private static Socio Mapa(SqlDataReader r) => new()
    {
        SocioId = r.GetInt32(0),
        DNI = r.GetString(1),
        Nombre = r.GetString(2),
        Email = r.IsDBNull(3) ? null : r.GetString(3),
        Activo = r.GetBoolean(4)
    };

    public Task<List<Socio>> ListarAsync(string? filtro) => Db.ListarAsync(
        Sel + @"WHERE Activo = 1 AND (@f IS NULL OR Nombre LIKE '%' + @f + '%' OR DNI LIKE '%' + @f + '%')
                ORDER BY Nombre",
        Mapa, Db.P("@f", string.IsNullOrWhiteSpace(filtro) ? null : filtro.Trim()));

    public async Task<Socio?> ObtenerAsync(int id) =>
        (await Db.ListarAsync(Sel + "WHERE SocioId = @id", Mapa, Db.P("@id", id))).FirstOrDefault();

    public async Task<bool> ExisteDniAsync(string dni, int excluirId) =>
        (int)(await Db.EscalarAsync("SELECT COUNT(*) FROM Socios WHERE DNI = @d AND SocioId <> @x",
            Db.P("@d", dni), Db.P("@x", excluirId)))! > 0;

    public async Task<int> InsertarAsync(Socio s) =>
        (int)(await Db.EscalarAsync(
            "INSERT INTO Socios (DNI, Nombre, Email) OUTPUT INSERTED.SocioId VALUES (@d, @n, @e)",
            Db.P("@d", s.DNI), Db.P("@n", s.Nombre), Db.P("@e", s.Email)))!;

    public Task ActualizarAsync(Socio s) => Db.EjecutarAsync(
        "UPDATE Socios SET DNI=@d, Nombre=@n, Email=@e WHERE SocioId=@id",
        Db.P("@d", s.DNI), Db.P("@n", s.Nombre), Db.P("@e", s.Email), Db.P("@id", s.SocioId));

    public Task DesactivarAsync(int id) =>
        Db.EjecutarAsync("UPDATE Socios SET Activo = 0 WHERE SocioId = @id", Db.P("@id", id));

    public async Task<int> ContarPendientesAsync(int socioId) =>
        (int)(await Db.EscalarAsync(
            @"SELECT COUNT(*) FROM DetallePrestamo d INNER JOIN Prestamos p ON p.PrestamoId = d.PrestamoId
              WHERE p.SocioId = @s AND d.FechaDevolucion IS NULL", Db.P("@s", socioId)))!;
}