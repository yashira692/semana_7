using Biblioteca.Datos;
using Biblioteca.Entidades;
namespace Biblioteca.Negocio;

public class PrestamoNegocio
{
    public const decimal MultaPorDia = 1.50m;  // S/ por día de retraso
    public const int MaxLibrosPendientes = 3;
    public const int DiasDePrestamo = 7;

    private readonly PrestamoDatos _prestamos = new();
    private readonly SocioDatos _socios = new();
    private readonly LibroDatos _libros = new();

    public static decimal CalcularMulta(DateTime fechaLimite, DateTime fechaDevolucion)
    {
        int diasRetraso = (fechaDevolucion.Date - fechaLimite.Date).Days;
        return diasRetraso > 0 ? diasRetraso * MultaPorDia : 0m;
    }

    public Task<int> RegistrarPrestamoAsync(int socioId, List<int> libroIds) => Guardia.Ejecutar(async () =>
    {
        if (socioId <= 0) throw new ReglaNegocioException("Seleccione un socio.");
        if (libroIds is null || libroIds.Count == 0) throw new ReglaNegocioException("Agregue al menos un libro.");
        if (libroIds.Distinct().Count() != libroIds.Count)
            throw new ReglaNegocioException("No repita el mismo libro en un préstamo.");

        var socio = await _socios.ObtenerAsync(socioId);
        if (socio is null || !socio.Activo)
            throw new ReglaNegocioException("El socio no existe o está dado de baja.");

        int pendientes = await _socios.ContarPendientesAsync(socioId);
        if (pendientes + libroIds.Count > MaxLibrosPendientes)
            throw new ReglaNegocioException(
                $"El socio tiene {pendientes} libro(s) pendiente(s). Con este préstamo superaría el máximo de {MaxLibrosPendientes}.");

        var prestamo = new Prestamo
        {
            SocioId = socioId,
            FechaPrestamo = DateTime.Today,
            FechaLimite = DateTime.Today.AddDays(DiasDePrestamo)
        };

        foreach (int id in libroIds)
        {
            var libro = await _libros.ObtenerAsync(id);
            if (libro is null || !libro.Activo)
                throw new ReglaNegocioException($"El libro con Id {id} no existe o está dado de baja.");
            if (libro.Ejemplares <= 0)
                throw new ReglaNegocioException($"No hay ejemplares disponibles de \"{libro.Titulo}\".");
            prestamo.Detalles.Add(new DetallePrestamo { LibroId = id });
        }

        return await _prestamos.RegistrarAsync(prestamo);
    });

    public Task<List<DetallePrestamo>> ListarPendientesSocioAsync(int socioId) => Guardia.Ejecutar(async () =>
    {
        if (socioId <= 0) throw new ReglaNegocioException("Seleccione un socio.");
        return await _prestamos.ListarPendientesSocioAsync(socioId);
    });

    // Devuelve la multa calculada
    public Task<decimal> DevolverAsync(int prestamoId, int libroId) => Guardia.Ejecutar(async () =>
    {
        var detalle = await _prestamos.ObtenerDetalleAsync(prestamoId, libroId)
            ?? throw new ReglaNegocioException("No se encontró ese libro en el préstamo.");
        if (detalle.FechaDevolucion is not null)
            throw new ReglaNegocioException("Ese libro ya fue devuelto.");

        DateTime hoy = DateTime.Today;
        decimal multa = CalcularMulta(detalle.FechaLimite!.Value, hoy);

        int pendientes = await _prestamos.ContarPendientesPrestamoAsync(prestamoId);
        bool cerrar = pendientes == 1; // era el último pendiente → el préstamo pasa a Devuelto

        await _prestamos.RegistrarDevolucionAsync(prestamoId, libroId, hoy, cerrar);
        return multa;
    });

    public Task<List<ReportePrestamo>> ReporteAsync(DateTime? desde, DateTime? hasta) => Guardia.Ejecutar(async () =>
    {
        if (desde is null || hasta is null) throw new ReglaNegocioException("Seleccione ambas fechas.");
        if (desde > hasta) throw new ReglaNegocioException("La fecha inicial no puede ser mayor que la final.");
        return await _prestamos.ReporteAsync(desde.Value, hasta.Value);
    });
}