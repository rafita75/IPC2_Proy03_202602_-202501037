using System.Collections.Generic;

namespace SAT.Backend.API.Models
{
    // 1. Respuesta para Consultar Contribuyente (/ConsultaDatos)
    public class ConsultaContribuyenteDto
    {
        public string Nit { get; set; } = string.Empty;
        public string NombreContribuyente { get; set; } = string.Empty;
        public string TipoOperacion { get; set; } = string.Empty; // Emitidas o Recibidas
        public string FechaInicio { get; set; } = string.Empty;
        public string FechaFin { get; set; } = string.Empty;
        public decimal TotalFacturadoEnRango { get; set; }
        public List<ResumenFechaContribuyenteDto> DetallePorFecha { get; set; } = new();
        public List<RegistroFactura> ListadoFacturas { get; set; } = new();
    }

    public class ResumenFechaContribuyenteDto
    {
        public string Fecha { get; set; } = string.Empty;
        public int CantidadFacturasAutorizadas { get; set; }
        public decimal MontoTotalFacturado { get; set; }
    }

    // 2. Respuesta para Resumen de IVA por fecha y NIT (/ResumenIva)
    public class ResumenIvaFechaDto
    {
        public string Fecha { get; set; } = string.Empty;
        public List<MovimientoIvaPorNitDto> MovimientosPorNit { get; set; } = new();
    }

    public class MovimientoIvaPorNitDto
    {
        public string Nit { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public decimal IvaEmitido { get; set; }
        public decimal IvaRecibido { get; set; }
    }

    // 3. Respuesta para Resumen por rango de fechas y Estadísticas (/ResumenRango)
    public class ResumenRangoFechasDto
    {
        public string FechaInicio { get; set; } = string.Empty;
        public string FechaFin { get; set; } = string.Empty;
        public bool MostrarValoresTotales { get; set; }
        public List<EstadisticaPorFechaDto> ReportePorFechas { get; set; } = new();
    }

    public class EstadisticaPorFechaDto
    {
        public string Fecha { get; set; } = string.Empty;

        // Valores monetarios para el resumen y gráfica
        public decimal ValorAutorizadoSinIva { get; set; }
        public decimal ValorAutorizadoTotal { get; set; }
        public decimal ValorMostrado { get; set; }

        // Desglose estadístico solicitado en la página 3 del enunciado
        public int CantidadTotalFacturasRecibidas { get; set; }
        public int ErrorNitEmisorInvalido { get; set; }
        public int ErrorNitReceptorInvalido { get; set; }
        public int ErrorNitEmisorInexistente { get; set; }
        public int ErrorNitReceptorInexistente { get; set; }
        public int ErrorIvaMalCalculado { get; set; }
        public int ErrorTotalMalCalculado { get; set; }
        public int ErrorReferenciaDuplicada { get; set; }
        public int CantidadFacturasSinErrores { get; set; }
        public int CantidadEmisoresDistintos { get; set; }
        public int CantidadReceptoresDistintos { get; set; }
    }
}