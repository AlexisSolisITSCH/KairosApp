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
using OfficeOpenXml;
using Microsoft.Win32;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using RestSharp;
using Newtonsoft.Json.Linq;
using KairosApp.Models;
using System.Windows.Threading;

namespace KairosApp;

public partial class MainWindow : Window
{
    // Variables
    private List<Contacto> contactos = new List<Contacto> ();
    private List<Recibidos> respuesta = new List<Recibidos>();  
    private string archivoAdjunto = "";
    private bool sesionIniciada = false;

    //API Whatsapp
    /*
    private const string TokenAcceso 
        = "EAAQt1G61oFwBOZBH6gELpQk4Kv6F4cZCbNfnAMHvZAGmiB6FHYdB1ZBesT2kBanxxU6ZCZBg6m5CBBUTmW3WfHSB09onGRcQyAZC7YfYuWVmGOOmYZB2xjtfhaJjngCDSqaqMfinyiVbWmAEmxEORNc2yjQT0g9RofTsaDSJ0m7G5jmDKZC2c4PYU9IfxOzKENZBInCRykaaMUihkSjZB5zxrtMMtcLsOEZD";
    private const string IdTelefonoMeta = "617710981414886";
    private const string UrlWhatsAppApi = "https://graph.facebook.com/v21.0/";
    */
    public MainWindow()
    {
        InitializeComponent();
        CargarContactosDB();
        CargarMensajesRecibidos();
    }

    private void BtnImportar_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog dialogo = new OpenFileDialog
        {
            Filter = "Archivos de Excel (*.xlsx)|*.xlsx",
            Title = "Seleccionar archivo de Excel"
        };

        if(dialogo.ShowDialog() == true)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(dialogo.FileName);
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using(ExcelPackage package = new ExcelPackage(fileInfo))
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[0];

                    if(worksheet.Dimension == null)
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

                    List<Contacto> nuevosContactos = new List<Contacto>();

