using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace CONATRADEC.Views
{
    /// <summary>
    /// Resuelve los tres niveles del Álbum Botánico para una sola fotografía.
    /// El analizador puede seleccionar o proponer; el aprobador confirma y,
    /// cuando corresponde, crea los niveles propuestos desde el mismo flujo.
    /// </summary>
    public partial class JerarquiaAlbumFotografiaPage :
        ContentPage
    {
        private readonly int diagnosticoId;
        private readonly string etapa;
        private readonly AlbumBotanicoApiService albumApi = new();
        private readonly AlbumJerarquiaApiService jerarquiaApi = new();
        private readonly TaskCompletionSource<bool> resultadoTcs = new();

        private CategoriaAlbumBotanicoResponse? categoriaSeleccionada;
        private SubcategoriaAlbumBotanicoResponse? subcategoriaSeleccionada;
        private AlbumRegistroJerarquiaResponse? fichaSeleccionada;
        private bool proponerCategoria;
        private bool proponerSubcategoria;
        private bool proponerFicha;
        private bool isBusy;
        private string categoriaPropuesta = string.Empty;
        private string subcategoriaPropuesta = string.Empty;
        private string fichaPropuesta = string.Empty;
        private string nombreCientifico = string.Empty;
        private string descripcion = string.Empty;
        private string sintomas = string.Empty;
        private string motivo = string.Empty;
        private bool inicializada;

        public JerarquiaAlbumFotografiaPage(
            int diagnosticoId,
            InspeccionFotoV2 fotografia,
            string etapa)
        {
            InitializeComponent();

            this.diagnosticoId = diagnosticoId;
            Fotografia = fotografia ??
                throw new ArgumentNullException(nameof(fotografia));
            this.etapa = NormalizarEtapa(etapa);
            JerarquiaActual = fotografia.JerarquiaAlbum;

            GuardarCommand = new Command(
                async () => await GuardarAsync(),
                () => !IsBusy);
            CancelarCommand = new Command(
                async () => await CerrarAsync(false),
                () => !IsBusy);

            BindingContext = this;
        }

        public InspeccionFotoV2 Fotografia { get; }
        public JerarquiaDiagnosticoFotoResponse? JerarquiaActual { get; }
        public ObservableCollection<CategoriaAlbumBotanicoResponse> Categorias { get; } = [];
        public ObservableCollection<SubcategoriaAlbumBotanicoResponse> Subcategorias { get; } = [];
        public ObservableCollection<AlbumRegistroJerarquiaResponse> Fichas { get; } = [];
        public Command GuardarCommand { get; }
        public Command CancelarCommand { get; }
        public Task<bool> ResultadoTask => resultadoTcs.Task;

        public bool EsAprobador => etapa == "APROBADOR";
        public bool TieneJerarquiaActual => JerarquiaActual?.TieneClasificacion == true;
        public string EtapaTexto => EsAprobador
            ? "Decisión del aprobador · los niveles propuestos pueden convertirse en catálogo oficial"
            : "Análisis humano · seleccione niveles existentes o proponga únicamente los que falten";
        public string AyudaEtapa => EsAprobador
            ? "Al guardar, el sistema dejará una categoría, una subcategoría y una ficha oficiales. Si un nivel fue propuesto y usted tiene permiso para administrar el álbum, se creará dentro de esta misma operación."
            : "La propuesta no crea catálogos automáticamente. El aprobador revisará los niveles faltantes antes de incorporarlos al Álbum Botánico.";
        public string TextoGuardar => EsAprobador
            ? "Confirmar jerarquía"
            : "Guardar clasificación";

        public new bool IsBusy
        {
            get => isBusy;
            private set
            {
                if (isBusy == value)
                    return;
                isBusy = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(NotIsBusy));
                GuardarCommand.ChangeCanExecute();
                CancelarCommand.ChangeCanExecute();
            }
        }

        public bool NotIsBusy => !IsBusy;
        public bool UsarCategoriaExistente => !ProponerCategoria;
        public bool UsarSubcategoriaExistente => !ProponerSubcategoria;
        public bool UsarFichaExistente => !ProponerFicha;
        public bool RequiereDescripcionCreacion => EsAprobador && ProponerFicha;

        public CategoriaAlbumBotanicoResponse? CategoriaSeleccionada
        {
            get => categoriaSeleccionada;
            set
            {
                if (ReferenceEquals(categoriaSeleccionada, value))
                    return;
                categoriaSeleccionada = value;
                OnPropertyChanged();
                _ = CargarSubcategoriasAsync(value?.CategoriaAlbumBotanicoId);
            }
        }

        public SubcategoriaAlbumBotanicoResponse? SubcategoriaSeleccionada
        {
            get => subcategoriaSeleccionada;
            set
            {
                if (ReferenceEquals(subcategoriaSeleccionada, value))
                    return;
                subcategoriaSeleccionada = value;
                OnPropertyChanged();
                _ = CargarFichasAsync(
                    CategoriaSeleccionada?.CategoriaAlbumBotanicoId,
                    value?.SubcategoriaAlbumBotanicoId);
            }
        }

        public AlbumRegistroJerarquiaResponse? FichaSeleccionada
        {
            get => fichaSeleccionada;
            set
            {
                if (ReferenceEquals(fichaSeleccionada, value))
                    return;
                fichaSeleccionada = value;
                OnPropertyChanged();
                if (value != null && string.IsNullOrWhiteSpace(NombreCientifico))
                    NombreCientifico = value.NombreCientifico ?? string.Empty;
            }
        }

        public bool ProponerCategoria
        {
            get => proponerCategoria;
            set
            {
                if (proponerCategoria == value)
                    return;
                proponerCategoria = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UsarCategoriaExistente));
                if (value)
                {
                    CategoriaSeleccionada = null;
                    ProponerSubcategoria = true;
                    ProponerFicha = true;
                }
            }
        }

        public bool ProponerSubcategoria
        {
            get => proponerSubcategoria;
            set
            {
                if (proponerSubcategoria == value)
                    return;
                proponerSubcategoria = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UsarSubcategoriaExistente));
                if (value)
                {
                    SubcategoriaSeleccionada = null;
                    ProponerFicha = true;
                }
            }
        }

        public bool ProponerFicha
        {
            get => proponerFicha;
            set
            {
                if (proponerFicha == value)
                    return;
                proponerFicha = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UsarFichaExistente));
                OnPropertyChanged(nameof(RequiereDescripcionCreacion));
                if (value)
                    FichaSeleccionada = null;
            }
        }

        public string CategoriaPropuesta { get => categoriaPropuesta; set => Set(ref categoriaPropuesta, value); }
        public string SubcategoriaPropuesta { get => subcategoriaPropuesta; set => Set(ref subcategoriaPropuesta, value); }
        public string FichaPropuesta { get => fichaPropuesta; set => Set(ref fichaPropuesta, value); }
        public string NombreCientifico { get => nombreCientifico; set => Set(ref nombreCientifico, value); }
        public string Descripcion { get => descripcion; set => Set(ref descripcion, value); }
        public string Sintomas { get => sintomas; set => Set(ref sintomas, value); }
        public string Motivo { get => motivo; set => Set(ref motivo, value); }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (!inicializada)
                await InicializarAsync();
        }

        protected override bool OnBackButtonPressed()
        {
            if (IsBusy)
                return true;

            _ = CerrarAsync(false);
            return true;
        }

        private async Task InicializarAsync()
        {
            inicializada = true;
            IsBusy = true;

            try
            {
                ApiResult<List<CategoriaAlbumBotanicoResponse>> resultado =
                    await albumApi.GetCategoriasAsync(false);

                if (!resultado.Success)
                    throw new InvalidOperationException(resultado.Message);

                Categorias.Clear();
                foreach (CategoriaAlbumBotanicoResponse item in resultado.Data ?? [])
                    Categorias.Add(item);

                PrecargarTextos();

                int? categoriaId = JerarquiaActual?.CategoriaAlbumBotanicoId ??
                    Fotografia.ResultadoIA?.CategoriaAlbumBotanicoIdSugerida;

                CategoriaAlbumBotanicoResponse? categoria = Categorias
                    .FirstOrDefault(item =>
                        item.CategoriaAlbumBotanicoId == categoriaId);

                if (categoria != null &&
                    JerarquiaActual?.CategoriaEsPropuesta != true)
                {
                    categoriaSeleccionada = categoria;
                    OnPropertyChanged(nameof(CategoriaSeleccionada));
                    await CargarSubcategoriasAsync(categoria.CategoriaAlbumBotanicoId);
                }
                else if (!string.IsNullOrWhiteSpace(
                             JerarquiaActual?.Categoria))
                {
                    ProponerCategoria = JerarquiaActual.CategoriaEsPropuesta;
                    CategoriaPropuesta = JerarquiaActual.Categoria;
                }
                else
                {
                    SugerirCategoriaDesdeIA();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "No fue posible cargar la jerarquía",
                    ex.Message,
                    "Aceptar");
                await CerrarAsync(false);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void PrecargarTextos()
        {
            JerarquiaDiagnosticoFotoResponse? actual = JerarquiaActual;
            InspeccionFotoResultadoIAV2? ia = Fotografia.ResultadoIA;

            CategoriaPropuesta = actual?.Categoria ??
                ia?.CategoriaAlbumPropuesta ?? string.Empty;
            SubcategoriaPropuesta = actual?.Subcategoria ??
                SugerirSubcategoria(ia) ?? string.Empty;
            FichaPropuesta = actual?.Ficha ??
                ia?.ClasificacionAlbumPropuesta ??
                ia?.DiagnosticoVisible ?? string.Empty;
            NombreCientifico = actual?.NombreCientifico ??
                ia?.NombreCientificoSugerido ?? string.Empty;
            Motivo = actual?.Motivo ??
                ia?.MotivoAlbumPropuesta ?? string.Empty;
            Descripcion = ia?.ResumenImagen ?? string.Empty;
            Sintomas = string.Join(
                Environment.NewLine,
                ia?.SintomasVisibles ?? []);

            ProponerCategoria = actual?.CategoriaEsPropuesta == true;
            ProponerSubcategoria = actual?.SubcategoriaEsPropuesta == true;
            ProponerFicha = actual?.FichaEsPropuesta == true ||
                ia?.RequiereGestionAlbum == true;
        }

        private async Task CargarSubcategoriasAsync(int? categoriaId)
        {
            Subcategorias.Clear();
            Fichas.Clear();
            subcategoriaSeleccionada = null;
            fichaSeleccionada = null;
            OnPropertyChanged(nameof(SubcategoriaSeleccionada));
            OnPropertyChanged(nameof(FichaSeleccionada));

            if (ProponerCategoria || categoriaId is not > 0)
                return;

            try
            {
                ApiResult<List<SubcategoriaAlbumBotanicoResponse>> resultado =
                    await jerarquiaApi.GetSubcategoriasAsync(categoriaId, false);

                if (!resultado.Success)
                    throw new InvalidOperationException(resultado.Message);

                foreach (SubcategoriaAlbumBotanicoResponse item in resultado.Data ?? [])
                    Subcategorias.Add(item);

                int? subcategoriaId =
                    JerarquiaActual?.SubcategoriaAlbumBotanicoId;

                SubcategoriaAlbumBotanicoResponse? seleccion =
                    Subcategorias.FirstOrDefault(item =>
                        item.SubcategoriaAlbumBotanicoId == subcategoriaId);

                if (seleccion != null &&
                    JerarquiaActual?.SubcategoriaEsPropuesta != true)
                {
                    subcategoriaSeleccionada = seleccion;
                    OnPropertyChanged(nameof(SubcategoriaSeleccionada));
                    await CargarFichasAsync(
                        categoriaId,
                        seleccion.SubcategoriaAlbumBotanicoId);
                }
                else if (Subcategorias.Count == 0)
                {
                    ProponerSubcategoria = true;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Subcategorías", ex.Message, "Aceptar");
            }
        }

        private async Task CargarFichasAsync(
            int? categoriaId,
            int? subcategoriaId)
        {
            Fichas.Clear();
            fichaSeleccionada = null;
            OnPropertyChanged(nameof(FichaSeleccionada));

            if (ProponerCategoria || ProponerSubcategoria ||
                categoriaId is not > 0 || subcategoriaId is not > 0)
            {
                return;
            }

            try
            {
                ApiResult<List<AlbumRegistroJerarquiaResponse>> resultado =
                    await jerarquiaApi.GetJerarquiaRegistrosAsync(
                        categoriaId: categoriaId,
                        subcategoriaId: subcategoriaId,
                        incluirInactivos: false);

                if (!resultado.Success)
                    throw new InvalidOperationException(resultado.Message);

                foreach (AlbumRegistroJerarquiaResponse item in resultado.Data ?? [])
                    Fichas.Add(item);

                int? fichaId = JerarquiaActual?.AlbumBotanicoCafeId ??
                    Fotografia.ResultadoIA?.AlbumBotanicoCafeIdSugerido;

                AlbumRegistroJerarquiaResponse? seleccion = Fichas
                    .FirstOrDefault(item => item.AlbumBotanicoCafeId == fichaId);

                if (seleccion != null &&
                    JerarquiaActual?.FichaEsPropuesta != true)
                {
                    fichaSeleccionada = seleccion;
                    OnPropertyChanged(nameof(FichaSeleccionada));
                    ProponerFicha = false;
                }
                else if (Fichas.Count == 0)
                {
                    ProponerFicha = true;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Fichas", ex.Message, "Aceptar");
            }
        }

        private void SugerirCategoriaDesdeIA()
        {
            string categoriaIa = Fotografia.ResultadoIA?.CategoriaPrincipal ?? string.Empty;
            string buscada = categoriaIa switch
            {
                "ENFERMEDAD" => "Enfermedades",
                "PLAGA" => "Plagas",
                "ALTERACION_NUTRICIONAL" => "Alteraciones nutricionales",
                "ESTRES_ABIOTICO" => "Estrés abiótico",
                "DANO_MECANICO" => "Daños mecánicos",
                "NO_APLICA" when Fotografia.ResultadoIA?.EsAparentementeSana == true => "Plantas sanas",
                _ => string.Empty
            };

            CategoriaAlbumBotanicoResponse? categoria = Categorias
                .FirstOrDefault(item => item.NombreCategoria.Contains(
                    buscada,
                    StringComparison.OrdinalIgnoreCase));

            if (categoria != null)
                CategoriaSeleccionada = categoria;
            else
            {
                ProponerCategoria = true;
                CategoriaPropuesta = string.IsNullOrWhiteSpace(buscada)
                    ? Fotografia.ResultadoIA?.CategoriaAlbumPropuesta ?? string.Empty
                    : buscada;
            }
        }

        private static string? SugerirSubcategoria(
            InspeccionFotoResultadoIAV2? ia)
        {
            if (ia == null)
                return null;

            string texto = string.Join(
                " ",
                new[]
                {
                    ia.DiagnosticoProbable,
                    ia.TipoDiagnostico,
                    string.Join(" ", ia.CategoriasSecundarias)
                }).ToLowerInvariant();

            if (ia.EsAparentementeSana)
            {
                return ia.PartePlanta.ToUpperInvariant() switch
                {
                    "HOJA" or "HOJAS" => "Hojas sanas",
                    "FRUTO" or "FRUTOS" => "Frutos sanos",
                    "TALLO" or "RAMA" or "RAMAS" => "Tallos y ramas sanos",
                    _ => "Planta completa sana"
                };
            }

            if (ia.CategoriaPrincipal == "PLAGA")
            {
                if (texto.Contains("ácar") || texto.Contains("acar")) return "Ácaros";
                if (texto.Contains("nematod")) return "Nematodos";
                if (texto.Contains("babosa") || texto.Contains("caracol")) return "Moluscos";
                return "Insectos";
            }

            if (ia.CategoriaPrincipal == "ENFERMEDAD")
            {
                if (texto.Contains("bacter")) return "Enfermedades bacterianas";
                if (texto.Contains("virus") || texto.Contains("viral")) return "Enfermedades virales";
                return "Enfermedades fúngicas";
            }

            if (ia.CategoriaPrincipal == "ALTERACION_NUTRICIONAL")
            {
                string micro = "hierro zinc boro cobre manganeso molibdeno cloro niquel";
                return micro.Split(' ').Any(texto.Contains)
                    ? "Deficiencias de micronutrientes"
                    : "Deficiencias de macronutrientes";
            }

            return ia.CategoriaPrincipal == "ESTRES_ABIOTICO"
                ? "Otros estreses abióticos"
                : null;
        }

        private async Task GuardarAsync()
        {
            if (IsBusy)
                return;

            if (!ValidarFormulario(out string error))
            {
                if (!string.IsNullOrWhiteSpace(error))
                {
                    await DisplayAlert(
                        "Revise la clasificación",
                        error,
                        "Aceptar");
                }

                return;
            }

            bool confirmar = await DisplayAlert(
                EsAprobador ? "Confirmar jerarquía" : "Guardar propuesta",
                EsAprobador
                    ? "La fotografía quedará vinculada con una categoría, subcategoría y ficha oficiales. Los niveles propuestos se crearán únicamente si aún no existen."
                    : "La clasificación quedará registrada para que el aprobador revise los niveles propuestos.",
                "Continuar",
                "Cancelar");

            if (!confirmar)
                return;

            IsBusy = true;
            try
            {
                var request = new ResolverJerarquiaAlbumRequest
                {
                    Etapa = etapa,
                    CategoriaAlbumBotanicoId = ProponerCategoria
                        ? null
                        : CategoriaSeleccionada?.CategoriaAlbumBotanicoId,
                    SubcategoriaAlbumBotanicoId = ProponerSubcategoria
                        ? null
                        : SubcategoriaSeleccionada?.SubcategoriaAlbumBotanicoId,
                    AlbumBotanicoCafeId = ProponerFicha
                        ? null
                        : FichaSeleccionada?.AlbumBotanicoCafeId,
                    ProponerCategoria = ProponerCategoria,
                    ProponerSubcategoria = ProponerSubcategoria,
                    ProponerFicha = ProponerFicha,
                    CategoriaPropuesta = CategoriaPropuesta.Trim(),
                    SubcategoriaPropuesta = SubcategoriaPropuesta.Trim(),
                    FichaPropuesta = FichaPropuesta.Trim(),
                    NombreCientifico = NombreCientifico.Trim(),
                    Descripcion = Descripcion.Trim(),
                    Sintomas = Sintomas.Trim(),
                    Motivo = Motivo.Trim()
                };

                ApiResult<bool> resultado = await jerarquiaApi
                    .ResolverJerarquiaAsync(
                        diagnosticoId,
                        Fotografia.FotografiaId,
                        request);

                if (!resultado.Success)
                    throw new InvalidOperationException(resultado.Message);

                await DisplayAlert(
                    "Clasificación guardada",
                    string.IsNullOrWhiteSpace(resultado.Message)
                        ? "La jerarquía de la fotografía fue actualizada."
                        : resultado.Message,
                    "Aceptar");

                await CerrarAsync(true);
            }
            catch (Exception ex)
            {
                await DisplayAlert("No fue posible guardar", ex.Message, "Aceptar");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool ValidarFormulario(out string error)
        {
            error = string.Empty;

            if (ProponerCategoria)
            {
                if (CategoriaPropuesta.Trim().Length < 3)
                    error = "Ingrese el nombre de la categoría propuesta.";
            }
            else if (CategoriaSeleccionada == null)
                error = "Seleccione una categoría.";

            if (string.IsNullOrEmpty(error))
            {
                if (ProponerSubcategoria)
                {
                    if (SubcategoriaPropuesta.Trim().Length < 3)
                        error = "Ingrese el nombre de la subcategoría propuesta.";
                }
                else if (SubcategoriaSeleccionada == null)
                    error = "Seleccione una subcategoría.";
            }

            if (string.IsNullOrEmpty(error))
            {
                if (ProponerFicha)
                {
                    if (FichaPropuesta.Trim().Length < 3)
                        error = "Ingrese el nombre de la ficha propuesta.";
                    else if (EsAprobador && Descripcion.Trim().Length < 8)
                        error = "Ingrese una descripción de al menos 8 caracteres para crear la ficha.";
                }
                else if (FichaSeleccionada == null)
                    error = "Seleccione una ficha específica.";
            }

            if (string.IsNullOrEmpty(error) &&
                !EsAprobador && Motivo.Trim().Length < 8)
            {
                error = "Explique la clasificación con al menos 8 caracteres.";
            }

            return string.IsNullOrEmpty(error);
        }

        private async Task CerrarAsync(bool resultado)
        {
            if (!resultadoTcs.Task.IsCompleted)
                resultadoTcs.TrySetResult(resultado);

            if (Navigation.ModalStack.Contains(this))
                await Navigation.PopModalAsync();
            else if (Navigation.NavigationStack.Contains(this))
                await Navigation.PopAsync();
        }

        private static string NormalizarEtapa(string? valor) =>
            string.Equals(valor, "APROBADOR", StringComparison.OrdinalIgnoreCase)
                ? "APROBADOR"
                : "ANALIZADOR";

        private void Set(ref string campo, string? valor,
            [CallerMemberName] string? nombre = null)
        {
            string nuevo = valor ?? string.Empty;
            if (campo == nuevo)
                return;
            campo = nuevo;
            OnPropertyChanged(nombre);
        }

    }
}
