using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SAT.Backend.API.Models
{
    public class SolicitudAutorizacionDto
    {
        [JsonPropertyName("TIEMPO")]
        public string Tiempo { get; set; } = string.Empty;

        [JsonPropertyName("REFERENCIA")]
        public string Referencia { get; set; } = string.Empty;

        [JsonPropertyName("NIT EMISOR")]
        public string NitEmisor { get; set; } = string.Empty;

        // Soporte simultáneo para los archivos de prueba que usan guion bajo
        [JsonPropertyName("NIT_EMISOR")]
        public string NitEmisorGuion
        {
            get => NitEmisor;
            set { if (!string.IsNullOrEmpty(value)) NitEmisor = value; }
        }

        [JsonPropertyName("NIT RECEPTOR")]
        public string NitReceptor { get; set; } = string.Empty;

        [JsonPropertyName("NIT_RECEPTOR")]
        public string NitReceptorGuion
        {
            get => NitReceptor;
            set { if (!string.IsNullOrEmpty(value)) NitReceptor = value; }
        }

        [JsonPropertyName("VALOR")]
        public decimal Valor { get; set; }

        [JsonPropertyName("IVA")]
        public decimal Iva { get; set; }

        [JsonPropertyName("TOTAL")]
        public decimal Total { get; set; }
    }

    public class RespuestaAutorizacionWrapperDto
    {
        [JsonPropertyName("resultadoAutorizaciones")]
        public ResultadoAutorizacionesDto ResultadoAutorizaciones { get; set; } = new();
    }

    public class ResultadoAutorizacionesDto
    {
        [JsonPropertyName("cantidadAutorizacionesAprobadas")]
        public int CantidadAutorizacionesAprobadas { get; set; }

        [JsonPropertyName("cantidadAutorizacionesRechazadasPorNitEmisorInexistente")]
        public int CantidadRechazadasNitEmisorInexistente { get; set; }

        [JsonPropertyName("cantidadAutorizacionesRechazadasPorNitReceptorInexistente")]
        public int CantidadRechazadasNitReceptorInexistente { get; set; }

        [JsonPropertyName("cantidadAutorizacionesRechazadasPorNitEmisorInvalido")]
        public int CantidadRechazadasNitEmisorInvalido { get; set; }

        [JsonPropertyName("cantidadAutorizacionesRechazadasPorNitReceptorInvalido")]
        public int CantidadRechazadasNitReceptorInvalido { get; set; }

        [JsonPropertyName("cantidadAutorizacionesRechazadasPorIvaMalCalculado")]
        public int CantidadRechazadasIvaMalCalculado { get; set; }

        [JsonPropertyName("cantidadAutorizacionesRechazadasPorTotalMalCalculado")]
        public int CantidadRechazadasTotalMalCalculado { get; set; }

        [JsonPropertyName("cantidadAutorizacionesRechazadasPorReferenciaDuplicada")]
        public int CantidadRechazadasReferenciaDuplicada { get; set; }

        [JsonPropertyName("cantidadAutorizacionesRechazadas")]
        public int CantidadAutorizacionesRechazadas { get; set; }

        [JsonPropertyName("cantidadDescartadasPorFormato")]
        public int CantidadDescartadasPorFormato { get; set; }

        [JsonPropertyName("detalleAprobaciones")]
        public List<DetalleAprobacionDto> DetalleAprobaciones { get; set; } = new();
    }

    public class DetalleAprobacionDto
    {
        [JsonPropertyName("referencia")]
        public string Referencia { get; set; } = string.Empty;

        [JsonPropertyName("codigoAprobacion")]
        public long CodigoAprobacion { get; set; }
    }
}