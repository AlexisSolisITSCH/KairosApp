using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KairosApp.Models
{
    public class Contacto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Telefono { get; set; }

        public string nolada
        {
            get
            {
                if (Telefono != null && Telefono.StartsWith("52") && Telefono.Length == 12)
                {
                    string numero = Telefono.Substring(2);
                    return $"({numero.Substring(0, 3)}) {numero.Substring(3, 3)}-{numero.Substring(6, 4)}";
                }
                return Telefono;
            }
        }
    }
}
