using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KairosApp.Models
{
    [Table("UrlConfig")]
    public class UrlConfig
    {
        public int Id { get; set; }
        public string TokenAcceso { get; set; }
        public string Version { get; set; }
        public int confTemp { get; set; }

        public string ApiUrl => "https://graph.facebook.com";

        public string UserAccessToken => TokenAcceso;

        public static UrlConfig Cargar()
        {
            using var db = new AppDbContext();
            var config = db.UrlConfigs.FirstOrDefault();
            if (config == null)
                throw new Exception("No hay configuración registrada.");

            return config;
        }

        public static void Guardar(string nuevoToken, string nuevaVersion, int delay)
        {
            using var db = new AppDbContext();
            var config = db.UrlConfigs.FirstOrDefault();
            if (config != null)
            {
                config.TokenAcceso = nuevoToken;
                config.Version = nuevaVersion;
                config.confTemp = delay;
            }
            else
            {
                config = new UrlConfig
                {
                    TokenAcceso = nuevoToken,
                    Version = nuevaVersion,
                    confTemp = delay
                };
                db.UrlConfigs.Add(config);
            }
            db.SaveChanges();
        }
    }
}
