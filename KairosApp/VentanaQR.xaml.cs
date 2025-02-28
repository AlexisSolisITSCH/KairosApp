using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace KairosApp
{
    /// <summary>
    /// Lógica de interacción para VentanaQR.xaml
    /// </summary>
    public partial class VentanaQR : Window
    {
        private MainWindow _mainWindow;
        public VentanaQR(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            CargarCodigoQRPrueba();
            SimulacionEsc();
        }

        private void CargarCodigoQRPrueba()
        {
            string qrUrl = "https://api.qrserver.com/v1/create-qr-code/?size=250x250&data=https://example.com";
            imgQR.Source = new BitmapImage(new Uri(qrUrl));
        }

        private void SimulacionEsc()
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(5); 
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                _mainWindow.CambioEstadoSesion(); 
                this.Hide();
            };
            timer.Start();
        }
    }
}
