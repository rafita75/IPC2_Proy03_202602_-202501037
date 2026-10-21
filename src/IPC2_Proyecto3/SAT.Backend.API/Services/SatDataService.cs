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

        // Estructuras en memoria
        private Dictionary<string, string> _rtu = new();
        private List<RegistroFactura> _facturas = new();
        private Dictionary<string, int> _correlativosPorDia = new();

        public SatDataService(ValidadorService validador, IWebHostEnvironment env)
        {
            _validador = validador;

            // Definir la carpeta "Data" dentro del proyecto Backend
            _carpetaDatos = Path.Combine(env.ContentRootPath, "Data");
            _archivoRtu = Path.Combine(_carpetaDatos, "rtu.json");
            _archivoFacturas = Path.Combine(_carpetaDatos, "facturas.json");
            _archivoCorrelativos = Path.Combine(_carpetaDatos, "correlativos.json");

            CargarDatosDesdeArchivos();
        }

        // 1. PERSISTENCIA EN ARCHIVOS 

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

        /// <summary>
        /// Devuelve el sistema al estado inicial (sin datos).
        /// </summary>
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

        // 2. ACTUALIZACIÓN DEL RTU 

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

                    // Validar NIT con algoritmo Módulo 11
                    if (!_validador.EsNitValido(nit))
                    {
                        respuesta.NitsInvalidos++;
                        continue;
                    }

                    // Si ya existe en el RTU se actualiza, si no existe se crea
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

        //  3. AUTORIZACIÓN DE FACTURAS (DTE) 

        public RespuestaAutorizacionWrapperDto ProcesarAutorizaciones(List<SolicitudAutorizacionDto> solicitudes)
        {
            var resultado = new ResultadoAutorizacionesDto();

            if (solicitudes == null)
                return new RespuestaAutorizacionWrapperDto { ResultadoAutorizaciones = resultado };

            lock (_lock)
            {
                // Conjunto de referencias ya registradas para detectar duplicados rápido
                var referenciasExistentes = new HashSet<string>(
                    _facturas.Select(f => f.Referencia),
                    StringComparer.OrdinalIgnoreCase);

                foreach (var sol in solicitudes)
                {
                    // Extraer fecha y hora limpias con Regex
                    if (!_validador.ExtraerFechaYHora(sol.Tiempo, out string fechaStr, out string horaStr, out DateTime fechaDt))
                    {
                        // Si no trae una fecha reconocible, se asigna fecha actual por defecto para poder registrarla
                        fechaDt = DateTime.Today;
                        fechaStr = fechaDt.ToString("dd/MM/yyyy");
                        horaStr = "00:00";
                    }

                    string referencia = (sol.Referencia ?? string.Empty).Trim();
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

                    // 1. Validar NIT Emisor (Formato/Módulo 11 y existencia en RTU)
                    if (!_validador.EsNitValido(nitEmisor))
                    {
                        registro.ErrorNitEmisorInvalido = true;
                    }
                    else if (!_rtu.ContainsKey(nitEmisor))
                    {
                        registro.ErrorNitEmisorInexistente = true;
                        resultado.CantidadRechazadasNitEmisorInexistente++;
                    }

                    // 2. Validar NIT Receptor (Formato/Módulo 11 y existencia en RTU)
                    if (!_validador.EsNitValido(nitReceptor))
                    {
                        registro.ErrorNitReceptorInvalido = true;
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
                    }

                    if (!_validador.EsTotalValido(sol.Valor, sol.Iva, sol.Total))
                    {
                        registro.ErrorTotalMalCalculado = true;
                    }

                    // 4. Validar Referencia (formato máx 40 caracteres y que sea única)
                    if (!_validador.EsReferenciaFormatoValido(referencia) || referenciasExistentes.Contains(referencia))
                    {
                        registro.ErrorReferenciaDuplicada = true;
                    }

                    // Verificar si la factura no tuvo ningún error
                    bool tieneErrores = registro.ErrorNitEmisorInvalido
                                     || registro.ErrorNitReceptorInvalido
                                     || registro.ErrorNitEmisorInexistente
                                     || registro.ErrorNitReceptorInexistente
                                     || registro.ErrorIvaMalCalculado
                                     || registro.ErrorTotalMalCalculado
                                     || registro.ErrorReferenciaDuplicada;

                    if (!tieneErrores)
                    {
                        registro.Aprobada = true;

                        // Generar número de autorización: yyyymmdd########
                        string claveDia = fechaDt.ToString("yyyyMMdd");
                        if (!_correlativosPorDia.ContainsKey(claveDia))
                        {
                            _correlativosPorDia[claveDia] = 0;
                        }

                        _correlativosPorDia[claveDia]++;
                        int correlativoActual = _correlativosPorDia[claveDia];

                        string codigoStr = $"{claveDia}{correlativoActual:D8}";
                        registro.CodigoAprobacion = long.Parse(codigoStr);

                        resultado.CantidadAutorizacionesAprobadas++;
                        resultado.DetalleAprobaciones.Add(new DetalleAprobacionDto
                        {
                            Referencia = referencia,
                            CodigoAprobacion = registro.CodigoAprobacion
                        });
                    }

                    // Registrar la referencia vista y guardar la factura en el historial
                    if (!string.IsNullOrWhiteSpace(referencia))
                    {
                        referenciasExistentes.Add(referencia);
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

        // Métodos de acceso para las consultas que haremos en la Fase 3
        public IReadOnlyDictionary<string, string> ObtenerRtu() => _rtu;
        public IReadOnlyList<RegistroFactura> ObtenerFacturas() => _facturas;
    }
}