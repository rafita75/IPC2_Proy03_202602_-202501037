using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SAT.Backend.API.Services
{
    public class ValidadorService
    {
        // Regex para NIT: de 1 a 20 dígitos (0-9) seguidos de 1 dígito verificador (0-9 o K)
        private static readonly Regex RegexNit = new Regex(@"^\d{1,20}[0-9K]$", RegexOptions.Compiled);

        // Regex para extraer fecha (dd/mm/yyyy) y hora (hh24:mi) ignorando lugares o texto extra
        private static readonly Regex RegexFechaHora = new Regex(@"(?<fecha>\d{2}/\d{2}/\d{4})\s+(?<hora>(?:[01]\d|2[0-3]):[0-5]\d)", RegexOptions.Compiled);

        /// <summary>
        /// Valida el formato y el dígito verificador del NIT usando el algoritmo Módulo 11.
        /// (En los ejemplos del PDF: "7378106" y "8338817" dan true; "5123456" da false).
        /// </summary>
        public bool EsNitValido(string nit)
        {
            if (string.IsNullOrWhiteSpace(nit))
                return false;

            nit = nit.Trim().ToUpperInvariant();

            // 1. Validar estructura XXXXXXV (1 a 20 números + verificador)
            if (!RegexNit.IsMatch(nit))
                return false;

            char caracterVerificador = nit[nit.Length - 1];
            string cuerpoNit = nit.Substring(0, nit.Length - 1);

            // 2. Multiplicar cada carácter por su posición de derecha a izquierda (empezando en 2)
            int suma = 0;
            int posicion = 2;

            for (int i = cuerpoNit.Length - 1; i >= 0; i--)
            {
                int digito = cuerpoNit[i] - '0';
                suma += digito * posicion;
                posicion++;
            }

            // 3. Módulo 11 de la sumatoria
            int modulo11 = suma % 11;

            // 4. A 11 restarle el resultado del punto 3
            int resta = 11 - modulo11;

            // 5. Módulo 11 del resultado del punto 4 (si es 10, el verificador es K)
            int resultadoFinal = resta % 11;
            char verificadorCalculado = (resultadoFinal == 10) ? 'K' : (char)('0' + resultadoFinal);

            return caracterVerificador == verificadorCalculado;
        }

        /// <summary>
        /// Extrae la fecha (dd/MM/yyyy) y hora (HH:mm) de la cadena TIEMPO usando expresiones regulares,
        /// descartando nombres de lugares o texto como "hrs.".
        /// </summary>
        public bool ExtraerFechaYHora(string tiempoRaw, out string fechaExtraida, out string horaExtraida, out DateTime fechaParseada)
        {
            fechaExtraida = string.Empty;
            horaExtraida = string.Empty;
            fechaParseada = DateTime.MinValue;

            if (string.IsNullOrWhiteSpace(tiempoRaw))
                return false;

            Match match = RegexFechaHora.Match(tiempoRaw);
            if (!match.Success)
                return false;

            fechaExtraida = match.Groups["fecha"].Value;
            horaExtraida = match.Groups["hora"].Value;

            // Verificar que sea una fecha calendario real (ej. que no acepte 31/02/2025)
            return DateTime.TryParseExact(
                fechaExtraida,
                "dd/MM/yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out fechaParseada);
        }

        /// <summary>
        /// Valida que la referencia no esté vacía y tenga máximo 40 posiciones.
        /// </summary>
        public bool EsReferenciaFormatoValido(string referencia)
        {
            return !string.IsNullOrWhiteSpace(referencia) && referencia.Trim().Length <= 40;
        }

        /// <summary>
        /// Valida que el IVA sea exactamente REDONDEAR(VALOR * 0.12, 2).
        /// </summary>
        public bool EsIvaValido(decimal valor, decimal iva)
        {
            if (valor <= 0) return false;
            decimal ivaCalculado = Math.Round(valor * 0.12m, 2, MidpointRounding.AwayFromZero);
            return iva == ivaCalculado;
        }

        /// <summary>
        /// Valida que el TOTAL sea exactamente VALOR + IVA.
        /// </summary>
        public bool EsTotalValido(decimal valor, decimal iva, decimal total)
        {
            if (valor <= 0) return false;
            return total == (valor + iva);
        }
    }
}