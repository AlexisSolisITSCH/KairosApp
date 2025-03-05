using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KairosApp.Models
{
    public class Recibidos
    {
        public int Id { get; set; }
        public DateTime FechaRecibido { get; set; } = DateTime.Now;
        public string NumRemitente { get; set; }
        public string Mensaje { get; set; }
    }
}
