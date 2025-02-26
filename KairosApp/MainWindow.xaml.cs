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
using System.Linq;
using KairosApp.Models;

namespace KairosApp;

public partial class MainWindow : Window
{
    private List<Contacto> contactos = new List<Contacto> ();

    public MainWindow()
    {
        InitializeComponent();
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

                    contactos.Clear();

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
                        }
                    }

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
            MessageBox.Show("Contactos Guardados de la Base de Datos.", "Exito", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}