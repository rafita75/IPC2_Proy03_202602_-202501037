using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SAT.Frontend.Web.Models;

namespace SAT.Frontend.Web.Pages
{
    public class PeticionesModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public PeticionesModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // Parámetros Consulta 1: Consultar Contribuyente
        [BindProperty(SupportsGet = true)]
        public string? NitConsulta { get; set; }

        [BindProperty(SupportsGet = true)]
        public string TipoOperacion { get; set; } = "emitidas";

        [BindProperty(SupportsGet = true)]
        public string? FechaInicioContribuyente { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FechaFinContribuyente { get; set; }

        // Parámetros Consulta 2: Resumen de IVA por Fecha y NIT
        [BindProperty(SupportsGet = true)]
        public string? FechaResumenIva { get; set; }

        // Parámetros Consulta 3: Resumen por Rango de Fechas
        [BindProperty(SupportsGet = true)]
        public string? FechaInicioRango { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FechaFinRango { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool MostrarTotales { get; set; } = true;

        // Resultados devueltos por la API
        public string? MensajeError { get; set; }
        public string ConsultaActiva { get; set; } = "ninguna";
        public ConsultaContribuyenteDto? ResultadoContribuyente { get; set; }
        public ResumenIvaFechaDto? ResultadoResumenIva { get; set; }
        public ResumenRangoFechasDto? ResultadoResumenRango { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostConsultarContribuyenteAsync()
        {
            ConsultaActiva = "contribuyente";
            try
            {
                var client = _httpClientFactory.CreateClient("SatApi");
                string url = $"/ConsultaDatos?nit={Uri.EscapeDataString(NitConsulta ?? "")}&tipo={Uri.EscapeDataString(TipoOperacion)}&fechaInicio={Uri.EscapeDataString(FechaInicioContribuyente ?? "")}&fechaFin={Uri.EscapeDataString(FechaFinContribuyente ?? "")}";

                var response = await client.GetAsync(url);
                string body = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    ResultadoContribuyente = JsonSerializer.Deserialize<ConsultaContribuyenteDto>(body, _jsonOptions);
                }
                else
                {
                    MensajeError = $"Error en la consulta: {body}";
                }
            }
            catch (Exception ex)
            {
                MensajeError = $"No se pudo conectar con la API: {ex.Message}";
            }

            return Page();
        }

        public async Task<IActionResult> OnPostResumenIvaAsync()
        {
            ConsultaActiva = "iva";
            try
            {
                var client = _httpClientFactory.CreateClient("SatApi");
                string url = $"/ResumenIva?fecha={Uri.EscapeDataString(FechaResumenIva ?? "")}";

                var response = await client.GetAsync(url);
                string body = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    ResultadoResumenIva = JsonSerializer.Deserialize<ResumenIvaFechaDto>(body, _jsonOptions);
                }
                else
                {
                    MensajeError = $"Error en la consulta: {body}";
                }
            }
            catch (Exception ex)
            {
                MensajeError = $"No se pudo conectar con la API: {ex.Message}";
            }

            return Page();
        }

        public async Task<IActionResult> OnPostResumenRangoAsync()
        {
            ConsultaActiva = "rango";
            try
            {
                var client = _httpClientFactory.CreateClient("SatApi");
                string url = $"/ResumenRango?fechaInicio={Uri.EscapeDataString(FechaInicioRango ?? "")}&fechaFin={Uri.EscapeDataString(FechaFinRango ?? "")}&mostrarTotales={MostrarTotales.ToString().ToLower()}";

                var response = await client.GetAsync(url);
                string body = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    ResultadoResumenRango = JsonSerializer.Deserialize<ResumenRangoFechasDto>(body, _jsonOptions);
                }
                else
                {
                    MensajeError = $"Error en la consulta: {body}";
                }
            }
            catch (Exception ex)
            {
                MensajeError = $"No se pudo conectar con la API: {ex.Message}";
            }

            return Page();
        }
    }
}