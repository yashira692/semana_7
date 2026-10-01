using System.Net.Mail;
using System.Text.RegularExpressions;
using Biblioteca.Datos;
using Biblioteca.Entidades;
namespace Biblioteca.Negocio;

public class SocioNegocio
{
    private readonly SocioDatos _datos = new();

    public Task<List<Socio>> ListarAsync(string? filtro = null) => Guardia.Ejecutar(() => _datos.ListarAsync(filtro));

    public Task<int> InsertarAsync(Socio s) => Guardia.Ejecutar(async () =>
    {
        Validar(s);
        if (await _datos.ExisteDniAsync(s.DNI, 0))
            throw new ReglaNegocioException("Ya existe un socio con ese DNI.");
        return await _datos.InsertarAsync(s);
    });

    public Task ActualizarAsync(Socio s) => Guardia.Ejecutar(async () =>
    {
        if (s.SocioId <= 0) throw new ReglaNegocioException("Seleccione un socio de la lista.");
        Validar(s);
        var actual = await _datos.ObtenerAsync(s.SocioId);
        if (actual is null || !actual.Activo)
            throw new ReglaNegocioException("El socio no existe o está dado de baja.");
        if (await _datos.ExisteDniAsync(s.DNI, s.SocioId))
            throw new ReglaNegocioException("Ya existe otro socio con ese DNI.");
        await _datos.ActualizarAsync(s);
    });

    public Task EliminarAsync(int socioId) => Guardia.Ejecutar(async () =>
    {
        if (socioId <= 0) throw new ReglaNegocioException("Seleccione un socio de la lista.");
        if (await _datos.ContarPendientesAsync(socioId) > 0)
            throw new ReglaNegocioException("No se puede dar de baja: el socio tiene libros pendientes de devolución.");
        await _datos.DesactivarAsync(socioId); // eliminación lógica
    });

    private static void Validar(Socio s)
    {
        s.DNI = (s.DNI ?? "").Trim();
        s.Nombre = (s.Nombre ?? "").Trim();
        s.Email = string.IsNullOrWhiteSpace(s.Email) ? null : s.Email.Trim();
        if (!Regex.IsMatch(s.DNI, @"^\d{8}$")) throw new ReglaNegocioException("El DNI debe tener exactamente 8 dígitos.");
        if (s.Nombre.Length == 0) throw new ReglaNegocioException("El nombre es obligatorio.");
        if (s.Email is not null && !MailAddress.TryCreate(s.Email, out _))
            throw new ReglaNegocioException("El email no tiene un formato válido.");
    }
}