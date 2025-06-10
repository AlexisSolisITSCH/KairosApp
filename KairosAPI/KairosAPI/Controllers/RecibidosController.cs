using KairosAPI.Data;
using KairosAPI.DTOs;
using KairosAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;

namespace KairosAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecibidosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RecibidosController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Recibido>>> GetRecibidos()
        {
            var lista = await _context.Recibidos
                .OrderByDescending(r => r.FechaRecibido)
                .Select(r => new RecibidoDTO{
                    NumRemitente = r.NumRemitente,
                    Mensaje = r.Mensaje,
                    FechaRecibido = r.FechaRecibido
                })
                .ToListAsync();
            return Ok(lista);
        }
    }
}
