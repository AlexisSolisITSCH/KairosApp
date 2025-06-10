using System.Runtime;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using OfficeOpenXml;
using Microsoft.Win32;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using RestSharp;
using Newtonsoft.Json.Linq;
using KairosApp.Models;
using System.Windows.Threading;
using System.Net.Http;
using Newtonsoft.Json;
using System.Threading.Tasks;
using OfficeOpenXml.ConditionalFormatting.Contracts;
using KairosApp.Servicios;
using System.Text.RegularExpressions;
using FrontContext = KairosApp.Models.AppDbContext;
using FrontMensaje = KairosApp.Models.Mensaje;
using FrontRecibidos = KairosApp.Models.Recibidos;
using FrontContacto = KairosApp.Models.Contacto;

//referencias a KairosAPI
using KairosAPI.Data;
using KairosAPI.Models;
using BackendContext = KairosAPI.Data.AppDbContext;
using BackendMensaje = KairosAPI.Models.Mensaje;
using BackendContacto = KairosAPI.Models.Contacto;
using BackendRecibido = KairosAPI.DTOs.RecibidoDTO;
using ControlzEx.Standard;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using AppDbContext = KairosAPI.Data.AppDbContext;

namespace KairosApp;

public partial class MainWindow : Window
{
    private List<FrontContacto> contactos = new List<FrontContacto>();
    private List<FrontMensaje> mensajes = new List<FrontMensaje>();
    private List<BackendRecibido> respuesta = new List<BackendRecibido>();
    private bool sesionIniciada = true;
    private string seleccionartelefono = "";
    private DispatcherTimer _timer;
    private DispatcherTimer _conexion;
    private HubConnection? _HubConnection;

    private string FormatearEstado(string estado)
    {
        return estado switch
        {
            "sent" => "Enviado",
            "delivered" => "Entregado",
            "read" => "Leido",
            "failed" => "Fallido",
            _ => estado
        };
    }

    public MainWindow()
    {
        InitializeComponent();
        txtUsuario.Text = $"Sesion: {LoginUser.nomb} ({LoginUser.numcel})";
        InicializarSignalR();
        CargarContactosDB();
        CargarMensajesRecibidos();
        ControlEnvios();
        ActualizarEnvios();
        RecConnection();
        Loaded += MainWindow_Loaded;
        this.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;


        //Actualizar en tiempo real
        _timer = new DispatcherTimer();
        _timer.Interval = TimeSpan.FromSeconds(5);
        _timer.Tick += async (s, e) => await CargarMensajesRecibidos();
        _timer.Start();
    }

    private void _timer_Tick(object? sender, EventArgs e)
    {
        throw new NotImplementedException();
    }

