namespace KairosAPI.Models
{
    public class Recibido
    {
        public int Id { get; set; }
        public DateTime FechaRecibido { get; set; }
        public string NumRemitente { get; set; }
        public string Mensaje { get; set; }

    }
}
