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
        public DbSet<Contacto> Contactos { get; set; }
        public DbSet<Mensaje> Mensajes { get; set; }
        public DbSet<Recibidos> Recibidos { get; set; }
        public DbSet<UrlConfig> UrlConfigs { get; set; }
        public static string ConnectionSQL { get; set; } =
            "Data Source=.\\SQLEXPRESS2005;Initial Catalog=eFactDCS_CFDi;Persist Security Info=True;User ID=sa;Password=dcs2010;Connection Timeout=0;MultipleActiveResultSets=True";

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(ConnectionSQL);
            }
        }


    }
}
