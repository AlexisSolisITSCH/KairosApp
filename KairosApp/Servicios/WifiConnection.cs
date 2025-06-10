using KairosApp.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using FrontContext = KairosApp.Models.AppDbContext;

namespace KairosApp.Servicios
{
    public static class WifiConnection
    {
        public static void Fallo(
            string telefono,
            string nombre,
            string folio,
            string comprobante,
            string total,
            string Fallo = "Desconocido",
            string? codigoError = null,
            string? mensajeMetaId = null)
        {
             Console.WriteLine($"Fallo de conexión: {telefono}, {nombre}, {folio}, {comprobante}, {total}, {Fallo}, {codigoError}, {mensajeMetaId}");
        }
    }
}
