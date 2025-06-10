using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using KairosApp.Models;

namespace KairosApp
{
    internal class SesionManager
    {
        private static readonly string ruta = "sesion.json";

        public static void GuardarSesion()
        {
            var datos = new
            {
                LoginUser.Id,
                LoginUser.nomb,
                LoginUser.numcel,
                LoginUser.phoneid
            };
            var json = JsonSerializer.Serialize(datos);
            File.WriteAllText(ruta, json);
        }

        public static void CargarSesion()
        {
            if (File.Exists(ruta))
            {
                var json = File.ReadAllText(ruta);
                var datos = JsonSerializer.Deserialize<LoginUserDTO>(json);

                LoginUser.Id = datos.Id;
                LoginUser.nomb = datos.nomb;
                LoginUser.numcel = datos.numcel;
                LoginUser.phoneid = datos.phoneid;
            }
        }

        public static void CerrarSesion()
        {
            if (File.Exists(ruta))
                File.Delete(ruta);

            LoginUser.Id = 0;
            LoginUser.nomb = null;
            LoginUser.numcel = null;
            LoginUser.phoneid = null;
            LoginUser.wabaid = null;
        }

        private class LoginUserDTO
        {
            public int Id { get; set; }
            public string nomb { get; set; }
            public string numcel { get; set; }
            public string phoneid { get; set; }
        }
    }
}
