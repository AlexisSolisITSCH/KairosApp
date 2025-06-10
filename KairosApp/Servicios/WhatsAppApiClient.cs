using Azure;
using KairosAPI.Models;
using KairosApp.Models;
using Microsoft.EntityFrameworkCore;
using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace KairosApp.Servicios
{
    public class WhatsAppApiClient
    {
        private readonly string token;
        private readonly string phoneNumberId;
        private readonly string apiUrl;
        private readonly string version;

        public WhatsAppApiClient(UrlConfig config)
        {
            token = config.UserAccessToken;
            phoneNumberId = LoginUser.phoneid;
            apiUrl = config.ApiUrl;
            version = config.Version;
        }

        public async Task<string?> plantMarkCupon(
            string telefonoDestino,
            string urlImageCupon)
        {
            var url = $"{apiUrl}/{version}/{phoneNumberId}/messages";
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    var body = new
                    {
                        messaging_product = "whatsapp",
                        to = telefonoDestino,
                        type = "template",
                        template = new
                        {
                            name = "marketing_cupon",
                            language = new { code = "es" },
                            components = new object[]
                            {
                                new
                                {
                                    type = "header",
                                    parameters = new object[]
                                    {
                                        new
                                        {
                                            type = "image",
                                            image = new
                                            {
                                                link = urlImageCupon
                                            }
                                        }
                                    }
                                },
                                new
                                {
                                    type = "body",
                                    parameters = Array.Empty<object>()
                                }
                            }
                        }
                    };

                    var json = System.Text.Json.JsonSerializer.Serialize(body);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync(url, content);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    string mensajeMetaId = null;
                    if (!string.IsNullOrEmpty(responseContent) && responseContent.Contains("messages"))
                    {
                        try
                        {
                            using var jsonDoc = System.Text.Json.JsonDocument.Parse(responseContent);
                            var root = jsonDoc.RootElement;

                            if (root.TryGetProperty("messages", out var messages) && messages.GetArrayLength() > 0)
                            {
                                mensajeMetaId = messages[0].GetProperty("id").GetString();
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Error al obtener mensajeMetaId " + ex.Message);
                        }
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        return mensajeMetaId;
                    }
                    else
                    {
                        var statusCode = response.StatusCode;
                        string motivoFallo;

                        switch (statusCode)
                        {
                            case HttpStatusCode.Unauthorized:
                                motivoFallo = "Token invalido o caducado!";
                                break;
                            case HttpStatusCode.BadRequest:
                                motivoFallo = responseContent.Contains("not a WhatsApp user")
                                    ? "Numero no registrado"
                                    : "Solicitud mal formada";
                                break;
                            case HttpStatusCode.NotFound:
                                motivoFallo = "Plantilla o numero inexistente";
                                break;
                            case HttpStatusCode.RequestTimeout:
                                motivoFallo = "Timeout al contactar API";
                                break;
                            case HttpStatusCode.InternalServerError:
                            case HttpStatusCode.BadGateway:
                            case HttpStatusCode.GatewayTimeout:
                                motivoFallo = "Error interno en la API de WhatsApp";
                                break;
                            default:
                                motivoFallo = $"Error desconocido ({(int)statusCode})";
                                break;
                        }
                        WifiConnection.Fallo(telefonoDestino, "Desconocido", "-", "-", "-", motivoFallo, ((int)statusCode).ToString(), mensajeMetaId);
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al enviar plantilla: " + ex.Message);
                return null;
            }
        }

        public async Task<string?> plantMarkPromo(
            string telefonoDestino,
            string urlImagePromo)
        {
            var url = $"{apiUrl}/{version}/{phoneNumberId}/messages";
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    var body = new
                    {
                        messaging_product = "whatsapp",
                        to = telefonoDestino,
                        type = "template",
                        template = new
                        {
                            name = "marketing_promocion_mes",
                            language = new { code = "es" },
                            components = new object[]
                            {
                                new
                                {
                                    type = "header",
                                    parameters = new object[]
                                    {
                                        new
                                        {
                                            type = "image",
                                            image = new
                                            {
                                                link = urlImagePromo
                                            }
                                        }
                                    }
                                },
                                new
                                {
                                    type = "body",
                                    parameters = Array.Empty<object>()
                                }
                            }
                        }
                    };

                    var json = System.Text.Json.JsonSerializer.Serialize(body);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync(url, content);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    string mensajeMetaId = null;
                    if (!string.IsNullOrEmpty(responseContent) && responseContent.Contains("messages"))
                    {
                        try
                        {
                            using var jsonDoc = System.Text.Json.JsonDocument.Parse(responseContent);
                            var root = jsonDoc.RootElement;

                            if (root.TryGetProperty("messages", out var messages) && messages.GetArrayLength() > 0)
                            {
                                mensajeMetaId = messages[0].GetProperty("id").GetString();
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Error al obtener mensajeMetaId " + ex.Message);
                        }
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        return mensajeMetaId;
                    }
                    else
                    {
                        var statusCode = response.StatusCode;
                        string motivoFallo;

                        switch (statusCode)
                        {
                            case HttpStatusCode.Unauthorized:
                                motivoFallo = "Token invalido o caducado!";
                                break;
                            case HttpStatusCode.BadRequest:
                                motivoFallo = responseContent.Contains("not a WhatsApp user")
                                    ? "Numero no registrado"
                                    : "Solicitud mal formada";
                                break;
                            case HttpStatusCode.NotFound:
                                motivoFallo = "Plantilla o numero inexistente";
                                break;
                            case HttpStatusCode.RequestTimeout:
                                motivoFallo = "Timeout al contactar API";
                                break;
                            case HttpStatusCode.InternalServerError:
                            case HttpStatusCode.BadGateway:
                            case HttpStatusCode.GatewayTimeout:
                                motivoFallo = "Error interno en la API de WhatsApp";
                                break;
                            default:
                                motivoFallo = $"Error desconocido ({(int)statusCode})";
                                break;
                        }
                        WifiConnection.Fallo(telefonoDestino, "Desconocido", "-", "-", "-", motivoFallo, ((int)statusCode).ToString(), mensajeMetaId);
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al enviar plantilla: " + ex.Message);
                return null;
            }
        }

        public async Task<string?> plantillaMarketing(
            string telefonoDestino,
            string urlImageMark)
        {
            var url = $"{apiUrl}/{version}/{phoneNumberId}/messages";
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                    var body = new
                    {
                        messaging_product = "whatsapp",
                        to = telefonoDestino,
                        type = "template",
                        template = new
                        {
                            name = "asesoria_marketing",
                            language = new { code = "es" },
                            components = new object[]
                            {
                                new
                                {
                                    type = "header",
                                    parameters = new object[]
                                    {
                                        new
                                        {
                                            type = "image",
                                            image = new
                                            {
                                                link = urlImageMark
                                            }
                                        }
                                    }
                                },
                                new
                                {
                                    type = "body",
                                    parameters = Array.Empty<object>()
                                }
                            }
                        }
                    };

                    var json = System.Text.Json.JsonSerializer.Serialize(body);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync(url, content);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    string mensajeMetaId = null;
                    if (!string.IsNullOrEmpty(responseContent) && responseContent.Contains("messages"))
                    {
                        try
                        {
                            using var jsonDoc = System.Text.Json.JsonDocument.Parse(responseContent);
                            var root = jsonDoc.RootElement;

                            if (root.TryGetProperty("messages", out var messages) && messages.GetArrayLength() > 0)
                            {
                                mensajeMetaId = messages[0].GetProperty("id").GetString();
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Error al obtener mensajeMetaId " + ex.Message);
                        }
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        return mensajeMetaId;
                    }
                    else
                    {
                        var statusCode = response.StatusCode;
                        string motivoFallo;

                        switch (statusCode)
                        {
                            case HttpStatusCode.Unauthorized:
                                motivoFallo = "Token invalido o caducado!";
                                break;
                            case HttpStatusCode.BadRequest:
                                motivoFallo = responseContent.Contains("not a WhatsApp user")
                                    ? "Numero no registrado"
                                    : "Solicitud mal formada";
                                break;
                            case HttpStatusCode.NotFound:
                                motivoFallo = "Plantilla o numero inexistente";
                                break;
                            case HttpStatusCode.RequestTimeout:
                                motivoFallo = "Timeout al contactar API";
                                break;
                            case HttpStatusCode.InternalServerError:
                            case HttpStatusCode.BadGateway:
                            case HttpStatusCode.GatewayTimeout:
                                motivoFallo = "Error interno en la API de WhatsApp";
                                break;
                            default:
                                motivoFallo = $"Error desconocido ({(int)statusCode})";
                                break;
                        }
                        WifiConnection.Fallo(telefonoDestino, "Desconocido", "-", "-", "-", motivoFallo, ((int)statusCode).ToString(), mensajeMetaId);
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al enviar plantilla: " + ex.Message);
                return null;
            }
        }

        public async Task<string?> EnviarPlantillaFactura2Async(
            string telefonoDestino,
            string nombreCliente,
            string folio,
            string comprobante,
            string total,
            string urlImageHeader,
            string linkDescarga)
        {
            var url = $"{apiUrl}/{version}/{phoneNumberId}/messages";
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                    var body = new
                    {
                        messaging_product = "whatsapp",
                        to = telefonoDestino,
                        type = "template",
                        template = new
                        {
                            name = "factura_prueba2",
                            language = new { code = "es" },
                            components = new object[]
                            {
                            new {
                                type = "header",
                                parameters = new object[]
                                {
                                    new
                                    {
                                        type = "image",
                                        image = new
                                        {
                                            link = urlImageHeader
                                        }
                                    }
                                }
                            },
                            new
                            {
                                type = "body",
                                parameters = new object[]
                                {
                                    new { type = "text", text = nombreCliente },
                                    new { type = "text", text = folio },
                                    new { type = "text", text = comprobante },
                                    new { type = "text", text = total },
                                    new { type = "text", text = linkDescarga }
                                }
                            }
                            }
                        }
                    };
                    var json = System.Text.Json.JsonSerializer.Serialize(body);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync(url, content);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    string mensajeMetaId = null;
                    if (!string.IsNullOrEmpty(responseContent) && responseContent.Contains("messages"))
                    {
                        try
                        {
                            using var jsonDoc = System.Text.Json.JsonDocument.Parse(responseContent);
                            var root = jsonDoc.RootElement;

                            if (root.TryGetProperty("messages", out var messages) && messages.GetArrayLength() > 0)
                            {
                                mensajeMetaId = messages[0].GetProperty("id").GetString();
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Error al obtener mensajeMetaId " + ex.Message);
                        }
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        return mensajeMetaId;
                    }
                    else
                    {
                        var statusCode = response.StatusCode;
                        string motivoFallo;

                        switch (statusCode)
                        {
                            case HttpStatusCode.Unauthorized:
                                motivoFallo = "Token invalido o caducado!";
                                break;
                            case HttpStatusCode.BadRequest:
                                motivoFallo = responseContent.Contains("not a WhatsApp user")
                                    ? "Numero no registrado"
                                    : "Solicitud mal formada";
                                break;
                            case HttpStatusCode.NotFound:
                                motivoFallo = "Plantilla o numero inexistente";
                                break;
                            case HttpStatusCode.RequestTimeout:
                                motivoFallo = "Timeout al contactar API";
                                break;
                            case HttpStatusCode.InternalServerError:
                            case HttpStatusCode.BadGateway:
                            case HttpStatusCode.GatewayTimeout:
                                motivoFallo = "Error interno en la API de WhatsApp";
                                break;
                            default:
                                motivoFallo = $"Error desconocido ({(int)statusCode})";
                                break;
                        }
                        WifiConnection.Fallo(telefonoDestino, nombreCliente, folio, comprobante, total, motivoFallo, ((int)statusCode).ToString(), mensajeMetaId);
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al enviar plantilla: " + ex.Message);
                return null;
            }
        }
    }
}