    public static bool Est_Inter()
    {
        try
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(3);
                var response = client.GetAsync("https://www.google.com").Result;
                return response.IsSuccessStatusCode;
            }
        }
        catch
        {
            return false;
        }
    }

    private void BtnImportar_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog dialogo = new OpenFileDialog
        {
            Filter = "Archivos de Excel (*.xlsx)|*.xlsx",
            Title = "Seleccionar archivo de Excel"
        };

        if (dialogo.ShowDialog() == true)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(dialogo.FileName);
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (ExcelPackage package = new ExcelPackage(fileInfo))
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[0];

                    if (worksheet.Dimension == null)
                    {
                        MessageBox.Show("El archivo esta vacio", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    int rowCount = worksheet.Dimension.Rows;
                    int colCount = worksheet.Dimension.Columns;

                    string col1 = worksheet.Cells[1, 1].Text.Trim().ToLower();
                    string col2 = worksheet.Cells[1, 2].Text.Trim().ToLower();

                    if (col1 != "nombre" || col2 != "telefono")
                    {
                        MessageBox.Show("Formato incorrecto!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    List<FrontContacto> nuevosContactos = new List<FrontContacto>();

                    for (int row = 2; row <= rowCount; row++)
                    {
                        string nombre = worksheet.Cells[row, 1].Text;
                        string telefono = worksheet.Cells[row, 2].Text;

                        if (!string.IsNullOrWhiteSpace(nombre) && !string.IsNullOrWhiteSpace(telefono))
                        {
                            telefono = Regex.Replace(telefono.Trim(), @"[^\d]", "");

                            if (telefono.Length == 10)
                            {
                                telefono = "52" + telefono;
                            }

                            if (!Regex.IsMatch(telefono, @"^52\d{10}$"))
                            {
                                MessageBox.Show($"El telefono {telefono} no es valido. Asegurate que no tenga letras ni caracteres especiales",
                                    "Formato Incorrecto", MessageBoxButton.OK, MessageBoxImage.Warning);
                                continue;
                            }

                            bool existe = contactos.Any(c => c.Telefono == telefono) || nuevosContactos.Any(c => c.Telefono == telefono);
                            if (!existe)
                            {
                                nuevosContactos.Add(new FrontContacto { Nombre = nombre, Telefono = telefono });
                            }
                        }
                    }

                    contactos.AddRange(nuevosContactos);
                    dgContactos.ItemsSource = null;
                    dgContactos.ItemsSource = contactos;

                    GuardarContactosDB();
                    MessageBox.Show("Contactos importados!!", "Exito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            } catch (IOException)
            {
                MessageBox.Show("El archivo Excel esta abierto. Cierralo e intenta nuevamente", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error al importar el archivo: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void BtnLimpiar_Click(object sender, RoutedEventArgs e)
    {
        contactos.Clear();
        dgContactos.ItemsSource = null;
        MessageBox.Show("Lista limpiada!", "Informacion", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private string filNum(string numero)
    {
        if (string.IsNullOrWhiteSpace(numero))
            return "";

        return numero.Replace(" ", "")
            .Replace("(", "")
            .Replace(")", "")
            .Replace("-", "")
            .ToLower();
    }

    private void TxtBuscar_Up(object sender, System.Windows.Input.KeyEventArgs e)
    {
        string filtro = txtBuscar.Text.ToLower();

        var contactosFiltrados = contactos
            .Where(c => c.Nombre.ToLower().Contains(filtro) || c.Telefono.Contains(filtro))
            .ToList();

        dgContactos.ItemsSource = contactosFiltrados;
    }

    private void searchRec(object sender, System.Windows.Input.KeyEventArgs e)
    {
        string filtro = filNum(txtBuscarRec.Text);

        var datos = dgMensajesRecibidos.ItemsSource as List<BackendRecibido> ?? new List<BackendRecibido>();

        var messRec = respuesta
            .Where(c => filNum(c.CR_nolada).Contains(filtro) || 
            (!string.IsNullOrEmpty(c.Mensaje) && c.Mensaje.ToLower().Contains(txtBuscarRec.Text.ToLower())))
            .ToList();

        dgMensajesRecibidos.ItemsSource = messRec;
    }

    private void searchEnv(object sender, System.Windows.Input.KeyEventArgs e)
    {
        string filtro = filNum(txtBuscarEnv.Text);

        var messSend = mensajes
            .Where(c => filNum(c.CE_nolada).Contains(filtro))
            .ToList();

        dgHistorialEnvios.ItemsSource = messSend;
    }


    private void BtnVincular_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("¿Estas seguro de cerrar sesion?", "Cerrar Sesion", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            SesionManager.CerrarSesion();

            var loginWindow = new LoginWindow();
            loginWindow.Show();

            Application.Current.MainWindow = loginWindow;
            this.Close();
        }
    }

    private void CerrarSesion()
    {
        SesionManager.CerrarSesion();
        this.Close();
    }

    private void BtnLimpiarMensajesRec(object sender, RoutedEventArgs e)
    {
        var resultado = MessageBox.Show("¿Estas seguro de eliminar la lista de mensajes recibidos?",
                                        "Confirmar eliminacion",
                                        MessageBoxButton.YesNoCancel,
                                        MessageBoxImage.Warning);

        if (resultado == MessageBoxResult.Yes)
        {
            using (var context = new FrontContext())
            {
                context.Recibidos.RemoveRange(context.Recibidos);
                context.SaveChanges();
            }
            CargarMensajesRecibidos();
            MessageBox.Show("Lista limpiada!", "Informacion", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void BtnActualizarEnvios(object sender, RoutedEventArgs e)
    {
        ControlEnvios();
    }
    
    private async void BtnEnviarPlantilla(object sender, RoutedEventArgs e)
    {
        btnEnviarPlantilla.IsEnabled = false;
        txtSending.Visibility = Visibility.Visible;
        pbCargando.Visibility = Visibility.Visible;

        try
        {
            var selecMult = dgContactos.SelectedItems.Cast<FrontContacto>().ToList();

            if (selecMult.Count == 0)
            {
                MessageBox.Show("Selecciona al menos un contacto.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                btnEnviarPlantilla.IsEnabled = true;
                return;
            }


            string plantillaSeleccionada = ((ComboBoxItem)cbTipoPlantilla.SelectedItem)?.Content.ToString();
            string comprobante = ((ComboBoxItem)cbComprobante.SelectedItem)?.Content.ToString();
            string folio = txtFolio.Text.Trim();
            string total = txtTotal.Text.Trim();
            string linkPdf = txtLinkPDF.Text.Trim();
            string urlImageHeader = "https://alexissolisitsch.github.io/KairosApp/eFACT%20LOGO.png";
            string urlImageMark = "https://alexissolisitsch.github.io/KairosApp/gesemcloud.png";
            string urlImageCupon = "https://alexissolisitsch.github.io/KairosApp/ImgMarkCupon.jpg";
            string urlImagePromo = "https://alexissolisitsch.github.io/KairosApp/ImgMarkPromo.jpg";

            if (!Est_Inter())
            {
                MessageBox.Show("Fallo al enviar el mensaje. Verifica tu conexion a Internet", "Sin Conexion", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var config = UrlConfig.Cargar();
            var client = new WhatsAppApiClient(config);

            List<string> exito = new();
            List<string> fallidos = new();

            foreach (var contacto in selecMult)
            {
                string telefono = Regex.Replace(contacto.Telefono.Trim(), @"[^\d]", "");
                if (telefono.Length == 10)
                {
                    telefono = "52" + telefono;
                }

                if (!Regex.IsMatch(telefono, @"^52\d{10}$"))
                {
                    fallidos.Add($"{telefono} (formato invalido)");
                    continue;
                }

                if (telefono.Length != 12)
                {
                    fallidos.Add($"{telefono} (longitud inválida)");
                    continue;
                }

                string nombre = contacto.Nombre;
                string nombreArchivo = $"{comprobante}-{folio}";
                string? messageId = null;
                string estado = "Pendiente";
                string contenido = $"Plantilla {plantillaSeleccionada}";

                if (plantillaSeleccionada == "Marketing")
                {
                    messageId = await client.plantillaMarketing(telefono, urlImageMark);
                }
                else if (plantillaSeleccionada == "Cupon")
                {
                    messageId = await client.plantMarkCupon(telefono, urlImageCupon);
                }
                else if (plantillaSeleccionada == "Promocion")
                {
                    messageId = await client.plantMarkPromo(telefono, urlImagePromo);
                }
                else if (plantillaSeleccionada == "Factura")
                {
                    if (string.IsNullOrEmpty(comprobante) || string.IsNullOrEmpty(folio) || string.IsNullOrEmpty(total) || string.IsNullOrEmpty(linkPdf))
                    {
                        MessageBox.Show("Todos los campos son obligatorios", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        btnEnviarPlantilla.IsEnabled = true;
                        return;
                    }

                    if (!decimal.TryParse(total, out decimal totalDecimal) || totalDecimal <= 0)
                    {
                        MessageBox.Show("El Total debe ser mayor a cero!!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        btnEnviarPlantilla.IsEnabled = true;
                        return;
                    }

                    messageId = await client.EnviarPlantillaFactura2Async(telefono, nombre, folio, comprobante, total, urlImageHeader, linkPdf);
                }

                if (messageId == null)
                {
                    contenido = "MENSAJE NO ENVIADO";
                    estado = "Fallido";
                    fallidos.Add($"{nombre} {telefono}");
                }
                else
                {
                    exito.Add($"{nombre} {telefono}");
                }

                var optionsBuilder = new DbContextOptionsBuilder<BackendContext>();
                optionsBuilder.UseSqlServer(AppDbContext.ConnectionSQL);

                using (var dbContext = new BackendContext(optionsBuilder.Options))
                {
                    var contactoDb = dbContext.Contactos.FirstOrDefault(c => c.Telefono == telefono);
                    if (contactoDb == null)
                    {
                        MessageBox.Show($"El contacto {telefono} no existe en la base de datos", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        continue;
                    }

                    var mensaje = new BackendMensaje
                    {
                        ContactoId = contactoDb.Id,
                        Contenido = contenido,
                        Estado = estado,
                        FechaEnvio = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.Local),
                        NumContacto = telefono,
                        MensajeMetaId = messageId
                    };
                    dbContext.Mensajes.Add(mensaje);
                    dbContext.SaveChanges();
                }
                int delay = 1000;
                using (var db = new FrontContext())
                {
                    var configDelay = db.UrlConfigs.FirstOrDefault();
                    if (configDelay != null && configDelay.confTemp > 0)
                        delay = configDelay.confTemp;
                }
                await Task.Delay(delay);
            }
            StringBuilder resumen = new();
            resumen.AppendLine("Envios Masivos con exito:");
            resumen.AppendLine(exito.Count > 0 ? string.Join("\n", exito) : "Ninguno");
            resumen.AppendLine("\nEnvios Fallidos:");
            resumen.AppendLine(fallidos.Count > 0 ? string.Join("\n", fallidos) : "Ninguno");
            MessageBox.Show(resumen.ToString(), "Resumen del Envio Masivo", MessageBoxButton.OK, MessageBoxImage.Information);
            ControlEnvios();
        }
        finally
        {
            btnEnviarPlantilla.IsEnabled = true;
            txtSending.Visibility = Visibility.Collapsed;
            pbCargando.Visibility = Visibility.Collapsed;
        }
    }

    private async Task InicializarSignalR()
    {
        _HubConnection = new HubConnectionBuilder()
            .WithUrl("https://localhost:7266/hub/notificaciones")
            .WithAutomaticReconnect()
            .Build();

        _HubConnection.On<EstadoActualizadoMessage>("EstadoActualizado", (mensaje) =>
        {
            Dispatcher.Invoke(() =>
            {
                int id = mensaje.Id;
                string estado = mensaje.Estado;
                string numContacto = mensaje.NumContacto;

                var item = ((List<FrontMensaje>)dgHistorialEnvios.ItemsSource)
                    .FirstOrDefault(m => m.Id == id);

                if (item != null)
                {
                    item.Estado = FormatearEstado(estado);
                    dgHistorialEnvios.Items.Refresh();
                }
            });
        });

        try
        {
            await _HubConnection.StartAsync();
            Console.WriteLine("Conectado al Hub de SignalR");
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error al conectar al Hub de SignalR: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RecConnection()
    {
        _conexion = new DispatcherTimer();
        _conexion.Interval = TimeSpan.FromSeconds(5);
        _conexion.Tick += (s, e) =>
        {
            wifiStatus("Verificando Conexion WiFi...");
            bool conectado = Est_Inter();

            if (conectado)
            {
                lblConnection.Content = "Conectado";
                wifiStatus("Conectado");
            }
            else
            {
                lblConnection.Content = "Sin Conexion";
                wifiStatus("Sin Conexion");
            }

                btnEnviarPlantilla.IsEnabled = conectado;
        };
        _conexion.Start();
    }

    private void wifiStatus(string statuscon)
    {
        wifiSearch.Visibility = statuscon == "Verificando Conexion WiFi..." ? Visibility.Visible : Visibility.Collapsed;
        wifiConnected.Visibility = statuscon == "Conectado" ? Visibility.Visible : Visibility.Collapsed;
        wifiError.Visibility = statuscon == "Sin Conexion" ? Visibility.Visible : Visibility.Collapsed;
    }

    private void GuardarContactosDB()
    {
        using (var context = new Models.AppDbContext())
        {
            foreach (var contacto in contactos)
            {
                if (!context.Contactos.Any(c => c.Telefono == contacto.Telefono))
                {
                    context.Contactos.Add(contacto);
                }
            }
            context.SaveChanges();
        }
    }

    private void CargarContactosDB()
    {
        using (var context = new FrontContext())
        {
            contactos = context.Contactos.ToList();
            dgContactos.ItemsSource = contactos;
        }
    } 

    private void ControlEnvios()
    {
        if (dgHistorialEnvios == null)
        {
            Console.WriteLine("dgHistorialEnvios es null");
            return;
        }

        using (var context = new FrontContext())
        {
            mensajes = context.Mensajes
                .OrderByDescending(m => m.FechaEnvio)
                .Where(m => m.FechaEnvio != null && m.Estado != null)
                .ToList()
                .Select(m =>
                {
                    m.Estado = FormatearEstado(m.Estado);
                    return m;
                })
                .ToList();


            string? filtro = cbFiltroEstado.SelectedItem is ComboBoxItem selectedItem ? selectedItem.Content.ToString() : "Todos";

            if (filtro != "Todos")
            {
                mensajes = mensajes.Where(m => m.Estado == filtro).ToList();
            }
            dgHistorialEnvios.ItemsSource = null;
            dgHistorialEnvios.ItemsSource = mensajes;
        }

        searchEnv(null, null);
    }

    private void CbFiltroEstado_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ControlEnvios();
    }

    private void ActualizarEnvios()
    {
        _timer = new DispatcherTimer();
        _timer.Interval = TimeSpan.FromSeconds(5);
        _timer.Tick += (s, e) => ControlEnvios();
        _timer.Start();
    }


    public void CambioEstadoSesion()
    {
        sesionIniciada = true;
        Dispatcher.Invoke(() => BtnVincular.Content = "Cerrar Sesion");
    }

    private async Task CargarMensajesRecibidos()
    {
        try
        {
            var mensajes = await ApiServices.ObtenerMensajeRecibido();
            respuesta = mensajes;
            filRec();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar mensajes recibidos: " + ex.Message);
        }
    }

    private void filRec()
    {
        if (respuesta == null || dgMensajesRecibidos == null)
            return;


        string filtroCombo = cbFiltroMensaje.SelectedItem is ComboBoxItem selectedItem
            ? selectedItem.Content.ToString()
            : "Todos";

        string filtroTexto = filNum(txtBuscarRec.Text);

        var resultado = respuesta;

        if (filtroCombo == "Interesado")
        {
            resultado = resultado
                .Where(m => m.Mensaje != null && (
                            m.Mensaje.Contains("Si") ||
                            m.Mensaje.Contains("me interesa") || 
                            m.Mensaje.Contains("Quiero la promoción") || 
                            m.Mensaje.Contains("Quiero el cupón") ||
                            m.Mensaje.Contains("Quiero el cupon") ||
                            m.Mensaje.Contains("Mas información") ||
                            m.Mensaje.Contains("Lo quiero")))
                .ToList();
        }
        else if (filtroCombo == "No Interesado")
        {
            resultado = resultado
                .Where(m => m.Mensaje != null && (
                            m.Mensaje.Contains("No") || 
                            m.Mensaje.Contains("no gracias") && 
                            !m.Mensaje.Contains("me interesa")))
                .ToList();
        }

        resultado = resultado
            .Where(c =>
                filNum(c.CR_nolada).Contains(filtroTexto) ||
                (!string.IsNullOrEmpty(c.Mensaje) && c.Mensaje.ToLower().Contains(txtBuscarRec.Text.ToLower()))
            )
            .ToList();

        dgMensajesRecibidos.ItemsSource = resultado;
    }

    private void CbFiltroMensaje_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        filRec();
    }

    private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
    {

    }

    private void dgContactos_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (dgContactos.SelectedItem is FrontContacto contacto)
        {
            txtTelefonoDestino.Text = contacto.Telefono;
            txtNombreCliente.Text = contacto.Nombre;
            seleccionartelefono = contacto.Telefono;
        }
    }

    private void cbTipoPlantilla_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (txtFolio == null || txtTotal == null || txtLinkPDF == null || cbComprobante == null)
            return;

        string? seleccion = ((ComboBoxItem)cbTipoPlantilla.SelectedItem)?.Content.ToString();
        bool esFactura = seleccion == "Factura";

        /******************************************************************/

        txtbxComp.Visibility = esFactura ? Visibility.Visible : Visibility.Hidden;
        cbComprobante.Visibility = esFactura ? Visibility.Visible : Visibility.Hidden;

        brComp.Visibility = esFactura ? Visibility.Visible : Visibility.Hidden;
        txtFolio.Visibility = esFactura ? Visibility.Visible : Visibility.Hidden;
        txtbxNoComp.Visibility = esFactura ? Visibility.Visible : Visibility.Hidden;

        txtTotal.Visibility = esFactura ? Visibility.Visible : Visibility.Hidden;
        txtbxTotal.Visibility = esFactura ? Visibility.Visible : Visibility.Hidden;
        brTotal.Visibility = esFactura ? Visibility.Visible : Visibility.Hidden;

        txtLinkPDF.Visibility = esFactura ? Visibility.Visible : Visibility.Hidden;
        txtbxPDF.Visibility = esFactura ? Visibility.Visible : Visibility.Hidden;
        brPDF.Visibility = esFactura ? Visibility.Visible : Visibility.Hidden;

        /****************************************************************/

        cbComprobante.IsEnabled = esFactura;
        txtbxComp.IsEnabled = esFactura;

        brComp.IsEnabled = esFactura;
        txtFolio.IsEnabled = esFactura;
        txtbxComp.IsEnabled = esFactura;

        txtTotal.IsEnabled = esFactura;
        txtbxTotal.IsEnabled = esFactura;
        brTotal.IsEnabled = esFactura;

        txtLinkPDF.IsEnabled = esFactura;
        txtbxPDF.IsEnabled = esFactura;
        brPDF.IsEnabled = esFactura;

        if (!esFactura)
        {
            txtFolio.Text = "";
            txtTotal.Text = "";
            txtLinkPDF.Text = "";
            cbComprobante.SelectedIndex = 0;
        }
    }

    private void BtnConfigWin_Click(object sender, RoutedEventArgs e)
    {
        var configWindow = new ConfigWindow();
        configWindow.ShowDialog();
    }

    //Comportamientos de la ventana
    [DllImport("user32.dll")]
    public static extern IntPtr SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);

    private void pnlControlMouse(object sender, MouseButtonEventArgs e)
    {
        WindowInteropHelper helper = new WindowInteropHelper(this);
        SendMessage(helper.Handle, 161, 2, 0);
    }

    private void btnClose_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void btnMaximize_Click(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Normal)
        {
            WindowState = WindowState.Maximized;
        }
        else
        {
            WindowState = WindowState.Normal;
        }
    }

    private void btnMinimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void txtBuscarEnv_TextChanged(object sender, TextChangedEventArgs e)
    {

    }
    private void txtBuscarRec_TextChanged(object sender, TextChangedEventArgs e)
    {

    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await CargarMensajesRecibidos();
    }
}