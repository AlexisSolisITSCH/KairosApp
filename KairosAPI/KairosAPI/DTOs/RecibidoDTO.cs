namespace KairosAPI.DTOs
{
    public class RecibidoDTO
    {
        public string NumRemitente { get; set; }
        public string Mensaje { get; set; }
        public DateTime FechaRecibido { get; set; }

        public string CR_nolada
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(NumRemitente))
                {
                    string telefono = NumRemitente;

                    if (telefono.StartsWith("521") && telefono.Length == 13)
                    {
                        telefono = telefono.Substring(3);
                    }else if (telefono.StartsWith("52") && telefono.Length == 12)
                    {
                        telefono = telefono.Substring(2);
                    }

                    if (telefono.Length == 10)
                    {
                        return $"({telefono.Substring(0, 3)}) {telefono.Substring(3, 3)} {telefono.Substring(6, 4)}";
                    }
                    return telefono;
                }
                return NumRemitente;
            }
        }
    }
}
