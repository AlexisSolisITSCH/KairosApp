using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace KairosApp.Models
{
    public class AppDbContext : DbContext
    {
        //Son las tablas de KairosDB
        public DbSet<Contacto> Contactos { get; set; }
        public DbSet<Mensaje> Mensajes { get; set; }
        public DbSet<Recibidos> Recibidos { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=MILANESO\\SQLEXPRESS;Database=EnviosMasivosDB;Trusted_Connection=True;TrustServerCertificate=True;");
            }
        }
    }
}
