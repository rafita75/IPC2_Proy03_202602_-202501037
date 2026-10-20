using System.Text.Json.Serialization;

namespace SAT.Backend.API.Models
{
    // Estructura de entrada para cada elemento del JSON de RTU
    public class ContribuyenteDto
    {
        [JsonPropertyName("NIT")]
        public string Nit { get; set; } = string.Empty;

        [JsonPropertyName("nombre")]
        public string Nombre { get; set; } = string.Empty;
    }

    // Estructura de salida al procesar una solicitud de actualización de RTU
    public class RespuestaRtuDto
    {
        [JsonPropertyName("CONTRIBUYENTES NUEVOS")]
        public int ContribuyentesNuevos { get; set; }

        [JsonPropertyName("CONTRIBUYENTES ACTUALIZADOS")]
        public int ContribuyentesActualizados { get; set; }

        [JsonPropertyName("NITS INVALIDOS")]
        public int NitsInvalidos { get; set; }
    }
}