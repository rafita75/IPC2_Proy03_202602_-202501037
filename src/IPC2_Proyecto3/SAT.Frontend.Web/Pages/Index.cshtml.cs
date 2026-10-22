using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SAT.Frontend.Web.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public IndexModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty]
        public IFormFile? ArchivoJson { get; set; }

        [BindProperty]
        public string TipoOperacion { get; set; } = "auto";

        public string? MensajeEstado { get; set; }
        public bool EsError { get; set; }
        public string? RespuestaJsonFormateada { get; set; }

        public void OnGet()
        {
        }

        // Enviar archivo JSON (RTU o Autorización de Facturas) al Servicio 2
        public async Task<IActionResult> OnPostEnviarAsync()
        {
            if (ArchivoJson == null || ArchivoJson.Length == 0)
            {
                EsError = true;
                MensajeEstado = "Por favor selecciona un archivo con extensión .json antes de enviar.";
                return Page();
            }

            if (!ArchivoJson.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                EsError = true;
                MensajeEstado = "El archivo seleccionado debe tener extensión .json.";
                return Page();
            }

            try
            {
                string contenidoJson;
                using (var reader = new StreamReader(ArchivoJson.OpenReadStream(), Encoding.UTF8))
                {
                    contenidoJson = await reader.ReadToEndAsync();
                }

                var client = _httpClientFactory.CreateClient("SatApi");
                var httpContent = new StringContent(contenidoJson, Encoding.UTF8, "application/json");

                string endpoint = TipoOperacion switch
                {
                    "rtu" => "/Procesar/Rtu",
                    "facturas" => "/Procesar/Autorizaciones",
                    _ => "/Procesar"
                };

                var response = await client.PostAsync(endpoint, httpContent);
                string respuestaRaw = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    EsError = false;
                    MensajeEstado = $"Archivo '{ArchivoJson.FileName}' procesado correctamente por el Servicio 2.";

                    // Formatear el JSON de respuesta con sangría para mostrarlo limpio en pantalla
                    using var doc = JsonDocument.Parse(respuestaRaw);
                    RespuestaJsonFormateada = JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
                }
                else
                {
                    EsError = true;
                    MensajeEstado = $"El Backend devolvió un error ({(int)response.StatusCode}): {respuestaRaw}";
                }
            }
            catch (Exception ex)
            {
                EsError = true;
                MensajeEstado = $"Error al conectar con el Servicio 2 (API): {ex.Message}";
            }

            return Page();
        }

        // Opción Inicializar: Devuelve el sistema al estado inicial (sin datos)
        public async Task<IActionResult> OnPostResetearAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("SatApi");
                var response = await client.PostAsync("/Resetear", null);
                string respuestaRaw = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    EsError = false;
                    MensajeEstado = "El sistema ha sido inicializado correctamente (todos los datos de RTU y facturas fueron eliminados).";
                    RespuestaJsonFormateada = respuestaRaw;
                }
                else
                {
                    EsError = true;
                    MensajeEstado = $"No se pudo inicializar el sistema: {respuestaRaw}";
                }
            }
            catch (Exception ex)
            {
                EsError = true;
                MensajeEstado = $"Error al conectar con el Servicio 2 (API): {ex.Message}";
            }

            return Page();
        }
    }
}