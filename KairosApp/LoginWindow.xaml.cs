using KairosApp.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using AppDbContext = KairosApp.Models.AppDbContext;

namespace KairosApp
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private async void btnRegisterClick(object sender, RoutedEventArgs e)
        {
            string nombre = txtNombreR.Text.Trim();
            string wabaid = txtWabaid.Text.Trim();

            if (string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(wabaid))
            {
                MessageBox.Show("Porfavor, completa todos lo campos necesarios.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (cbPhoneNumers.SelectedItem is not NumeroMeta numeroSeleccionado)
            {
                MessageBox.Show("Debes seleccionar un numero valido de la lista", "Campo Requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string telefono = numeroSeleccionado.Numero;
            string phoneIdSelect = numeroSeleccionado.Id;
            
            using (SqlConnection connection = new SqlConnection(AppDbContext.ConnectionSQL))
            {
                try
                {
                    connection.Open();
                    string checkQuery = "SELECT COUNT(*) FROM loginuser WHERE numcel = @telefono OR wabaid = @wabaid";
                    using (SqlCommand checkcmd = new SqlCommand(checkQuery, connection))
                    {
                        checkcmd.Parameters.AddWithValue("@telefono", telefono);
                        checkcmd.Parameters.AddWithValue("@wabaid", wabaid);
                        int count = (int)checkcmd.ExecuteScalar();

                        if (count > 0)
                        {
                            MessageBox.Show("Este numero ya esta registrado.", "Numero ya existente", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            return;
                        }
                    }

                    string insertQuery = "INSERT INTO loginuser (nomb, numcel, phoneid, wabaid) VALUES (@nombre, @telefono, @phoneid, @wabaid)";
                    using (SqlCommand insertcmd = new SqlCommand(insertQuery, connection))
                    {
                        insertcmd.Parameters.AddWithValue("@nombre", nombre);
                        insertcmd.Parameters.AddWithValue("@telefono", telefono);
                        insertcmd.Parameters.AddWithValue("@wabaid", wabaid);
                        insertcmd.Parameters.AddWithValue("@phoneid", phoneIdSelect);

                        insertcmd.ExecuteNonQuery();

                        MessageBox.Show("Usuario Registrado. Ya puedes iniciar sesion!", "Exito", MessageBoxButton.OK, MessageBoxImage.Information);
                        LabelLogin(null, null);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al conectar con la Base de datos.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnLoginClick(object sender, RoutedEventArgs e)
        {
            string nombre = txtNombreL.Text.Trim();
            string telefono = txtNumL.Text.Trim();

            if (string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(telefono))
            {
                MessageBox.Show("Por favor, completa todos los campos", "Campos sin rellenar", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (SqlConnection connection = new SqlConnection(AppDbContext.ConnectionSQL))
            {
                try
                {
                    connection.Open();
                    string query = "SELECT id, nomb, numcel, phoneid, wabaid FROM loginuser WHERE nomb = @nombre AND numcel = @telefono";

                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@nombre", nombre);
                        cmd.Parameters.AddWithValue("@telefono", telefono);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                LoginUser.Id = reader.GetInt32(0);
                                LoginUser.nomb = reader.GetString(1);
                                LoginUser.numcel = reader.GetString(2);
                                LoginUser.phoneid = reader.GetString(3);
                                LoginUser.wabaid = reader.GetString(4);

                                SesionManager.GuardarSesion();
                                MainWindow main = new MainWindow();
                                main.Show();
                                this.Close();
                            }
                            else
                            {
                                MessageBox.Show("Usuario no encontrado. Verifica tus datos", "Error de Inicio de Sesion", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }
                catch(Exception ex)
                {
                    MessageBox.Show("Error al conectar con la Base de datos. " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void btnBuscarNum(object sender, RoutedEventArgs e)
        {
            string wabaid = txtWabaid.Text.Trim();
            string token;
            txtValidarId.Visibility = Visibility.Visible;

            try
            {
                var config = UrlConfig.Cargar();
                token = config.UserAccessToken;
            }
            catch
            {
                MessageBox.Show("No hay configuracion guardada. Ingresa el token manualmente o configura desde el apartado de configuracion");
                return;
            }

            if (string.IsNullOrEmpty(wabaid))
            {
                MessageBox.Show("Ingresa un WABA ID valido.", "Error de Validacion", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            using var httpClient = new HttpClient();

            var configWa = UrlConfig.Cargar();
            token = configWa.UserAccessToken;
            string version = configWa.Version;

            string url = $"https://graph.facebook.com/{version}/{wabaid}/phone_numbers?access_token={token}";

            try
            {
                var response = await httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show("No se pudo obtener los numeros desde Meta. Revisa tu WABA ID o tu Token sea valido", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var phoneNumbers = doc.RootElement.GetProperty("data");
                if (phoneNumbers.GetArrayLength() == 0)
                {
                    MessageBox.Show("No hay numeros registrados con este WABA_ID", "Sin Numeros", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var listNumbers = new List<NumeroMeta>();
                foreach (var numero in phoneNumbers.EnumerateArray())
                {
                    listNumbers.Add(new NumeroMeta
                    {
                        Id = numero.GetProperty("id").GetString(),
                        Numero = numero.GetProperty("display_phone_number").GetString()
                    });
                }
                cbPhoneNumers.ItemsSource = listNumbers;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al consular Meta: ", "Error de Red", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            txtValidarId.Visibility = Visibility.Collapsed;
            return;
        }

        private void LabelRegistro(object sender, MouseButtonEventArgs e)
        {
            LoginPanel.Visibility = Visibility.Collapsed;
            RegistroPanel.Visibility = Visibility.Visible;
        }

        private void LabelLogin(object sender, MouseButtonEventArgs e)
        {
            RegistroPanel.Visibility = Visibility.Collapsed;
            LoginPanel.Visibility = Visibility.Visible;
        }

        public class NumeroMeta
        {
            public string Id { get; set; }
            public string Numero { get; set; }
        }

        private void cbPhoneNumbers_Select(object sender, SelectionChangedEventArgs e)
        {
            if (cbPhoneNumers.SelectedItem is NumeroMeta seleccionado)
            {
                txtTelefonoR.Text = seleccionado.Numero;
            }
            else
            {
                txtTelefonoR.Text = "";
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void btnMinimizar_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void btnCerrar_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void BtnConfigWin_Click(object sender, RoutedEventArgs e)
        {
            var configWindow = new ConfigWindow();
            configWindow.ShowDialog();
        }
    }
}
