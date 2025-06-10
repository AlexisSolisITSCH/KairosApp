using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KairosApp.Models
{
    public class Mensaje
    {
        public int Id { get; set; }
        public int ContactoId { get; set; }
        public string NumContacto { get; set; }
        public string Contenido { get; set; }
        public string Estado { get; set; } = "Pendiente";
        public DateTime FechaEnvio { get; set; } 
        public string? MensajeMetaId { get; set; }

        public string CE_nolada
        {
            get
            {
                if (NumContacto != null && NumContacto.StartsWith("52") && NumContacto.Length == 12)
                {
                    string numero = NumContacto.Substring(2);
                    return $"({numero.Substring(0, 3)}) {numero.Substring(3, 3)}-{numero.Substring(6, 4)}";
                }
                return NumContacto;
            }
        }
    }
}
