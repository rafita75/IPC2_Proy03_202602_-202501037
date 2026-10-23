using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using SAT.Backend.API.Models;
using SAT.Backend.API.Services;

namespace SAT.Backend.API.Controllers
{
    [ApiController]
    public class ConsultasController : ControllerBase
    {
        private readonly SatDataService _satService;

        public ConsultasController(SatDataService satService)
        {
            _satService = satService;
        }

        // Método auxiliar para aceptar fechas en formato dd/MM/yyyy o yyyy-MM-dd
        private bool IntentarParsearFecha(string fechaStr, out DateTime fecha)
        {
            fecha = DateTime.MinValue;
            if (string.IsNullOrWhiteSpace(fechaStr)) return false;

            string[] formatos = { "dd/MM/yyyy", "yyyy-MM-dd", "d/M/yyyy" };
            return DateTime.TryParseExact(
                fechaStr.Trim(),
                formatos,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out fecha);
        }

        /// <summary>
        /// 1. Consultar Contribuyente: Facturas emitidas o recibidas por NIT en un rango de fechas.
        /// </summary>
        [HttpGet("/ConsultaDatos")]
        public ActionResult<ConsultaContribuyenteDto> ConsultarContribuyente(
            [FromQuery] string nit,
            [FromQuery] string tipo = "emitidas",
            [FromQuery] string fechaInicio = "",
            [FromQuery] string fechaFin = "")
        {
            if (string.IsNullOrWhiteSpace(nit))
                return BadRequest(new { error = "Debe especificar un NIT." });

            if (!IntentarParsearFecha(fechaInicio, out DateTime dtInicio) ||
                !IntentarParsearFecha(fechaFin, out DateTime dtFin))
            {
                return BadRequest(new { error = "Rango de fechas inválido. Use dd/MM/yyyy o yyyy-MM-dd." });
            }

            nit = nit.Trim().ToUpperInvariant();
            bool buscarEmitidas = !tipo.Trim().Equals("recibidas", StringComparison.OrdinalIgnoreCase);

            var rtu = _satService.ObtenerRtu();
            string nombre = rtu.ContainsKey(nit) ? rtu[nit] : "No registrado en RTU";

            // Filtrar facturas autorizadas del contribuyente en el rango de fechas
            var facturasFiltradas = _satService.ObtenerFacturas()
                .Where(f => f.Aprobada &&
                            (buscarEmitidas ? f.NitEmisor == nit : f.NitReceptor == nit) &&
                            IntentarParsearFecha(f.Fecha, out DateTime fDate) &&
                            fDate.Date >= dtInicio.Date && fDate.Date <= dtFin.Date)
                .OrderBy(f => DateTime.ParseExact(f.Fecha, "dd/MM/yyyy", CultureInfo.InvariantCulture))
                .ToList();

            var detallePorFecha = facturasFiltradas
                .GroupBy(f => f.Fecha)
                .Select(g => new ResumenFechaContribuyenteDto
                {
                    Fecha = g.Key,
                    CantidadFacturasAutorizadas = g.Count(),
                    MontoTotalFacturado = g.Sum(x => x.Total)
                })
                .ToList();

            var respuesta = new ConsultaContribuyenteDto
            {
                Nit = nit,
                NombreContribuyente = nombre,
                TipoOperacion = buscarEmitidas ? "Emitidas" : "Recibidas",
                FechaInicio = dtInicio.ToString("dd/MM/yyyy"),
                FechaFin = dtFin.ToString("dd/MM/yyyy"),
                TotalFacturadoEnRango = detallePorFecha.Sum(d => d.MontoTotalFacturado),
                DetallePorFecha = detallePorFecha,
                ListadoFacturas = facturasFiltradas
            };

            return Ok(respuesta);
        }

        /// <summary>
        /// 2. Resumen de IVA por fecha y NIT: Muestra IVA emitido e IVA recibido por cada NIT en una fecha.
        /// </summary>
        [HttpGet("/ResumenIva")]
        public ActionResult<ResumenIvaFechaDto> ObtenerResumenIva([FromQuery] string fecha)
        {
            if (!IntentarParsearFecha(fecha, out DateTime dtFecha))
                return BadRequest(new { error = "Fecha inválida. Use dd/MM/yyyy o yyyy-MM-dd." });

            string fechaBuscada = dtFecha.ToString("dd/MM/yyyy");
            var rtu = _satService.ObtenerRtu();

            // Tomamos las facturas aprobadas en esa fecha
            var facturasDelDia = _satService.ObtenerFacturas()
                .Where(f => f.Aprobada && f.Fecha == fechaBuscada)
                .ToList();

            // Obtener todos los NITs que tuvieron movimiento (como emisor o receptor) en esa fecha
            var nitsConMovimiento = facturasDelDia.Select(f => f.NitEmisor)
                .Union(facturasDelDia.Select(f => f.NitReceptor))
                .Distinct()
                .OrderBy(n => n)
                .ToList();

            var movimientos = nitsConMovimiento.Select(nit => new MovimientoIvaPorNitDto
            {
                Nit = nit,
                Nombre = rtu.ContainsKey(nit) ? rtu[nit] : "Desconocido",
                IvaEmitido = facturasDelDia.Where(f => f.NitEmisor == nit).Sum(f => f.Iva),
                IvaRecibido = facturasDelDia.Where(f => f.NitReceptor == nit).Sum(f => f.Iva)
            }).ToList();

            return Ok(new ResumenIvaFechaDto
            {
                Fecha = fechaBuscada,
                MovimientosPorNit = movimientos
            });
        }

