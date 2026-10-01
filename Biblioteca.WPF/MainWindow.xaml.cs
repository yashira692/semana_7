using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Biblioteca.Entidades;
using Biblioteca.Negocio;

namespace Biblioteca.WPF;

public partial class MainWindow : Window
{
    private readonly LibroNegocio _libros = new();
    private readonly SocioNegocio _socios = new();
    private readonly PrestamoNegocio _prestamos = new();
    private readonly ObservableCollection<Libro> _carrito = new();

    public MainWindow()
    {
        InitializeComponent();
        dgCarrito.ItemsSource = _carrito;
        dpHasta.SelectedDate = DateTime.Today;
        dpDesde.SelectedDate = DateTime.Today.AddDays(-30);
    }

    // ---------- Utilidades ----------
    private async Task Seguro(Func<Task> accion)
    {
        try { await accion(); }
        catch (ReglaNegocioException ex)
        {
            MessageBox.Show(ex.Message, "Regla de negocio", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Ocurrió un error inesperado: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e) => await Seguro(async () =>
    {
        cmbAutor.ItemsSource = await _libros.ListarAutoresAsync();
        await RefrescarLibrosAsync();
        await RefrescarSociosAsync();
    });

    private async Task RefrescarLibrosAsync()
    {
        dgLibros.ItemsSource = await _libros.ListarAsync(txtBuscarLibro.Text);
        cmbLibroPrestamo.ItemsSource = await _libros.ListarAsync(null);
    }

    private async Task RefrescarSociosAsync()
    {
        dgSocios.ItemsSource = await _socios.ListarAsync(txtBuscarSocio.Text);
        var todos = await _socios.ListarAsync(null);
        cmbSocioPrestamo.ItemsSource = todos;
        cmbSocioDev.ItemsSource = todos;
    }

    // ---------- LIBROS ----------
    private Libro LeerLibro()
    {
        if (!int.TryParse(txtEjemplares.Text, out int ej)) ej = -1;
        return new Libro
        {
            LibroId = (dgLibros.SelectedItem as Libro)?.LibroId ?? 0,
            Titulo = txtTitulo.Text,
            ISBN = txtIsbn.Text,
            AutorId = cmbAutor.SelectedValue is int a ? a : 0,
            Ejemplares = ej
        };
    }

    private void LimpiarLibro()
    {
        dgLibros.UnselectAll();
        txtTitulo.Clear(); txtIsbn.Clear(); txtEjemplares.Clear();
        cmbAutor.SelectedIndex = -1;
    }

    private void dgLibros_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (dgLibros.SelectedItem is Libro l)
        {
            txtTitulo.Text = l.Titulo; txtIsbn.Text = l.ISBN;
            cmbAutor.SelectedValue = l.AutorId; txtEjemplares.Text = l.Ejemplares.ToString();
        }
    }

    private async void BuscarLibro_Click(object sender, RoutedEventArgs e) =>
        await Seguro(async () => dgLibros.ItemsSource = await _libros.ListarAsync(txtBuscarLibro.Text));

    private async void InsertarLibro_Click(object sender, RoutedEventArgs e) => await Seguro(async () =>
    {
        var l = LeerLibro(); l.LibroId = 0;
        await _libros.InsertarAsync(l);
        LimpiarLibro(); await RefrescarLibrosAsync();
        MessageBox.Show("Libro registrado correctamente.");
    });

    private async void ActualizarLibro_Click(object sender, RoutedEventArgs e) => await Seguro(async () =>
    {
        await _libros.ActualizarAsync(LeerLibro());
        LimpiarLibro(); await RefrescarLibrosAsync();
        MessageBox.Show("Libro actualizado correctamente.");
    });

    private async void EliminarLibro_Click(object sender, RoutedEventArgs e) => await Seguro(async () =>
    {
        int id = (dgLibros.SelectedItem as Libro)?.LibroId ?? 0;
        if (id > 0 && MessageBox.Show("¿Dar de baja este libro?", "Confirmar", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        await _libros.EliminarAsync(id);
        LimpiarLibro(); await RefrescarLibrosAsync();
        MessageBox.Show("Libro dado de baja.");
    });

    private void LimpiarLibro_Click(object sender, RoutedEventArgs e) => LimpiarLibro();

    // ---------- SOCIOS ----------
    private Socio LeerSocio() => new()
    {
        SocioId = (dgSocios.SelectedItem as Socio)?.SocioId ?? 0,
        DNI = txtDni.Text,
        Nombre = txtNombreSocio.Text,
        Email = txtEmail.Text
    };

    private void LimpiarSocio()
    {
        dgSocios.UnselectAll();
        txtDni.Clear(); txtNombreSocio.Clear(); txtEmail.Clear();
    }

    private void dgSocios_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (dgSocios.SelectedItem is Socio s)
        { txtDni.Text = s.DNI; txtNombreSocio.Text = s.Nombre; txtEmail.Text = s.Email; }
    }

    private async void BuscarSocio_Click(object sender, RoutedEventArgs e) =>
        await Seguro(async () => dgSocios.ItemsSource = await _socios.ListarAsync(txtBuscarSocio.Text));

    private async void InsertarSocio_Click(object sender, RoutedEventArgs e) => await Seguro(async () =>
    {
        var s = LeerSocio(); s.SocioId = 0;
        await _socios.InsertarAsync(s);
        LimpiarSocio(); await RefrescarSociosAsync();
        MessageBox.Show("Socio registrado correctamente.");
    });

    private async void ActualizarSocio_Click(object sender, RoutedEventArgs e) => await Seguro(async () =>
    {
        await _socios.ActualizarAsync(LeerSocio());
        LimpiarSocio(); await RefrescarSociosAsync();
        MessageBox.Show("Socio actualizado correctamente.");
    });

    private async void EliminarSocio_Click(object sender, RoutedEventArgs e) => await Seguro(async () =>
    {
        int id = (dgSocios.SelectedItem as Socio)?.SocioId ?? 0;
        if (id > 0 && MessageBox.Show("¿Dar de baja a este socio?", "Confirmar", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        await _socios.EliminarAsync(id);
        LimpiarSocio(); await RefrescarSociosAsync();
        MessageBox.Show("Socio dado de baja.");
    });

    private void LimpiarSocio_Click(object sender, RoutedEventArgs e) => LimpiarSocio();

    // ---------- PRÉSTAMO ----------
    private void AgregarLibro_Click(object sender, RoutedEventArgs e)
    {
        if (cmbLibroPrestamo.SelectedItem is not Libro libro)
        { MessageBox.Show("Seleccione un libro."); return; }
        if (_carrito.Any(x => x.LibroId == libro.LibroId))
        { MessageBox.Show("Ese libro ya está en la lista."); return; }
        _carrito.Add(libro);
    }

    private void QuitarLibro_Click(object sender, RoutedEventArgs e)
    {
        if (dgCarrito.SelectedItem is Libro l) _carrito.Remove(l);
    }

    private async void RegistrarPrestamo_Click(object sender, RoutedEventArgs e) => await Seguro(async () =>
    {
        int socioId = cmbSocioPrestamo.SelectedValue is int id ? id : 0;
        int prestamoId = await _prestamos.RegistrarPrestamoAsync(socioId, _carrito.Select(l => l.LibroId).ToList());
        _carrito.Clear();
        await RefrescarLibrosAsync();
        MessageBox.Show($"Préstamo N° {prestamoId} registrado correctamente.");
    });

    // ---------- DEVOLUCIÓN ----------
    private async Task CargarPendientesAsync()
    {
        int socioId = cmbSocioDev.SelectedValue is int id ? id : 0;
        dgPendientes.ItemsSource = await _prestamos.ListarPendientesSocioAsync(socioId);
        txtMulta.Text = "Multa: S/ 0.00";
    }

    private async void CargarPendientes_Click(object sender, RoutedEventArgs e) =>
        await Seguro(CargarPendientesAsync);

    private void dgPendientes_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (dgPendientes.SelectedItem is DetallePrestamo d && d.FechaLimite is not null)
        {
            decimal multa = PrestamoNegocio.CalcularMulta(d.FechaLimite.Value, DateTime.Today);
            txtMulta.Text = $"Multa: S/ {multa:0.00}";
        }
    }

    private async void Devolver_Click(object sender, RoutedEventArgs e) => await Seguro(async () =>
    {
        if (dgPendientes.SelectedItem is not DetallePrestamo d)
        { MessageBox.Show("Seleccione el libro que se devuelve."); return; }

        decimal multa = await _prestamos.DevolverAsync(d.PrestamoId, d.LibroId);
        await CargarPendientesAsync();
        await RefrescarLibrosAsync();
        MessageBox.Show(multa > 0 ? $"Devolución registrada. Multa a cobrar: S/ {multa:0.00}"
                                  : "Devolución registrada. Sin multa.");
    });

    // ---------- REPORTE ----------
    private async void GenerarReporte_Click(object sender, RoutedEventArgs e) => await Seguro(async () =>
        dgReporte.ItemsSource = await _prestamos.ReporteAsync(dpDesde.SelectedDate, dpHasta.SelectedDate));
}