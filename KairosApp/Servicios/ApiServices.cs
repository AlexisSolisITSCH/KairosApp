using KairosAPI.DTOs;
using KairosAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace KairosApp.Servicios
{
    public static class ApiServices
    {
        public static async Task<List<RecibidoDTO>> ObtenerMensajeRecibido()
        {
            using var httpClient = new HttpClient();
            string url = "https://localhost:7266/api/recibidos";
            return await httpClient.GetFromJsonAsync<List<RecibidoDTO>>(url);
        }
    }
}
