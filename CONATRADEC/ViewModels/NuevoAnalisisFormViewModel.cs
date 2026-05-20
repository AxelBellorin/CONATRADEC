using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace CONATRADEC.ViewModels
{
    // ===============================================================
    // Clase: NuevoAnalisisFormViewModel
    // Descripción:
    //   ViewModel encargado de manejar la captura del formulario de
    //   análisis de suelo.
    //
    //   Hereda de GlobalService para usar:
    //     - IsBusy
    //     - CanRead
    //     - CanAdd
    //     - CanUpdate
    //     - CanDelete
    //     - LoadPagePermissions
    //     - OnPropertyChanged
    // ===============================================================
    public class NuevoAnalisisFormViewModel : GlobalService
    {
        // ===========================================================
        // =============== CAMPOS PRIVADOS DE LA CLASE ===============
        // ===========================================================

        private int? usuarioId;
        private string inicialesUsuario = string.Empty;
        private string nombreCompletoUsuario = string.Empty;
        private string correoUsuario = string.Empty;
        private string urlImagenUsuario = string.Empty;

        private string textoBusquedaTerreno = string.Empty;
        private TerrenoAnalisisResponse? terrenoSeleccionado;

        private string tipoCultivoSeleccionado = string.Empty;
        private string tipoAnalisisSueloSeleccionado = string.Empty;
        private DateTime fechaAnalisisLaboratorio = DateTime.Today;

        private string laboratorio = string.Empty;
        private string identificadorAnalisisSuelo = string.Empty;
        private string cantidadQuintalesOro = string.Empty;
        private string tamanoFinca = string.Empty;
        private string tipoMuestra = "Suelo";


        // ===========================================================
        // ===================== CONSTRUCTOR =========================
        // ===========================================================

        public NuevoAnalisisFormViewModel()
        {
            ResultadosAnalisis = new ObservableCollection<ResultadoAnalisisItemViewModel>();

            Terrenos = new ObservableCollection<TerrenoAnalisisResponse>();
            TerrenosFiltrados = new ObservableCollection<TerrenoAnalisisResponse>();

            TiposCultivo = new ObservableCollection<string>();
            TiposAnalisisSuelo = new ObservableCollection<string>();

            BuscarTerrenoCommand = new Command(FiltrarTerrenos);

            SeleccionarTerrenoCommand = new Command<TerrenoAnalisisResponse>(
                terreno => SeleccionarTerreno(terreno)
            );

            EnviarAnalisisCommand = new Command(
                async () => await EnviarAnalisisAsync(),
                () => PuedeEnviar
            );

            CancelarCommand = new Command(
                async () => await CancelarAsync(),
                () => !IsBusy
            );
        }


        // ===========================================================
        // ===================== USUARIO =============================
        // ===========================================================

        public int? UsuarioId
        {
            get => usuarioId;
            set
            {
                usuarioId = value;
                OnPropertyChanged(nameof(UsuarioId));
            }
        }

        public string InicialesUsuario
        {
            get => inicialesUsuario;
            set
            {
                inicialesUsuario = value;
                OnPropertyChanged(nameof(InicialesUsuario));
            }
        }

        public string NombreCompletoUsuario
        {
            get => nombreCompletoUsuario;
            set
            {
                nombreCompletoUsuario = value;
                OnPropertyChanged(nameof(NombreCompletoUsuario));
            }
        }

        public string CorreoUsuario
        {
            get => correoUsuario;
            set
            {
                correoUsuario = value;
                OnPropertyChanged(nameof(CorreoUsuario));
            }
        }

        public string UrlImagenUsuario
        {
            get => urlImagenUsuario;
            set
            {
                urlImagenUsuario = value;
                OnPropertyChanged(nameof(UrlImagenUsuario));
                OnPropertyChanged(nameof(TieneImagenUsuario));
                OnPropertyChanged(nameof(NoTieneImagenUsuario));
            }
        }

        public bool TieneImagenUsuario => !string.IsNullOrWhiteSpace(UrlImagenUsuario);

        public bool NoTieneImagenUsuario => string.IsNullOrWhiteSpace(UrlImagenUsuario);


        // ===========================================================
        // ===================== TERRENO =============================
        // ===========================================================

        public ObservableCollection<TerrenoAnalisisResponse> Terrenos { get; }

        public ObservableCollection<TerrenoAnalisisResponse> TerrenosFiltrados { get; }

        public string TextoBusquedaTerreno
        {
            get => textoBusquedaTerreno;
            set
            {
                textoBusquedaTerreno = value;
                OnPropertyChanged(nameof(TextoBusquedaTerreno));
                FiltrarTerrenos();
            }
        }

        public TerrenoAnalisisResponse? TerrenoSeleccionado
        {
            get => terrenoSeleccionado;
            set
            {
                terrenoSeleccionado = value;
                OnPropertyChanged(nameof(TerrenoSeleccionado));
                OnPropertyChanged(nameof(TieneTerrenoSeleccionado));

                if (terrenoSeleccionado != null)
                {
                    CantidadQuintalesOro = terrenoSeleccionado.CantidadQuintalesOro?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
                    TamanoFinca = terrenoSeleccionado.TamanoFinca?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
                }

                RefrescarComandos();
            }
        }

        public bool TieneTerrenoSeleccionado => TerrenoSeleccionado != null;


        // ===========================================================
        // ===================== DATOS DEL ANÁLISIS ==================
        // ===========================================================

        public ObservableCollection<string> TiposCultivo { get; }

        public ObservableCollection<string> TiposAnalisisSuelo { get; }

        public string TipoCultivoSeleccionado
        {
            get => tipoCultivoSeleccionado;
            set
            {
                tipoCultivoSeleccionado = value;
                OnPropertyChanged(nameof(TipoCultivoSeleccionado));
                RefrescarComandos();
            }
        }

        public string TipoAnalisisSueloSeleccionado
        {
            get => tipoAnalisisSueloSeleccionado;
            set
            {
                tipoAnalisisSueloSeleccionado = value;
                OnPropertyChanged(nameof(TipoAnalisisSueloSeleccionado));
                RefrescarComandos();
            }
        }

        public DateTime FechaAnalisisLaboratorio
        {
            get => fechaAnalisisLaboratorio;
            set
            {
                fechaAnalisisLaboratorio = value;
                OnPropertyChanged(nameof(FechaAnalisisLaboratorio));
            }
        }

        public string Laboratorio
        {
            get => laboratorio;
            set
            {
                laboratorio = value;
                OnPropertyChanged(nameof(Laboratorio));
                RefrescarComandos();
            }
        }

        public string IdentificadorAnalisisSuelo
        {
            get => identificadorAnalisisSuelo;
            set
            {
                identificadorAnalisisSuelo = value;
                OnPropertyChanged(nameof(IdentificadorAnalisisSuelo));
                RefrescarComandos();
            }
        }

        public string CantidadQuintalesOro
        {
            get => cantidadQuintalesOro;
            set
            {
                cantidadQuintalesOro = value;
                OnPropertyChanged(nameof(CantidadQuintalesOro));
                RefrescarComandos();
            }
        }

        public string TamanoFinca
        {
            get => tamanoFinca;
            set
            {
                tamanoFinca = value;
                OnPropertyChanged(nameof(TamanoFinca));
                RefrescarComandos();
            }
        }

        public string TipoMuestra
        {
            get => tipoMuestra;
            set
            {
                tipoMuestra = value;
                OnPropertyChanged(nameof(TipoMuestra));
                OnPropertyChanged(nameof(TextoTipoMuestra));
            }
        }

        public string TextoTipoMuestra => $"Tipo de muestra: {TipoMuestra}";


        // ===========================================================
        // ===================== RESULTADOS ==========================
        // ===========================================================

        public ObservableCollection<ResultadoAnalisisItemViewModel> ResultadosAnalisis { get; }


        // ===========================================================
        // ===================== COMANDOS ============================
        // ===========================================================

        public Command BuscarTerrenoCommand { get; }

        public Command<TerrenoAnalisisResponse> SeleccionarTerrenoCommand { get; }

        public Command EnviarAnalisisCommand { get; }

        public Command CancelarCommand { get; }

        public bool PuedeEnviar => !IsBusy && CanAdd;


        // ===========================================================
        // ================= MÉTODOS DE INICIALIZACIÓN ===============
        // ===========================================================

        public async Task InicializarAsync(bool forceReload = false)
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                RefrescarComandos();

                CargarDatosUsuario();

                if (forceReload || TiposCultivo.Count == 0 || TiposAnalisisSuelo.Count == 0)
                {
                    CargarCatalogosFormulario();
                }

                if (forceReload || Terrenos.Count == 0)
                {
                    CargarTerrenosPrueba();
                }

                if (forceReload || ResultadosAnalisis.Count == 0)
                {
                    CargarParametrosAnalisis();
                }
            }
            catch (Exception ex)
            {
                await MostrarMensajeAsync("Error", $"No se pudo cargar el formulario: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                RefrescarComandos();
            }
        }

        private void CargarDatosUsuario()
        {
            string usuarioIdTexto = Preferences.Get(SessionKeys.KeyUserId, "0");

            if (int.TryParse(usuarioIdTexto, out int idUsuario))
                UsuarioId = idUsuario;
            else
                UsuarioId = 0;

            NombreCompletoUsuario = Preferences.Get(SessionKeys.KeyNombreCompletoUsuario, string.Empty);
            CorreoUsuario = Preferences.Get(SessionKeys.KeyCorreoUsuario, string.Empty);
            UrlImagenUsuario = Preferences.Get(SessionKeys.KeyUrlImagenUsuario, string.Empty);

            InicialesUsuario = ObtenerIniciales(NombreCompletoUsuario);
            TipoMuestra = "Suelo";
        }

        private void CargarCatalogosFormulario()
        {
            TiposCultivo.Clear();
            TiposCultivo.Add("Café");

            TiposAnalisisSuelo.Clear();
            TiposAnalisisSuelo.Add("Análisis químico de suelo");
            TiposAnalisisSuelo.Add("Análisis físico de suelo");
            TiposAnalisisSuelo.Add("Análisis completo de suelo");

            TipoCultivoSeleccionado = "Café";
            TipoAnalisisSueloSeleccionado = "Análisis químico de suelo";
            FechaAnalisisLaboratorio = DateTime.Today;
        }

        private void CargarTerrenosPrueba()
        {
            Terrenos.Clear();

            string nombreCliente = string.IsNullOrWhiteSpace(NombreCompletoUsuario)
                ? "Cliente no definido"
                : NombreCompletoUsuario;

            Terrenos.Add(new TerrenoAnalisisResponse
            {
                TerrenoId = 1,
                UsuarioId = UsuarioId,
                CodigoTerreno = "TER-001",
                NombreTerreno = "Lote El Porvenir",
                NombreCliente = nombreCliente,
                CantidadQuintalesOro = 25,
                TamanoFinca = 3.5m
            });

            Terrenos.Add(new TerrenoAnalisisResponse
            {
                TerrenoId = 2,
                UsuarioId = UsuarioId,
                CodigoTerreno = "TER-002",
                NombreTerreno = "Lote La Esperanza",
                NombreCliente = nombreCliente,
                CantidadQuintalesOro = 40,
                TamanoFinca = 5m
            });

            FiltrarTerrenos();
        }

        private void CargarParametrosAnalisis()
        {
            ResultadosAnalisis.Clear();

            ResultadosAnalisis.Add(new ResultadoAnalisisItemViewModel
            {
                CodigoParametro = "PH",
                NombreParametro = "pH",
                PlaceholderValor = "Ejemplo: 5.8",
                UnidadesMedida = new ObservableCollection<string>
                {
                    "pH",
                    "sin unidad"
                },
                UnidadSeleccionada = "pH"
            });

            ResultadosAnalisis.Add(new ResultadoAnalisisItemViewModel
            {
                CodigoParametro = "MATERIA_ORGANICA",
                NombreParametro = "Materia Orgánica",
                PlaceholderValor = "Ejemplo: 3.2",
                UnidadesMedida = new ObservableCollection<string>
                {
                    "%",
                    "g/kg",
                    "mg/kg",
                    "ppm"
                },
                UnidadSeleccionada = "%"
            });

            ResultadosAnalisis.Add(new ResultadoAnalisisItemViewModel
            {
                CodigoParametro = "ACIDEZ_TOTAL",
                NombreParametro = "Acidez Total",
                PlaceholderValor = "Ejemplo: 0.5",
                UnidadesMedida = new ObservableCollection<string>
                {
                    "meq/100g",
                    "cmol/kg"
                },
                UnidadSeleccionada = "meq/100g"
            });

            ResultadosAnalisis.Add(new ResultadoAnalisisItemViewModel
            {
                CodigoParametro = "NITROGENO",
                NombreParametro = "Nitrógeno",
                PlaceholderValor = "Ejemplo: 0.05",
                UnidadesMedida = new ObservableCollection<string>
                {
                    "%",
                    "ppm",
                    "mg/kg"
                },
                UnidadSeleccionada = "%"
            });

            ResultadosAnalisis.Add(new ResultadoAnalisisItemViewModel
            {
                CodigoParametro = "FOSFORO_DISPONIBLE",
                NombreParametro = "Fósforo disponible",
                PlaceholderValor = "Ejemplo: 18",
                UnidadesMedida = new ObservableCollection<string>
                {
                    "mg/kg",
                    "ppm",
                    "g/kg"
                },
                UnidadSeleccionada = "mg/kg"
            });

            ResultadosAnalisis.Add(new ResultadoAnalisisItemViewModel
            {
                CodigoParametro = "POTASIO",
                NombreParametro = "Potasio",
                PlaceholderValor = "Ejemplo: 120",
                UnidadesMedida = new ObservableCollection<string>
                {
                    "mg/kg",
                    "ppm"
                },
                UnidadSeleccionada = "mg/kg"
            });

            ResultadosAnalisis.Add(new ResultadoAnalisisItemViewModel
            {
                CodigoParametro = "CALCIO",
                NombreParametro = "Calcio",
                PlaceholderValor = "Ejemplo: 850",
                UnidadesMedida = new ObservableCollection<string>
                {
                    "mg/kg",
                    "g/kg"
                },
                UnidadSeleccionada = "mg/kg"
            });

            ResultadosAnalisis.Add(new ResultadoAnalisisItemViewModel
            {
                CodigoParametro = "MAGNESIO",
                NombreParametro = "Magnesio",
                PlaceholderValor = "Ejemplo: 160",
                UnidadesMedida = new ObservableCollection<string>
                {
                    "mg/kg",
                    "g/kg"
                },
                UnidadSeleccionada = "mg/kg"
            });
        }


        // ===========================================================
        // ===================== TERRENOS ============================
        // ===========================================================

        private void FiltrarTerrenos()
        {
            TerrenosFiltrados.Clear();

            string texto = TextoBusquedaTerreno?.Trim().ToLower() ?? string.Empty;

            IEnumerable<TerrenoAnalisisResponse> lista;

            if (string.IsNullOrWhiteSpace(texto))
            {
                lista = Terrenos;
            }
            else
            {
                lista = Terrenos.Where(t =>
                    (t.NombreCliente ?? string.Empty).ToLower().Contains(texto) ||
                    (t.CodigoTerreno ?? string.Empty).ToLower().Contains(texto) ||
                    (t.NombreTerreno ?? string.Empty).ToLower().Contains(texto)
                );
            }

            foreach (var item in lista)
                TerrenosFiltrados.Add(item);
        }

        private void SeleccionarTerreno(TerrenoAnalisisResponse? terreno)
        {
            if (terreno == null)
                return;

            TerrenoSeleccionado = terreno;

            textoBusquedaTerreno = $"{terreno.CodigoTerreno} - {terreno.NombreTerreno}";
            OnPropertyChanged(nameof(TextoBusquedaTerreno));

            TerrenosFiltrados.Clear();
            TerrenosFiltrados.Add(terreno);
        }


        // ===========================================================
        // ===================== ENVIAR ANÁLISIS =====================
        // ===========================================================

        private async Task EnviarAnalisisAsync()
        {
            if (IsBusy)
                return;

            if (!CanAdd)
            {
                await MostrarMensajeAsync("Acceso denegado", "No tiene permisos para registrar análisis.");
                return;
            }

            try
            {
                IsBusy = true;
                RefrescarComandos();

                if (UsuarioId == null || UsuarioId <= 0)
                {
                    await MostrarMensajeAsync("Sesión", "No se encontró el usuario autenticado.");
                    return;
                }

                if (TerrenoSeleccionado == null)
                {
                    await MostrarMensajeAsync("Validación", "Debe seleccionar un cliente/terreno.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(TipoCultivoSeleccionado))
                {
                    await MostrarMensajeAsync("Validación", "Debe seleccionar el tipo de cultivo.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(TipoAnalisisSueloSeleccionado))
                {
                    await MostrarMensajeAsync("Validación", "Debe seleccionar el tipo de análisis de suelo.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(Laboratorio))
                {
                    await MostrarMensajeAsync("Validación", "Debe ingresar el laboratorio del análisis.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(IdentificadorAnalisisSuelo))
                {
                    await MostrarMensajeAsync("Validación", "Debe ingresar el identificador del análisis de suelo.");
                    return;
                }

                if (!TryParseDecimal(CantidadQuintalesOro, out decimal quintalesOro))
                {
                    await MostrarMensajeAsync("Validación", "La cantidad de quintales oro no es válida.");
                    return;
                }

                if (!TryParseDecimal(TamanoFinca, out decimal tamanoFincaDecimal))
                {
                    await MostrarMensajeAsync("Validación", "El tamaño de la finca no es válido.");
                    return;
                }

                var resultados = new List<ResultadoAnalisisRequest>();

                foreach (var item in ResultadosAnalisis)
                {
                    if (string.IsNullOrWhiteSpace(item.Valor))
                    {
                        await MostrarMensajeAsync("Validación", $"Debe ingresar el valor para {item.NombreParametro}.");
                        return;
                    }

                    if (!TryParseDecimal(item.Valor, out decimal valorConvertido))
                    {
                        await MostrarMensajeAsync("Validación", $"El valor de {item.NombreParametro} no es válido.");
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(item.UnidadSeleccionada))
                    {
                        await MostrarMensajeAsync("Validación", $"Debe seleccionar la unidad de {item.NombreParametro}.");
                        return;
                    }

                    resultados.Add(new ResultadoAnalisisRequest
                    {
                        CodigoParametro = item.CodigoParametro,
                        NombreParametro = item.NombreParametro,
                        Valor = valorConvertido,
                        UnidadMedida = item.UnidadSeleccionada
                    });
                }

                var request = new NuevoAnalisisRequest
                {
                    UsuarioId = UsuarioId,
                    TerrenoId = TerrenoSeleccionado.TerrenoId,
                    NombreCliente = TerrenoSeleccionado.NombreCliente,
                    CodigoTerreno = TerrenoSeleccionado.CodigoTerreno,
                    NombreTerreno = TerrenoSeleccionado.NombreTerreno,
                    TipoCultivo = TipoCultivoSeleccionado,
                    TipoAnalisisSuelo = TipoAnalisisSueloSeleccionado,
                    FechaAnalisisLaboratorio = FechaAnalisisLaboratorio,
                    Laboratorio = Laboratorio.Trim(),
                    IdentificadorAnalisisSuelo = IdentificadorAnalisisSuelo.Trim(),
                    CantidadQuintalesOro = quintalesOro,
                    TamanoFinca = tamanoFincaDecimal,
                    TipoMuestra = TipoMuestra,
                    Resultados = resultados
                };

                /*
                 * Aquí ya queda armado el Request completo.
                 *
                 * Luego conectamos con el servicio API:
                 *
                 * NuevoAnalisisResponse response =
                 *     await nuevoAnalisisApiService.CrearAsync(request);
                 */

                await MostrarMensajeAsync("Correcto", "El análisis fue capturado correctamente.");
            }
            catch (Exception ex)
            {
                await MostrarMensajeAsync("Error", $"No se pudo enviar el análisis: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                RefrescarComandos();
            }
        }

        private async Task CancelarAsync()
        {
            if (IsBusy)
                return;

            await Shell.Current.GoToAsync("..");
        }


        // ===========================================================
        // ===================== MÉTODOS AUXILIARES ==================
        // ===========================================================

        private void RefrescarComandos()
        {
            OnPropertyChanged(nameof(PuedeEnviar));
            EnviarAnalisisCommand.ChangeCanExecute();
            CancelarCommand.ChangeCanExecute();
        }

        private static bool TryParseDecimal(string valor, out decimal resultado)
        {
            if (decimal.TryParse(valor, NumberStyles.Any, CultureInfo.CurrentCulture, out resultado))
                return true;

            if (decimal.TryParse(valor, NumberStyles.Any, CultureInfo.InvariantCulture, out resultado))
                return true;

            string valorNormalizado = valor.Replace(",", ".");

            return decimal.TryParse(
                valorNormalizado,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out resultado
            );
        }

        private static string ObtenerIniciales(string? nombreCompleto)
        {
            if (string.IsNullOrWhiteSpace(nombreCompleto))
                return "US";

            var partes = nombreCompleto
                .Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (partes.Length == 1)
                return partes[0].Substring(0, Math.Min(2, partes[0].Length)).ToUpper();

            string inicialNombre = partes[0].Substring(0, 1);
            string inicialApellido = partes[1].Substring(0, 1);

            return $"{inicialNombre}{inicialApellido}".ToUpper();
        }

        private static async Task MostrarMensajeAsync(string titulo, string mensaje)
        {
            if (Application.Current?.MainPage != null)
                await Application.Current.MainPage.DisplayAlert(titulo, mensaje, "Aceptar");
        }
    }
}