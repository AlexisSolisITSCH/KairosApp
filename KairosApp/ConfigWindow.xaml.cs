using KairosApp.Models;
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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace KairosApp
{
    public partial class ConfigWindow : Window
    {
        private bool ventanaCargada = false;
        public ConfigWindow()
        {
            InitializeComponent();
            Loaded += (s, e) => ventanaCargada = true;
            btnGuardar.IsEnabled = false;
        }

        private void Windows_MousDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
        private async void Guardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;
                var storyboard = (Storyboard)LoadingSpinner.Resources["RotateStoryboard"];
                storyboard.Begin();

                await Task.Delay(500);
                string token = txtAccessToken.Text.Trim();
                bool tokenValido = await ValidToken(token);
                if (!tokenValido)
                {
                    storyboard.Stop();
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                    MessageBox.Show("El token de acceso no es válido. Por favor, verifica el valor del token"
                        , "Token Inválido"
                        , MessageBoxButton.OK
                        , MessageBoxImage.Warning);
                    return;
                }

                using var db = new AppDbContext();
                db.Database.EnsureCreated();

                var configExistente = db.UrlConfigs.FirstOrDefault();

                int delay = 1000;
                if (cmbTemp.SelectedItem is ComboBoxItem item &&
                    int.TryParse(item.Tag?.ToString(), out int parsedDelay))
                {
                    delay = parsedDelay;
                }

                if (configExistente != null)
                {
                    configExistente.TokenAcceso = txtAccessToken.Text;
                    configExistente.Version = ((ComboBoxItem)cmbVersion.SelectedItem).Content.ToString();
                    configExistente.confTemp = delay;
                }
                else
                {
                    var nuevaConfig = new UrlConfig
                    {
                        TokenAcceso = txtAccessToken.Text,
                        Version = ((ComboBoxItem)cmbVersion.SelectedItem).Content.ToString(),
                    };
                    db.UrlConfigs.Add(nuevaConfig);
                }

                db.SaveChanges();

                storyboard.Stop();
                LoadingOverlay.Visibility = Visibility.Collapsed;

                MessageBox.Show("Configuración guardada correctamente", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                var storyboard = (Storyboard)LoadingSpinner.Resources["RotateStoryboard"];
                storyboard.Stop();
                LoadingOverlay.Visibility = Visibility.Collapsed;

                MessageBox.Show("Error al guardar:" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private bool ValidarCampo(TextBox campo)
        {
            DependencyObject parent = campo;
            while (parent != null && !(parent is Border))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            var border = parent as Border;

            if (string.IsNullOrWhiteSpace(campo.Text))
            {
                if (border != null)
                    border.BorderBrush = Brushes.Red;

                return false;
            }
            else
            {
                if (border != null)
                    border.BorderBrush = Brushes.Gray;

                return true;
            }
        }

        private void ValidarCampos(object sender, RoutedEventArgs e)
        {
            if (!ventanaCargada) return;
            
            bool tokenOk = ValidarCampo(txtAccessToken);
            bool versionOk = cmbVersion.SelectedItem is ComboBoxItem;
            bool tempOk = cmbTemp.SelectedItem is ComboBoxItem;

            cmbVersion.BorderBrush = versionOk ? Brushes.Gray : Brushes.Red;
            cmbVersion.Background = versionOk ? Brushes.Transparent : new SolidColorBrush(Color.FromRgb(255, 240, 240));

            cmbTemp.BorderBrush = tempOk ? Brushes.Gray : Brushes.Red;
            cmbTemp.Background = tempOk ? Brushes.Transparent : new SolidColorBrush(Color.FromRgb(255, 240, 240));

            btnGuardar.IsEnabled = tokenOk && tempOk && versionOk;
        }

        private async Task<bool> ValidToken(string token)
        {
            try
            {
                string url = $"https://graph.facebook.com/debug_token?input_token={token}&access_token={token}";

                using var client = new HttpClient();
                var response = await client.GetAsync(url);
                string json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var data = root.GetProperty("data");
                bool isValid = data.GetProperty("is_valid").GetBoolean();
                return isValid;
            }
            catch
            {
                return false;
            }
        }

        private void TxtPhoneId_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !EsNumero(e.Text);
        }

        private bool EsNumero(string texto)
        {
            return texto.All(char.IsDigit);
        }

        private void btnCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var config = UrlConfig.Cargar();

                txtAccessToken.Text = config.TokenAcceso;
                txtUrl.Text = config.ApiUrl;
                txtPhoneId.Text = LoginUser.phoneid;
                cmbVersion.SelectedItem = cmbVersion.Items
                    .OfType<ComboBoxItem>()
                    .FirstOrDefault(item => item.Content.ToString() == config.Version);
                cmbTemp.SelectedItem = cmbTemp.Items
                    .OfType<ComboBoxItem>()
                    .FirstOrDefault(i => 
                    int.TryParse(i.Tag?.ToString(),out int tagValue) && tagValue == config.confTemp);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar configuración: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}
