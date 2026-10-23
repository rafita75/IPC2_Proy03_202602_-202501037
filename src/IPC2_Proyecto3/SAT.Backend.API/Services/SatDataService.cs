using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using SAT.Backend.API.Models;

namespace SAT.Backend.API.Services
{
    public class SatDataService
    {
        private readonly ValidadorService _validador;
        private readonly string _carpetaDatos;
        private readonly string _archivoRtu;
        private readonly string _archivoFacturas;
        private readonly string _archivoCorrelativos;
        private readonly object _lock = new object();

        private Dictionary<string, string> _rtu = new();
        private List<RegistroFactura> _facturas = new();
        private Dictionary<string, int> _correlativosPorDia = new();

        public SatDataService(ValidadorService validador, IWebHostEnvironment env)
        {
            _validador = validador;
            _carpetaDatos = Path.Combine(env.ContentRootPath, "Data");
            _archivoRtu = Path.Combine(_carpetaDatos, "rtu.json");
            _archivoFacturas = Path.Combine(_carpetaDatos, "facturas.json");
            _archivoCorrelativos = Path.Combine(_carpetaDatos, "correlativos.json");

            CargarDatosDesdeArchivos();
        }

        private void CargarDatosDesdeArchivos()
        {
            lock (_lock)
            {
                if (!Directory.Exists(_carpetaDatos))
                    Directory.CreateDirectory(_carpetaDatos);

                if (File.Exists(_archivoRtu))
                {
                    string jsonRtu = File.ReadAllText(_archivoRtu);
                    _rtu = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonRtu) ?? new();
                }

                if (File.Exists(_archivoFacturas))
                {
                    string jsonFacturas = File.ReadAllText(_archivoFacturas);
                    _facturas = JsonSerializer.Deserialize<List<RegistroFactura>>(jsonFacturas) ?? new();
                }

                if (File.Exists(_archivoCorrelativos))
                {
                    string jsonCorr = File.ReadAllText(_archivoCorrelativos);
                    _correlativosPorDia = JsonSerializer.Deserialize<Dictionary<string, int>>(jsonCorr) ?? new();
                }
            }
        }

        private void GuardarDatosEnArchivos()
        {
            var opciones = new JsonSerializerOptions { WriteIndented = true };

            if (!Directory.Exists(_carpetaDatos))
                Directory.CreateDirectory(_carpetaDatos);

            File.WriteAllText(_archivoRtu, JsonSerializer.Serialize(_rtu, opciones));
            File.WriteAllText(_archivoFacturas, JsonSerializer.Serialize(_facturas, opciones));
            File.WriteAllText(_archivoCorrelativos, JsonSerializer.Serialize(_correlativosPorDia, opciones));
        }

        public void ResetearDatos()
        {
            lock (_lock)
            {
                _rtu.Clear();
                _facturas.Clear();
                _correlativosPorDia.Clear();
                GuardarDatosEnArchivos();
            }
        }

        public RespuestaRtuDto ProcesarActualizacionRtu(List<ContribuyenteDto> solicitudes)
        {
            var respuesta = new RespuestaRtuDto();

            if (solicitudes == null)
                return respuesta;

            lock (_lock)
            {
                foreach (var item in solicitudes)
                {
                    string nit = (item.Nit ?? string.Empty).Trim().ToUpperInvariant();
                    string nombre = (item.Nombre ?? string.Empty).Trim();

                    if (!_validador.EsNitValido(nit))
                    {
                        respuesta.NitsInvalidos++;
                        continue;
                    }

                    if (_rtu.ContainsKey(nit))
                    {
                        _rtu[nit] = nombre;
                        respuesta.ContribuyentesActualizados++;
                    }
                    else
                    {
                        _rtu.Add(nit, nombre);
                        respuesta.ContribuyentesNuevos++;
                    }
                }

                GuardarDatosEnArchivos();
            }

            return respuesta;
        }

