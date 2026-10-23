using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SAT.Backend.API.Services
{
    public class ValidadorService
    {
        // Regex para NIT: de 1 a 20 dígitos (0-9) seguidos de 1 dígito verificador (0-9 o K)
        private static readonly Regex RegexNit = new Regex(@"^\d{1,20}[0-9K]$", RegexOptions.Compiled);

        // Regex para extraer fecha (d/m/yyyy o dd/mm/yyyy) y hora (h:mm o hh:mm)
        private static readonly Regex RegexFechaHora = new Regex(@"(?<fecha>\b\d{1,2}/\d{1,2}/\d{4})\s+(?<hora>(?:[01]?\d|2[0-3]):[0-5]\d)\b", RegexOptions.Compiled);

        public bool EsNitValido(string nit)
        {
            if (string.IsNullOrWhiteSpace(nit))
                return false;

            nit = nit.Trim().ToUpperInvariant();

            if (!RegexNit.IsMatch(nit))
                return false;

            char caracterVerificador = nit[nit.Length - 1];
            string cuerpoNit = nit.Substring(0, nit.Length - 1);

            int suma = 0;
            int posicion = 2;

            for (int i = cuerpoNit.Length - 1; i >= 0; i--)
            {
                int digito = cuerpoNit[i] - '0';
                suma += digito * posicion;
                posicion++;
            }

            int modulo11 = suma % 11;
            int resta = 11 - modulo11;
            int resultadoFinal = resta % 11;
            char verificadorCalculado = (resultadoFinal == 10) ? 'K' : (char)('0' + resultadoFinal);

            return caracterVerificador == verificadorCalculado;
        }

        public bool ExtraerFechaYHora(string tiempoRaw, out string fechaNormalizada, out string horaNormalizada, out DateTime fechaParseada)
        {
            fechaNormalizada = string.Empty;
            horaNormalizada = string.Empty;
            fechaParseada = DateTime.MinValue;

            if (string.IsNullOrWhiteSpace(tiempoRaw))
                return false;

            Match match = RegexFechaHora.Match(tiempoRaw);
            if (!match.Success)
                return false;

            string fechaCandidata = match.Groups["fecha"].Value;
            string horaCandidata = match.Groups["hora"].Value;

            string[] formatosFecha = { "dd/MM/yyyy", "d/M/yyyy", "dd/M/yyyy", "d/MM/yyyy" };
            if (!DateTime.TryParseExact(
                fechaCandidata,
                formatosFecha,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out fechaParseada))
            {
                return false;
            }

            // Normalizar siempre a dd/MM/yyyy y HH:mm
            fechaNormalizada = fechaParseada.ToString("dd/MM/yyyy");
            string[] partesHora = horaCandidata.Split(':');
            horaNormalizada = $"{int.Parse(partesHora[0]):D2}:{partesHora[1]}";

            return true;
        }

        public bool EsReferenciaFormatoValido(string referencia)
        {
            return !string.IsNullOrWhiteSpace(referencia) && referencia.Trim().Length <= 40;
        }

        public bool EsIvaValido(decimal valor, decimal iva)
        {
            if (valor <= 0) return false;
            decimal ivaCalculado = Math.Round(valor * 0.12m, 2, MidpointRounding.AwayFromZero);
            return iva == ivaCalculado;
        }

        public bool EsTotalValido(decimal valor, decimal iva, decimal total)
        {
            if (valor <= 0) return false;
            return total == (valor + iva);
        }
    }
}