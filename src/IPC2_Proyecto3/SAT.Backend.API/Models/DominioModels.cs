using System.Collections.Generic;

namespace SAT.Backend.API.Models
{
    // Representa un registro de factura ya evaluado para guardar en el historial y generar reportes
    public class RegistroFactura
    {
        public string Fecha { get; set; } = string.Empty; // Formato dd/MM/yyyy
        public string Hora { get; set; } = string.Empty;  // Formato HH:mm
        public string Referencia { get; set; } = string.Empty;
        public string NitEmisor { get; set; } = string.Empty;
        public string NitReceptor { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public decimal Iva { get; set; }
        public decimal Total { get; set; }

        // Estado de aprobación
        public bool Aprobada { get; set; }
        public long CodigoAprobacion { get; set; }

        // Banderas de error (según las estadísticas requeridas por fecha)
        public bool ErrorNitEmisorInvalido { get; set; }
        public bool ErrorNitReceptorInvalido { get; set; }
        public bool ErrorNitEmisorInexistente { get; set; }
        public bool ErrorNitReceptorInexistente { get; set; }
        public bool ErrorIvaMalCalculado { get; set; }
        public bool ErrorTotalMalCalculado { get; set; }
        public bool ErrorReferenciaDuplicada { get; set; }
    }

    // Contenedor general para guardar/cargar todos los datos en el archivo de persistencia
    public class BaseDatosSat
    {
        // Diccionario de NIT -> Nombre del contribuyente (RTU)
        public Dictionary<string, string> Rtu { get; set; } = new();

        // Historial completo de facturas recibidas (aprobadas y con errores)
        public List<RegistroFactura> Facturas { get; set; } = new();

        // Diccionario de Fecha (yyyyMMdd) -> Último correlativo asignado en ese día
        public Dictionary<string, int> CorrelativosPorDia { get; set; } = new();
    }
}