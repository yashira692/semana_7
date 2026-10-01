using Microsoft.Data.SqlClient;
namespace Biblioteca.Negocio;

internal static class Guardia
{
    public static async Task<T> Ejecutar<T>(Func<Task<T>> accion)
    {
        try { return await accion(); }
        catch (SqlException) { throw new ReglaNegocioException("No se pudo completar la operación por un problema con la base de datos. Intente nuevamente."); }
        catch (InvalidOperationException ex) { throw new ReglaNegocioException(ex.Message); }
    }

    public static async Task Ejecutar(Func<Task> accion)
    {
        try { await accion(); }
        catch (SqlException) { throw new ReglaNegocioException("No se pudo completar la operación por un problema con la base de datos. Intente nuevamente."); }
        catch (InvalidOperationException ex) { throw new ReglaNegocioException(ex.Message); }
    }
}