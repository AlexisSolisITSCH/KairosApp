using KairosAPI.Data;
using KairosAPI.Hubs;
using KairosAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace KairosAPI.Controllers
{
    [ApiController]
    [Route("webhook/whatsapp")]
    public class WebhookController : ControllerBase
    {
        private readonly string VERIFY_TOKEN = "kairos_token_2025";
        private readonly AppDbContext _context;
        private readonly IHubContext<NotificacionesHub> _hub;
        private readonly ILogger<WebhookController> _logger;

        public WebhookController(AppDbContext context, IHubContext<NotificacionesHub> hub, ILogger<WebhookController> logger)
        {
            _context = context;
            _hub = hub;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Verify(
            [FromQuery(Name = "hub.mode")] string mode,
            [FromQuery(Name = "hub.challenge")] string challenge,
            [FromQuery(Name = "hub.verify_token")] string verifyToken)
        {
            if (mode == "subscribe" && verifyToken == VERIFY_TOKEN)
            {
                return Ok(challenge);
            }
            return Unauthorized();
        }

        [HttpPost]
        public async Task<IActionResult> Receive([FromBody] JObject body)
        {
            var entries = body["entry"];
            if (entries != null)
            {
                foreach (var entry in entries)
                {
                    var changes = entry["changes"];
                    foreach (var change in changes)
                    {
                        var value = change["value"];
                        var statuses = value?["statuses"];
                        if (statuses != null)
                        {
                            foreach (var status in statuses)
                            {
                                string? messageId = status?["id"]?.ToString()?.Trim();
                                string? statusValue = status?["status"]?.ToString()?.Trim();
                                string? timestamp = status?["timestamp"]?.ToString();

                                if (!string.IsNullOrEmpty(messageId))
                                {
                                    var mensaje = _context.Mensajes.FirstOrDefault(m => m.MensajeMetaId == messageId);
                                    if (mensaje != null)
                                    {
                                        Console.WriteLine($"[STATUS] Mensaje ID: {messageId}, Estado Anterior: {mensaje.Estado}");
                                        Console.WriteLine($"[STATUS] Actualizando Estado a: {statusValue}");

                                        mensaje.Estado = statusValue;
                                        if (long.TryParse(timestamp, out long unixTime))
                                        {
                                            mensaje.FechaEnvio = TimeZoneInfo.ConvertTimeFromUtc(
                                                    DateTimeOffset.FromUnixTimeSeconds(unixTime).UtcDateTime,
                                                    TimeZoneInfo.Local);

                                        }
                                        await _context.SaveChangesAsync();
                                        Console.WriteLine($"[STATUS] Estado en: {mensaje.Estado}");
                                        await _hub.Clients.All.SendAsync("EstadoActualizado", new
                                        {
                                            mensaje.Id,
                                            mensaje.Estado,
                                            mensaje.NumContacto
                                        });
                                    }
                                    else
                                    {
                                       Console.WriteLine($"Mensaje no encontrado con ID: " + messageId);
                                    }
                                }
                            }
                        }

                        // Proceso de Mensajes recibidos
                        var messages = value?["messages"];
                        if (messages != null)
                        {
                            foreach (var msg in messages)
                            {
                                string? waId = value["contacts"]?[0]?["wa_id"]?.ToString()?.Trim();
                                string? timestamp = msg?["timestamp"]?.ToString();
                                string? tipo = msg?["type"].ToString();
                                string mensajeTexto = "";

                                if (tipo == "text")
                                {
                                    mensajeTexto = msg["text"]?["body"]?.ToString() ?? "";
                                }
                                else if (tipo == "sticker")
                                {
                                    mensajeTexto = "Sticker";
                                }
                                else if (tipo == "image")
                                {
                                    mensajeTexto = "Imagen";
                                }
                                else if (tipo == "audio")
                                {
                                    mensajeTexto = "Audio";
                                }
                                else if (tipo == "button")
                                {
                                    string respuesta = msg["button"]?["text"]?.ToString() ?? "";
                                    mensajeTexto = $"{respuesta}";
                                }

                                if (!string.IsNullOrEmpty(mensajeTexto) && !string.IsNullOrEmpty(waId))
                                {
                                    Console.WriteLine($"[RECIBIDO] Mensaje Recibido de {waId}: {mensajeTexto}");
                                    var recibido = new Recibido
                                    {
                                        NumRemitente = waId,
                                        Mensaje = mensajeTexto,
                                        FechaRecibido = long.TryParse(timestamp, out long unixTime)
                                            ? TimeZoneInfo.ConvertTimeFromUtc(
                                                DateTimeOffset.FromUnixTimeSeconds(unixTime).UtcDateTime,
                                                TimeZoneInfo.Local)
                                            : DateTime.Now
                                    };
                                    _context.Recibidos.Add(recibido);
                                    _context.SaveChanges();

                                    string? mrId = msg["context"]?["id"]?.ToString()?.Trim();
                                    Mensaje? mensajeOriginal = null;


                                    if (!string.IsNullOrEmpty(mrId))
                                    {
                                        Console.WriteLine($"[RECIBIDO] Context ID detectado: {mrId}");
                                        mensajeOriginal = _context.Mensajes
                                            .FirstOrDefault(m => !string.IsNullOrEmpty(m.MensajeMetaId)
                                            && m.MensajeMetaId.Trim() == mrId.Trim());
                                        if (mensajeOriginal == null)
                                        {
                                            Console.WriteLine($"[WEBHOOK] No se encontró mensaje con MensajeMetaId: {mrId}");
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("[RECIBIDO] El mensaje recibido no contiene context.id");
                                        Console.WriteLine($"[WEBHOOK] Buscando por numero de contacto: {waId}");

                                        mensajeOriginal = _context.Mensajes
                                            .Where(m => m.NumContacto == waId)
                                            .OrderByDescending(m => m.FechaEnvio)
                                            .FirstOrDefault();
                                    }

                                    if (mensajeOriginal != null)
                                    {
                                        Console.WriteLine($"[RECIBIDO] Mensaje Encontrado: ID: {mensajeOriginal.Id}, Estado Actual: {mensajeOriginal.Estado}");
                                        mensajeOriginal.Estado = "read";
                                        mensajeOriginal.FechaEnvio = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time"));

                                        _context.Mensajes.Update(mensajeOriginal);
                                        await _context.SaveChangesAsync();

                                        await _hub.Clients.All.SendAsync("EstadoActualizado", new
                                        {
                                            mensajeOriginal.Id,
                                            mensajeOriginal.Estado,
                                            mensajeOriginal.NumContacto,
                                        });
                                        Console.WriteLine($"[RECIBIDO] Estado Actualizado a 'read' y notificado");
                                    }
                                    else
                                    {
                                        Console.WriteLine($"[RECIBIDO] No se encontro el mensaje original a actualizar para: {waId}");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return Ok();
        }
    }
}
