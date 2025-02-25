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
        public string Contenido { get; set; }
        public string Estado { get; set; } = "Pendiente";
        public DateTime FechaEnvio { get; set; } = DateTime.Now;
    }
}
