using KairosApp.Models;
using System.Configuration;
using System.Data;
using System.Windows;

namespace KairosApp
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            SesionManager.CargarSesion();

            if (!string.IsNullOrEmpty(LoginUser.nomb))
            {
                new MainWindow().Show();
            }
            else
            {
                new LoginWindow().Show();
            }
        }
    }
}