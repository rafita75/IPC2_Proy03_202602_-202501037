using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SAT.Backend.API.Models
{
    // Mensaje de entrada para solicitar autorización de factura
    public class SolicitudAutorizacionDto
    {
        [JsonPropertyName("TIEMPO")]
        public string Tiempo { get; set; } = string.Empty;

        [JsonPropertyName("REFERENCIA")]
        public string Referencia { get; set; } = string.Empty;

        [JsonPropertyName("NIT EMISOR")]
        public string NitEmisor { get; set; } = string.Empty;

        [JsonPropertyName("NIT RECEPTOR")]
        public string NitReceptor { get; set; } = string.Empty;

        [JsonPropertyName("VALOR")]
        public decimal Valor { get; set; }

        [JsonPropertyName("IVA")]
        public decimal Iva { get; set; }

        [JsonPropertyName("TOTAL")]
        public decimal Total { get; set; }
    }

    // Envoltorio principal del mensaje de respuesta de autorizaciones
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