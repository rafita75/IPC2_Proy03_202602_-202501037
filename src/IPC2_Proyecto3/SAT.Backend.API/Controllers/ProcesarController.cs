using System.Collections.Generic;
using System.Text.Json;
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

        /// <summary>
        /// Endpoint principal sugerido en la Fig. 2 (POST /Procesar).
        /// Detecta automáticamente si el JSON entrante corresponde a actualización de RTU o a Autorización de DTEs.
        /// </summary>
        [HttpPost("/Procesar")]
        public IActionResult ProcesarAutomatico([FromBody] JsonElement payload)
        {
            if (payload.ValueKind != JsonValueKind.Array)
            {
                return BadRequest(new { error = "El mensaje JSON de entrada debe ser un arreglo [...]" });
            }

            string rawJson = payload.GetRawText();

            // Si el arreglo está vacío, devolvemos respuesta vacía de RTU por defecto
            if (payload.GetArrayLength() == 0)
            {
                return Ok(new RespuestaRtuDto());
            }

            JsonElement primerElemento = payload[0];

            // Si el primer objeto contiene la propiedad "TIEMPO" o "REFERENCIA", es un archivo de facturas (DTE)
            if (primerElemento.TryGetProperty("TIEMPO", out _) || primerElemento.TryGetProperty("REFERENCIA", out _))
            {
                var solicitudesFacturas = JsonSerializer.Deserialize<List<SolicitudAutorizacionDto>>(rawJson);
                var resultadoFacturas = _satService.ProcesarAutorizaciones(solicitudesFacturas ?? new());
                return Ok(resultadoFacturas);
            }

            // Si contiene "NIT" o "nombre", es una actualización del RTU
            if (primerElemento.TryGetProperty("NIT", out _) || primerElemento.TryGetProperty("nombre", out _))
            {
                var solicitudesRtu = JsonSerializer.Deserialize<List<ContribuyenteDto>>(rawJson);
                var resultadoRtu = _satService.ProcesarActualizacionRtu(solicitudesRtu ?? new());
                return Ok(resultadoRtu);
            }

            return BadRequest(new { error = "No se reconoció la estructura del JSON enviado." });
        }

        /// <summary>
        /// Ruta explícita para actualizar el RTU (POST /Procesar/Rtu).
        /// </summary>
        [HttpPost("/Procesar/Rtu")]
        public ActionResult<RespuestaRtuDto> ProcesarRtu([FromBody] List<ContribuyenteDto> solicitudes)
        {
            var respuesta = _satService.ProcesarActualizacionRtu(solicitudes);
            return Ok(respuesta);
        }

        /// <summary>
        /// Ruta explícita para solicitar autorización de facturas (POST /Procesar/Autorizaciones).
        /// </summary>
        [HttpPost("/Procesar/Autorizaciones")]
        public ActionResult<RespuestaAutorizacionWrapperDto> ProcesarAutorizaciones([FromBody] List<SolicitudAutorizacionDto> solicitudes)
        {
            var respuesta = _satService.ProcesarAutorizaciones(solicitudes);
            return Ok(respuesta);
        }

        /// <summary>
        /// Devuelve el sistema al estado inicial (sin datos), limpiando memoria y archivos JSON.
        /// </summary>
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