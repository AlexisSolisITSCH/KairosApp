using KairosAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace KairosAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        
        public DbSet<Mensaje> Mensajes { get; set; }
        public DbSet<Recibido> Recibidos { get; set; }
        public DbSet<Contacto> Contactos { get; set; }
        public static string ConnectionSQL { get; set; } =
            "Data Source=.\\SQLEXPRESS2005;Initial Catalog=eFactDCS_CFDi;Persist Security Info=True;User ID=sa;Password=dcs2010;Connection Timeout=0;MultipleActiveResultSets=True";
    }
}