                    for(int row = 2; row < rowCount; row++)
                    {
                        string nombre = worksheet.Cells[row, 1].Text;
                        string telefono = worksheet.Cells[row, 2].Text;

                        if (!string.IsNullOrWhiteSpace(nombre) && !string.IsNullOrWhiteSpace(telefono))
                        {
                            if (!long.TryParse(telefono, out _))
                            {
                                MessageBox.Show($"Error en la fila {row}: El teléfono debe contener solo números.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
                            contactos.Add(new Contacto { Nombre = nombre, Telefono = telefono });

                            if (!contactos.Any(c => c.Telefono == telefono))
                            {
                                nuevosContactos.Add(new Contacto { Nombre = nombre, Telefono = telefono });
                            }
                        }
                    }

                    contactos.AddRange(nuevosContactos);
                    dgContactos.ItemsSource = null;
                    dgContactos.ItemsSource = contactos;

                    GuardarContactosDB();
                    MessageBox.Show("Contactos importados!!", "Exito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }catch(IOException)
            {
                MessageBox.Show("El archivo Excel esta abierto. Cierralo e intenta nuevamente", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch(System.Exception ex)
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

    private void TxtBuscar_Up(object sender, System.Windows.Input.KeyEventArgs e)
    {
        string filtro = txtBuscar.Text.ToLower();

        var contactosFiltrados = contactos
            .Where(c =>  c.Nombre.ToLower().Contains(filtro) || c.Telefono.Contains(filtro))
            .ToList();

        dgContactos.ItemsSource = contactosFiltrados;
    }

    private void BtnAdjuntar_Click(object sender, RoutedEventArgs e) 
    {
        OpenFileDialog dialogo = new OpenFileDialog
        {
            Filter = "Todos los archivos|*.*|Imágenes|*.jpg;*.jpeg;*.png|PDF|*.pdf",
            Title = "Seleccionar archivo adjunto"
        };

        if (dialogo.ShowDialog() == true)
        {
            archivoAdjunto = dialogo.FileName;
            ArchAdjunto.Text = $"Archivo(s) Seleccionado(s): {System.IO.Path.GetFileName(archivoAdjunto)}";
        }
    }

    private void BtnEnviar_Click(object sender, RoutedEventArgs e)
    {
        if (dgContactos.SelectedItem is Contacto contactoSeleccionado)
        {
            if (string.IsNullOrWhiteSpace(txtMensaje.Text))
            {
                MessageBox.Show("El Mensaje no puede estar vacio", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (var context = new AppDbContext())
            {
                var nuevoMensaje = new Mensaje
                {
                    ContactoId = contactoSeleccionado.Id,
                    Contenido = txtMensaje.Text,
                    ArchivoAdjunto = string.IsNullOrEmpty(archivoAdjunto) ? null : archivoAdjunto,
                    Estado = "Pendiente"
                };

                context.Mensajes.Add(nuevoMensaje);
                context.SaveChanges();

                MessageBox.Show("Mensaje guardado correctamente.", "Exito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            //EnviarMsgWhatsapp(contactoSeleccionado.Telefono, txtMensaje.Text);

            txtMensaje.Text = "";
            archivoAdjunto = "";
            ArchAdjunto.Text = "Ningun archivo adjunto";
        }
        else
        {
            MessageBox.Show("Selecciona un contacto a enviar el mensaje!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void BtnVincular_Click(object sender, RoutedEventArgs e)
    {
        if (!sesionIniciada)
        {
            VentanaQR ventanaQR = new VentanaQR(this);
            ventanaQR.ShowDialog();
        }
        else
        {
            CerrarSesion();
        }
    }

    public void BtnSimularRespuesta_Click(object sender, RoutedEventArgs e)
    {
        using (var context = new AppDbContext())
        {
            var respuesta = new Recibidos{
                NumRemitente = "1234567890",
                Mensaje = "Hola, soy un mensaje de prueba",
                FechaRecibido = DateTime.Now
            };

            context.Recibidos.Add(respuesta);
            context.SaveChanges();

            MessageBox.Show("Mensaje de prueba guardado correctamente.", "Exito", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        CargarMensajesRecibidos();
    }

    private void BtnLimpiarMensajesRec(object sender, RoutedEventArgs e)
    {
        var resultado = MessageBox.Show("¿Estas seguro de eliminar la lista de mensajes recibidos?",
                                        "Confirmar eliminacion",
                                        MessageBoxButton.YesNo,
                                        MessageBoxImage.Warning);

        if (resultado == MessageBoxResult.Yes)
        {
            using(var context = new AppDbContext())
            {
                context.Recibidos.RemoveRange(context.Recibidos);
                context.SaveChanges();
            }
            CargarMensajesRecibidos();
            MessageBox.Show("Lista limpiada!", "Informacion", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    /*Enviar mensaje API Whatsapp
    private void EnviarMsgWhatsapp(string telefonoDestino, string mensaje)
    {
        var client = new RestClient();
        var request = new RestRequest($"{UrlWhatsAppApi}{IdTelefonoMeta}/messages", Method.Post);

        request.AddHeader("Authorization", $"Bearer {TokenAcceso}");
        request.AddHeader("Content-Type", "application/json");

        var body = new
        {
            messaging_product = "whatsapp",
            to = telefonoDestino,
            type = "text",
            text = new { body = mensaje }
        };

        request.AddJsonBody(body);

        var response = client.Execute(request);

        if (response.IsSuccessful)
        {
            MessageBox.Show("Mensaje enviado con éxito.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show($"Error al enviar mensaje: {response.Content}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
    */
    private void GuardarContactosDB()
    {
        using (var context = new AppDbContext())//->Funciona para abrir a la conexion de la base de datos
        {
            // El contacto se guardara si no existe en DB
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
        using (var context = new AppDbContext())
        {
            contactos = context.Contactos.ToList();
            dgContactos.ItemsSource = contactos;
        }
    } 

    // Metodos de prueba
    public void CambioEstadoSesion()
    {
        sesionIniciada = true;
        Dispatcher.Invoke(() => BtnVincular.Content = "Cerrar Sesion");
    }

    private void CerrarSesion()
    {
        sesionIniciada = false;
        Dispatcher.Invoke(() => BtnVincular.Content = "Iniciar Sesión");
        MessageBox.Show("Sesión cerrada correctamente.", "Información", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void CargarMensajesRecibidos()
    {
        using (var context = new AppDbContext())
        {
            var mensajes = context.Recibidos.ToList();
            dgMensajesRecibidos.ItemsSource = mensajes;
        }
    }
}