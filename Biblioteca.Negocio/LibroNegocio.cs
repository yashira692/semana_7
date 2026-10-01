using Biblioteca.Datos;
using Biblioteca.Entidades;
namespace Biblioteca.Negocio;

public class LibroNegocio
{
    private readonly ILibroRepositorio _repo;

    public LibroNegocio() : this(new LibroDatos()) { }          // para que WPF no toque Datos
    public LibroNegocio(ILibroRepositorio repo) => _repo = repo; // inyección por constructor (punto extra)

    public Task<List<Autor>> ListarAutoresAsync() => Guardia.Ejecutar(() => new AutorDatos().ListarAsync());

    public Task<List<Libro>> ListarAsync(string? filtro = null) => Guardia.Ejecutar(() => _repo.ListarAsync(filtro));

    public Task<int> InsertarAsync(Libro l) => Guardia.Ejecutar(async () =>
    {
        Validar(l);
        if (await _repo.ExisteIsbnAsync(l.ISBN, 0))
            throw new ReglaNegocioException("Ya existe un libro con ese ISBN.");
        return await _repo.InsertarAsync(l);
    });

    public Task ActualizarAsync(Libro l) => Guardia.Ejecutar(async () =>
    {
        if (l.LibroId <= 0) throw new ReglaNegocioException("Seleccione un libro de la lista.");
        Validar(l);
        var actual = await _repo.ObtenerAsync(l.LibroId);
        if (actual is null || !actual.Activo)
            throw new ReglaNegocioException("El libro no existe o está dado de baja.");
        if (await _repo.ExisteIsbnAsync(l.ISBN, l.LibroId))
            throw new ReglaNegocioException("Ya existe otro libro con ese ISBN.");
        await _repo.ActualizarAsync(l);
    });

    public Task EliminarAsync(int libroId) => Guardia.Ejecutar(async () =>
    {
        if (libroId <= 0) throw new ReglaNegocioException("Seleccione un libro de la lista.");
        if (await _repo.TienePendientesAsync(libroId))
            throw new ReglaNegocioException("No se puede dar de baja: el libro tiene préstamos pendientes.");
        await _repo.DesactivarAsync(libroId); // eliminación lógica (Activo = 0)
    });

    private static void Validar(Libro l)
    {
        l.Titulo = (l.Titulo ?? "").Trim();
        l.ISBN = (l.ISBN ?? "").Trim();
        if (l.Titulo.Length == 0) throw new ReglaNegocioException("El título es obligatorio.");
        if (l.ISBN.Length == 0) throw new ReglaNegocioException("El ISBN es obligatorio.");
        if (l.AutorId <= 0) throw new ReglaNegocioException("Seleccione un autor.");
        if (l.Ejemplares < 0) throw new ReglaNegocioException("Los ejemplares deben ser un número entero mayor o igual a 0.");
    }
}