        /// <summary>
        /// 3. Resumen por rango de fechas: Valores autorizados (Total o Sin IVA) y estadísticas de errores por fecha.
        /// </summary>
        [HttpGet("/ResumenRango")]
        public ActionResult<ResumenRangoFechasDto> ObtenerResumenRango(
            [FromQuery] string fechaInicio,
            [FromQuery] string fechaFin,
            [FromQuery] bool mostrarTotales = true)
        {
            if (!IntentarParsearFecha(fechaInicio, out DateTime dtInicio) ||
                !IntentarParsearFecha(fechaFin, out DateTime dtFin))
            {
                return BadRequest(new { error = "Rango de fechas inválido. Use dd/MM/yyyy o yyyy-MM-dd." });
            }

            var facturasEnRango = _satService.ObtenerFacturas()
                .Where(f => IntentarParsearFecha(f.Fecha, out DateTime fDate) &&
                            fDate.Date >= dtInicio.Date && fDate.Date <= dtFin.Date)
                .ToList();

            var reportePorFechas = facturasEnRango
                .GroupBy(f => f.Fecha)
                .OrderBy(g => DateTime.ParseExact(g.Key, "dd/MM/yyyy", CultureInfo.InvariantCulture))
                .Select(g =>
                {
                    decimal sinIva = g.Where(f => f.Aprobada).Sum(f => f.Valor);
                    decimal conIva = g.Where(f => f.Aprobada).Sum(f => f.Total);

                    return new EstadisticaPorFechaDto
                    {
                        Fecha = g.Key,
                        ValorAutorizadoSinIva = sinIva,
                        ValorAutorizadoTotal = conIva,
                        ValorMostrado = mostrarTotales ? conIva : sinIva,

                        CantidadTotalFacturasRecibidas = g.Count(),
                        ErrorNitEmisorInvalido = g.Count(f => f.ErrorNitEmisorInvalido),
                        ErrorNitReceptorInvalido = g.Count(f => f.ErrorNitReceptorInvalido),
                        ErrorNitEmisorInexistente = g.Count(f => f.ErrorNitEmisorInexistente),
                        ErrorNitReceptorInexistente = g.Count(f => f.ErrorNitReceptorInexistente),
                        ErrorIvaMalCalculado = g.Count(f => f.ErrorIvaMalCalculado),
                        ErrorTotalMalCalculado = g.Count(f => f.ErrorTotalMalCalculado),
                        ErrorReferenciaDuplicada = g.Count(f => f.ErrorReferenciaDuplicada),
                        CantidadFacturasSinErrores = g.Count(f => f.Aprobada),
                        CantidadEmisoresDistintos = g.Select(f => f.NitEmisor).Distinct().Count(),
                        CantidadReceptoresDistintos = g.Select(f => f.NitReceptor).Distinct().Count()
                    };
                })
                .ToList();

            return Ok(new ResumenRangoFechasDto
            {
                FechaInicio = dtInicio.ToString("dd/MM/yyyy"),
                FechaFin = dtFin.ToString("dd/MM/yyyy"),
                MostrarValoresTotales = mostrarTotales,
                ReportePorFechas = reportePorFechas
            });
        }

        /// <summary>
        /// 4. Endpoint /Grafica sugerido en la Fig. 2 para devolver las series de datos de las gráficas.
        /// Parámetros: tipo = "iva" (requiere fecha) o "rango" (requiere fechaInicio, fechaFin, mostrarTotales).
        /// </summary>
        [HttpGet("/Grafica")]
        public IActionResult ObtenerDatosGrafica(
            [FromQuery] string tipo = "iva",
            [FromQuery] string fecha = "",
            [FromQuery] string fechaInicio = "",
            [FromQuery] string fechaFin = "",
            [FromQuery] bool mostrarTotales = true)
        {
            if (tipo.Trim().Equals("iva", StringComparison.OrdinalIgnoreCase))
            {
                var resultadoIva = ObtenerResumenIva(fecha);
                if (resultadoIva.Result is OkObjectResult okIva && okIva.Value is ResumenIvaFechaDto dtoIva)
                {
                    return Ok(new
                    {
                        tipoGrafica = "Resumen de IVA por Fecha y NIT",
                        fecha = dtoIva.Fecha,
                        etiquetasNits = dtoIva.MovimientosPorNit.Select(m => m.Nit).ToList(),
                        ivaEmitido = dtoIva.MovimientosPorNit.Select(m => m.IvaEmitido).ToList(),
                        ivaRecibido = dtoIva.MovimientosPorNit.Select(m => m.IvaRecibido).ToList()
                    });
                }
                return BadRequest(new { error = "Parámetros inválidos para gráfica de IVA." });
            }
            else
            {
                var resultadoRango = ObtenerResumenRango(fechaInicio, fechaFin, mostrarTotales);
                if (resultadoRango.Result is OkObjectResult okRango && okRango.Value is ResumenRangoFechasDto dtoRango)
                {
                    return Ok(new
                    {
                        tipoGrafica = mostrarTotales ? "Valores Totales Autorizados por Fecha" : "Valores Sin IVA Autorizados por Fecha",
                        fechaInicio = dtoRango.FechaInicio,
                        fechaFin = dtoRango.FechaFin,
                        etiquetasFechas = dtoRango.ReportePorFechas.Select(r => r.Fecha).ToList(),
                        valoresAutorizados = dtoRango.ReportePorFechas.Select(r => r.ValorMostrado).ToList()
                    });
                }
                return BadRequest(new { error = "Parámetros inválidos para gráfica por rango de fechas." });
            }
        }
    }
}