        public RespuestaAutorizacionWrapperDto ProcesarAutorizaciones(List<SolicitudAutorizacionDto> solicitudes)
        {
            var resultado = new ResultadoAutorizacionesDto();

            if (solicitudes == null)
                return new RespuestaAutorizacionWrapperDto { ResultadoAutorizaciones = resultado };

            lock (_lock)
            {
                var referenciasExistentes = new HashSet<string>(_facturas.Select(f => f.Referencia));

                // Fecha de hoy en Guatemala (UTC-6) para el código de autorización yyyymmdd########
                string claveHoyGuatemala = DateTime.UtcNow.AddHours(-6).ToString("yyyyMMdd");

                foreach (var sol in solicitudes)
                {
                    string referencia = (sol.Referencia ?? string.Empty).Trim();

                    // Si TIEMPO no contiene fecha y hora válidas o la referencia excede 40 caracteres, se descarta por formato
                    if (!_validador.ExtraerFechaYHora(sol.Tiempo, out string fechaStr, out string horaStr, out _) ||
                        !_validador.EsReferenciaFormatoValido(referencia))
                    {
                        resultado.CantidadDescartadasPorFormato++;
                        continue;
                    }

                    string nitEmisor = (sol.NitEmisor ?? string.Empty).Trim().ToUpperInvariant();
                    string nitReceptor = (sol.NitReceptor ?? string.Empty).Trim().ToUpperInvariant();

                    var registro = new RegistroFactura
                    {
                        Fecha = fechaStr,
                        Hora = horaStr,
                        Referencia = referencia,
                        NitEmisor = nitEmisor,
                        NitReceptor = nitReceptor,
                        Valor = sol.Valor,
                        Iva = sol.Iva,
                        Total = sol.Total
                    };

                    // 1. Validar NIT Emisor
                    if (!_validador.EsNitValido(nitEmisor))
                    {
                        registro.ErrorNitEmisorInvalido = true;
                        resultado.CantidadRechazadasNitEmisorInvalido++;
                    }
                    else if (!_rtu.ContainsKey(nitEmisor))
                    {
                        registro.ErrorNitEmisorInexistente = true;
                        resultado.CantidadRechazadasNitEmisorInexistente++;
                    }

                    // 2. Validar NIT Receptor
                    if (!_validador.EsNitValido(nitReceptor))
                    {
                        registro.ErrorNitReceptorInvalido = true;
                        resultado.CantidadRechazadasNitReceptorInvalido++;
                    }
                    else if (!_rtu.ContainsKey(nitReceptor))
                    {
                        registro.ErrorNitReceptorInexistente = true;
                        resultado.CantidadRechazadasNitReceptorInexistente++;
                    }

                    // 3. Validar IVA y TOTAL
                    if (!_validador.EsIvaValido(sol.Valor, sol.Iva))
                    {
                        registro.ErrorIvaMalCalculado = true;
                        resultado.CantidadRechazadasIvaMalCalculado++;
                    }

                    if (!_validador.EsTotalValido(sol.Valor, sol.Iva, sol.Total))
                    {
                        registro.ErrorTotalMalCalculado = true;
                        resultado.CantidadRechazadasTotalMalCalculado++;
                    }

                    // 4. Validar Referencia duplicada
                    if (referenciasExistentes.Contains(referencia))
                    {
                        registro.ErrorReferenciaDuplicada = true;
                        resultado.CantidadRechazadasReferenciaDuplicada++;
                    }

                    referenciasExistentes.Add(referencia);

                    bool tieneErrores = registro.ErrorNitEmisorInvalido
                                     || registro.ErrorNitReceptorInvalido
                                     || registro.ErrorNitEmisorInexistente
                                     || registro.ErrorNitReceptorInexistente
                                     || registro.ErrorIvaMalCalculado
                                     || registro.ErrorTotalMalCalculado
                                     || registro.ErrorReferenciaDuplicada;

                    if (tieneErrores)
                    {
                        resultado.CantidadAutorizacionesRechazadas++;
                    }
                    else
                    {
                        registro.Aprobada = true;

                        if (!_correlativosPorDia.ContainsKey(claveHoyGuatemala))
                        {
                            _correlativosPorDia[claveHoyGuatemala] = 0;
                        }

                        _correlativosPorDia[claveHoyGuatemala]++;
                        int correlativoActual = _correlativosPorDia[claveHoyGuatemala];

                        string codigoStr = $"{claveHoyGuatemala}{correlativoActual:D8}";
                        registro.CodigoAprobacion = long.Parse(codigoStr);

                        resultado.CantidadAutorizacionesAprobadas++;
                        resultado.DetalleAprobaciones.Add(new DetalleAprobacionDto
                        {
                            Referencia = referencia,
                            CodigoAprobacion = registro.CodigoAprobacion
                        });
                    }

                    _facturas.Add(registro);
                }

                GuardarDatosEnArchivos();
            }

            return new RespuestaAutorizacionWrapperDto
            {
                ResultadoAutorizaciones = resultado
            };
        }

        public IReadOnlyDictionary<string, string> ObtenerRtu() => _rtu;
        public IReadOnlyList<RegistroFactura> ObtenerFacturas() => _facturas;
    }
}