using Biblioteca.Entidades;
using Microsoft.Data.SqlClient;
namespace Biblioteca.Datos;

public class PrestamoDatos
{
    private const string SelDetalle = @"SELECT d.PrestamoId, d.LibroId, d.FechaDevolucion, l.Titulo, p.FechaLimite
        FROM DetallePrestamo d
        INNER JOIN Prestamos p ON p.PrestamoId = d.PrestamoId
        INNER JOIN Libros l ON l.LibroId = d.LibroId ";

    private static DetallePrestamo MapaDetalle(SqlDataReader r) => new()
    {
        PrestamoId = r.GetInt32(0),
        LibroId = r.GetInt32(1),
        FechaDevolucion = r.IsDBNull(2) ? null : r.GetDateTime(2),
        LibroTitulo = r.GetString(3),
        FechaLimite = r.GetDateTime(4)
    };

    private static async Task<int> EjecutarTx(SqlConnection cn, SqlTransaction tx, string sql, params SqlParameter[] ps)
    {
        await using var cmd = new SqlCommand(sql, cn, tx);
        cmd.Parameters.AddRange(ps);
        return await cmd.ExecuteNonQueryAsync();
    }

    // Cabecera + detalles + descuento de ejemplares en UNA sola transacción
    public async Task<int> RegistrarAsync(Prestamo p)
    {
        await using var cn = new SqlConnection(Db.Cadena);
        await cn.OpenAsync();
        await using var tx = (SqlTransaction)await cn.BeginTransactionAsync();
        try
        {
            int id;
            await using (var cmd = new SqlCommand(
                @"INSERT INTO Prestamos (SocioId, FechaPrestamo, FechaLimite, Estado)
                  VALUES (@s, @fp, @fl, 'Pendiente'); SELECT CAST(SCOPE_IDENTITY() AS INT);", cn, tx))
            {
                cmd.Parameters.Add(Db.P("@s", p.SocioId));
                cmd.Parameters.Add(Db.P("@fp", p.FechaPrestamo.Date));
                cmd.Parameters.Add(Db.P("@fl", p.FechaLimite.Date));
                id = (int)(await cmd.ExecuteScalarAsync())!;
            }

            foreach (var d in p.Detalles)
            {
                await EjecutarTx(cn, tx, "INSERT INTO DetallePrestamo (PrestamoId, LibroId) VALUES (@p, @l)",
                    Db.P("@p", id), Db.P("@l", d.LibroId));

                int filas = await EjecutarTx(cn, tx,
                    "UPDATE Libros SET Ejemplares = Ejemplares - 1 WHERE LibroId = @l AND Ejemplares > 0",
                    Db.P("@l", d.LibroId));
                if (filas == 0) throw new InvalidOperationException("Un libro se quedó sin ejemplares durante el registro.");
            }
            await tx.CommitAsync();
            return id;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task RegistrarDevolucionAsync(int prestamoId, int libroId, DateTime fecha, bool cerrarPrestamo)
    {
        await using var cn = new SqlConnection(Db.Cadena);
        await cn.OpenAsync();
        await using var tx = (SqlTransaction)await cn.BeginTransactionAsync();
        try
        {
            int filas = await EjecutarTx(cn, tx,
                @"UPDATE DetallePrestamo SET FechaDevolucion = @f
                  WHERE PrestamoId = @p AND LibroId = @l AND FechaDevolucion IS NULL",
                Db.P("@f", fecha.Date), Db.P("@p", prestamoId), Db.P("@l", libroId));
            if (filas == 0) throw new InvalidOperationException("Ese libro ya fue devuelto.");

            await EjecutarTx(cn, tx, "UPDATE Libros SET Ejemplares = Ejemplares + 1 WHERE LibroId = @l",
                Db.P("@l", libroId));

            if (cerrarPrestamo)
                await EjecutarTx(cn, tx, "UPDATE Prestamos SET Estado = 'Devuelto' WHERE PrestamoId = @p",
                    Db.P("@p", prestamoId));

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<DetallePrestamo?> ObtenerDetalleAsync(int prestamoId, int libroId) =>
        (await Db.ListarAsync(SelDetalle + "WHERE d.PrestamoId = @p AND d.LibroId = @l",
            MapaDetalle, Db.P("@p", prestamoId), Db.P("@l", libroId))).FirstOrDefault();

    public Task<List<DetallePrestamo>> ListarPendientesSocioAsync(int socioId) => Db.ListarAsync(
        SelDetalle + "WHERE p.SocioId = @s AND d.FechaDevolucion IS NULL ORDER BY p.FechaLimite",
        MapaDetalle, Db.P("@s", socioId));

    public async Task<int> ContarPendientesPrestamoAsync(int prestamoId) =>
        (int)(await Db.EscalarAsync(
            "SELECT COUNT(*) FROM DetallePrestamo WHERE PrestamoId = @p AND FechaDevolucion IS NULL",
            Db.P("@p", prestamoId)))!;

    // Reporte con INNER JOIN entre Prestamos, DetallePrestamo, Libros y Socios
    public Task<List<ReportePrestamo>> ReporteAsync(DateTime desde, DateTime hasta) => Db.ListarAsync(
        @"SELECT p.PrestamoId, s.Nombre, l.Titulo, p.FechaPrestamo, p.FechaLimite, p.Estado
          FROM Prestamos p
          INNER JOIN DetallePrestamo d ON d.PrestamoId = p.PrestamoId
          INNER JOIN Libros l ON l.LibroId = d.LibroId
          INNER JOIN Socios s ON s.SocioId = p.SocioId
          WHERE p.FechaPrestamo BETWEEN @d AND @h
          ORDER BY p.FechaPrestamo, p.PrestamoId",
        r => new ReportePrestamo
        {
            PrestamoId = r.GetInt32(0),
            Socio = r.GetString(1),
            Libro = r.GetString(2),
            FechaPrestamo = r.GetDateTime(3),
            FechaLimite = r.GetDateTime(4),
            Estado = r.GetString(5)
        },
        Db.P("@d", desde.Date), Db.P("@h", hasta.Date));
}