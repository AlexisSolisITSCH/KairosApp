namespace KairosAPI.Models
{
    public class Mensaje
    {
        public int Id { get; set; } // Primary Key
        public int ContactoId { get; set; }
        public string Contenido { get; set; }
        public string Estado { get; set; }
        public DateTime FechaEnvio { get; set; }
        public string NumContacto { get; set; }
        public string MensajeMetaId { get; set; }
    }
}
