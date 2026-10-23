using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SAT.Backend.API.Models;
using SAT.Backend.API.Services;

namespace SAT.Backend.API.Controllers
{
    [ApiController]
    public class ProcesarController : ControllerBase
    {
        private readonly SatDataService _satService;

        public ProcesarController(SatDataService satService)
        {
            _satService = satService;
        }

        [HttpPost("/Procesar")]
        public async Task<IActionResult> ProcesarAutomatico()
        {
            string rawJson;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                rawJson = await reader.ReadToEndAsync();
            }

            if (string.IsNullOrWhiteSpace(rawJson))
            {
                return BadRequest(new { error = "El cuerpo del mensaje JSON está vacío." });
            }

            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(rawJson);
            }
            catch (JsonException)
            {
                return BadRequest(new { error = "El archivo enviado no tiene un formato JSON válido." });
            }

            using (doc)
            {
                JsonElement payload = doc.RootElement;

                if (payload.ValueKind != JsonValueKind.Array)
                {
                    return BadRequest(new { error = "El mensaje JSON de entrada debe ser un arreglo [...]" });
                }

                if (payload.GetArrayLength() == 0)
                {
                    return BadRequest(new { error = "El arreglo JSON no contiene elementos para procesar." });
                }

                JsonElement primerElemento = payload[0];
                if (primerElemento.ValueKind != JsonValueKind.Object)
                {
                    return BadRequest(new { error = "Los elementos del arreglo deben ser objetos JSON." });
                }

                // Detectar si es solicitud de Autorización de Facturas (DTE)
                if (primerElemento.TryGetProperty("TIEMPO", out _) ||
                    primerElemento.TryGetProperty("REFERENCIA", out _) ||
                    primerElemento.TryGetProperty("NIT_EMISOR", out _) ||
                    primerElemento.TryGetProperty("NIT EMISOR", out _))
                {
                    try
                    {
                        var solicitudesFacturas = JsonSerializer.Deserialize<List<SolicitudAutorizacionDto>>(rawJson);
                        var resultadoFacturas = _satService.ProcesarAutorizaciones(solicitudesFacturas ?? new());
                        return Ok(resultadoFacturas);
                    }
                    catch (JsonException)
                    {
                        return BadRequest(new { error = "Valores numéricos o estructura inválida en las facturas." });
                    }
                }

                // Detectar si es actualización del RTU
                if (primerElemento.TryGetProperty("NIT", out _) && primerElemento.TryGetProperty("nombre", out _))
                {
                    var solicitudesRtu = JsonSerializer.Deserialize<List<ContribuyenteDto>>(rawJson);
                    var resultadoRtu = _satService.ProcesarActualizacionRtu(solicitudesRtu ?? new());
                    return Ok(resultadoRtu);
                }

                return BadRequest(new { error = "No se reconoció la estructura del JSON enviado." });
            }
        }

        [HttpPost("/Procesar/Rtu")]
        public ActionResult<RespuestaRtuDto> ProcesarRtu([FromBody] List<ContribuyenteDto> solicitudes)
        {
            if (solicitudes == null || solicitudes.Count == 0)
                return BadRequest(new { error = "El listado de RTU está vacío." });

            return Ok(_satService.ProcesarActualizacionRtu(solicitudes));
        }

        [HttpPost("/Procesar/Autorizaciones")]
        public ActionResult<RespuestaAutorizacionWrapperDto> ProcesarAutorizaciones([FromBody] List<SolicitudAutorizacionDto> solicitudes)
        {
            if (solicitudes == null || solicitudes.Count == 0)
                return BadRequest(new { error = "El listado de facturas está vacío." });

            return Ok(_satService.ProcesarAutorizaciones(solicitudes));
        }

        [HttpPost("/Resetear")]
        [HttpPost("/Inicializar")]
        [HttpDelete("/Resetear")]
        public IActionResult ResetearSistema()
        {
            _satService.ResetearDatos();
            return Ok(new { mensaje = "Sistema reiniciado al estado inicial correctamente." });
        }
    }
}