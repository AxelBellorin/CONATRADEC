using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace CONATRADEC.Views
{
    /// <summary>
    /// Clasifica una fotografía con la estructura oficial del Álbum Botánico:
    /// Categoría -> Subcategoría específica -> Fotografías.
    /// </summary>
    public partial class JerarquiaAlbumFotografiaPage : ContentPage
    {
        private readonly int diagnosticoId;
        private readonly string etapa;
        private readonly AlbumBotanicoApiService albumApi = new();
        private readonly AlbumJerarquiaApiService jerarquiaApi = new();
        private readonly TaskCompletionSource<bool> resultadoTcs = new();

        private CategoriaAlbumBotanicoResponse? categoriaSeleccionada;
        private AlbumRegistroJerarquiaResponse? subcategoriaSeleccionada;
        private bool proponerCategoria;
        private bool proponerSubcategoria;
        private bool isBusy;
        private bool inicializada;
        private int versionCarga;

        private string categoriaPropuesta = string.Empty;
        private string subcategoriaPropuesta = string.Empty;
        private string nombreCientifico = string.Empty;
        private string descripcion = string.Empty;
        private string sintomas = string.Empty;
        private string motivo = string.Empty;

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
        public ObservableCollection<CategoriaAlbumBotanicoResponse>
            Categorias { get; } = [];
        public ObservableCollection<AlbumRegistroJerarquiaResponse>
            SubcategoriasEspecificas { get; } = [];

        public Command GuardarCommand { get; }
        public Command CancelarCommand { get; }
        public Task<bool> ResultadoTask => resultadoTcs.Task;

        public bool EsAprobador => etapa == "APROBADOR";
        public bool TieneJerarquiaActual =>
            JerarquiaActual?.TieneClasificacion == true;

        public string EtapaTexto => EsAprobador
            ? "Decisión del aprobador · las propuestas pueden convertirse en catálogo oficial"
            : "Análisis humano · seleccione una subcategoría existente o proponga la que falta";

        public string AyudaEtapa => EsAprobador
            ? "Al guardar, el sistema dejará una categoría y una subcategoría específica oficiales. Las fotografías aprobadas podrán asociarse a esa subcategoría."
            : "La propuesta no crea catálogos automáticamente. El aprobador revisará la categoría y la subcategoría antes de incorporarlas al Álbum Botánico.";

        public string TextoGuardar => EsAprobador
            ? "Confirmar clasificación"
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
        public bool RequiereDescripcionCreacion =>
            EsAprobador && ProponerSubcategoria;

        public CategoriaAlbumBotanicoResponse? CategoriaSeleccionada
        {
            get => categoriaSeleccionada;
            set
            {
                if (ReferenceEquals(categoriaSeleccionada, value))
                    return;

                categoriaSeleccionada = value;
                OnPropertyChanged();

                if (!ProponerCategoria)
                {
                    _ = CargarSubcategoriasEspecificasAsync(
                        value?.CategoriaAlbumBotanicoId);
                }
            }
        }

        public AlbumRegistroJerarquiaResponse? SubcategoriaSeleccionada
        {
            get => subcategoriaSeleccionada;
            set
            {
                if (ReferenceEquals(subcategoriaSeleccionada, value))
                    return;

                subcategoriaSeleccionada = value;
                OnPropertyChanged();

                if (value != null)
                {
                    ProponerSubcategoria = false;
                    NombreCientifico = value.NombreCientifico ?? string.Empty;
                }
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
                    categoriaSeleccionada = null;
                    OnPropertyChanged(nameof(CategoriaSeleccionada));
                    SubcategoriasEspecificas.Clear();
                    ProponerSubcategoria = true;
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
                OnPropertyChanged(nameof(RequiereDescripcionCreacion));

                if (value)
                {
                    subcategoriaSeleccionada = null;
                    OnPropertyChanged(nameof(SubcategoriaSeleccionada));
                }
            }
        }

        public string CategoriaPropuesta
        {
            get => categoriaPropuesta;
            set => Set(ref categoriaPropuesta, value);
        }

        public string SubcategoriaPropuesta
        {
            get => subcategoriaPropuesta;
            set => Set(ref subcategoriaPropuesta, value);
        }

        public string NombreCientifico
        {
            get => nombreCientifico;
            set => Set(ref nombreCientifico, value);
        }

        public string Descripcion
        {
            get => descripcion;
            set => Set(ref descripcion, value);
        }

        public string Sintomas
        {
            get => sintomas;
            set => Set(ref sintomas, value);
        }

        public string Motivo
        {
            get => motivo;
            set => Set(ref motivo, value);
        }

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
                foreach (CategoriaAlbumBotanicoResponse item in
                         resultado.Data ?? [])
                {
                    Categorias.Add(item);
                }

                PrecargarTextos();

                int? categoriaId =
                    JerarquiaActual?.CategoriaAlbumBotanicoId ??
                    Fotografia.ResultadoIA?.CategoriaAlbumBotanicoIdSugerida;

                CategoriaAlbumBotanicoResponse? categoria = Categorias
                    .FirstOrDefault(item =>
                        item.CategoriaAlbumBotanicoId == categoriaId);

                if (categoria != null &&
                    JerarquiaActual?.CategoriaEsPropuesta != true)
                {
                    categoriaSeleccionada = categoria;
                    OnPropertyChanged(nameof(CategoriaSeleccionada));
                    await CargarSubcategoriasEspecificasAsync(
                        categoria.CategoriaAlbumBotanicoId);
                }
                else if (!string.IsNullOrWhiteSpace(
                             JerarquiaActual?.Categoria))
                {
                    ProponerCategoria =
                        JerarquiaActual.CategoriaEsPropuesta;
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
                    "No fue posible cargar la clasificación",
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
            SubcategoriaPropuesta = actual?.Ficha ??
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
            ProponerSubcategoria =
                actual?.FichaEsPropuesta == true ||
                ia?.RequiereGestionAlbum == true;
        }

        private async Task CargarSubcategoriasEspecificasAsync(
            int? categoriaId)
        {
            int version = Interlocked.Increment(ref versionCarga);

            SubcategoriasEspecificas.Clear();
            subcategoriaSeleccionada = null;
            OnPropertyChanged(nameof(SubcategoriaSeleccionada));

            if (ProponerCategoria || categoriaId is not > 0)
                return;

            try
            {
                ApiResult<List<AlbumRegistroJerarquiaResponse>> resultado =
                    await jerarquiaApi.GetJerarquiaRegistrosAsync(
                        categoriaId: categoriaId,
                        subcategoriaId: null,
                        incluirInactivos: false);

                if (version != versionCarga)
                    return;

                if (!resultado.Success)
                    throw new InvalidOperationException(resultado.Message);

                foreach (AlbumRegistroJerarquiaResponse item in
                         resultado.Data ?? [])
                {
                    SubcategoriasEspecificas.Add(item);
                }

                int? id = JerarquiaActual?.AlbumBotanicoCafeId ??
                    Fotografia.ResultadoIA?.AlbumBotanicoCafeIdSugerido;

                AlbumRegistroJerarquiaResponse? seleccion =
                    SubcategoriasEspecificas.FirstOrDefault(item =>
                        item.AlbumBotanicoCafeId == id);

                if (seleccion != null &&
                    JerarquiaActual?.FichaEsPropuesta != true)
                {
                    subcategoriaSeleccionada = seleccion;
                    OnPropertyChanged(nameof(SubcategoriaSeleccionada));
                    NombreCientifico =
                        seleccion.NombreCientifico ?? string.Empty;
                    ProponerSubcategoria = false;
                }
                else if (SubcategoriasEspecificas.Count == 0)
                {
                    ProponerSubcategoria = true;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Subcategorías",
                    ex.Message,
                    "Aceptar");
            }
        }

        private void SugerirCategoriaDesdeIA()
        {
            string categoriaIa =
                Fotografia.ResultadoIA?.CategoriaPrincipal ?? string.Empty;

            string buscada = categoriaIa switch
            {
                "ENFERMEDAD" => "Enfermedades",
                "PLAGA" => "Plagas",
                "ALTERACION_NUTRICIONAL" => "Alteraciones nutricionales",
                "ESTRES_ABIOTICO" => "Estrés abiótico",
                "DANO_MECANICO" => "Daños mecánicos",
                "NO_APLICA" when
                    Fotografia.ResultadoIA?.EsAparentementeSana == true =>
                        "Plantas sanas",
                _ => string.Empty
            };

            CategoriaAlbumBotanicoResponse? categoria = Categorias
                .FirstOrDefault(item => item.NombreCategoria.Contains(
                    buscada,
                    StringComparison.OrdinalIgnoreCase));

            if (categoria != null)
            {
                CategoriaSeleccionada = categoria;
            }
            else
            {
                ProponerCategoria = true;
                CategoriaPropuesta = string.IsNullOrWhiteSpace(buscada)
                    ? Fotografia.ResultadoIA?.CategoriaAlbumPropuesta ??
                        string.Empty
                    : buscada;
            }
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
                EsAprobador
                    ? "Confirmar clasificación"
                    : "Guardar propuesta",
                EsAprobador
                    ? "La fotografía quedará vinculada con una categoría y una subcategoría específica oficiales."
                    : "La clasificación quedará registrada para que el aprobador revise la categoría y la subcategoría propuestas.",
                "Continuar",
                "Cancelar");

            if (!confirmar)
                return;

            IsBusy = true;
            try
            {
                int? subcategoriaId = ProponerSubcategoria
                    ? null
                    : SubcategoriaSeleccionada?.AlbumBotanicoCafeId;

                var request = new ResolverJerarquiaAlbumRequest
                {
                    Etapa = etapa,
                    CategoriaAlbumBotanicoId = ProponerCategoria
                        ? null
                        : CategoriaSeleccionada?.CategoriaAlbumBotanicoId,
                    SubcategoriaAlbumBotanicoId = subcategoriaId,
                    AlbumBotanicoCafeId = subcategoriaId,
                    ProponerCategoria = ProponerCategoria,
                    ProponerSubcategoria = ProponerSubcategoria,
                    ProponerFicha = false,
                    CategoriaPropuesta = CategoriaPropuesta.Trim(),
                    SubcategoriaPropuesta = SubcategoriaPropuesta.Trim(),
                    FichaPropuesta = SubcategoriaPropuesta.Trim(),
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
                        ? "La clasificación de la fotografía fue actualizada."
                        : resultado.Message,
                    "Aceptar");

                await CerrarAsync(true);
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "No fue posible guardar",
                    ex.Message,
                    "Aceptar");
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
            {
                error = "Seleccione una categoría.";
            }

            if (string.IsNullOrEmpty(error))
            {
                if (ProponerSubcategoria)
                {
                    if (SubcategoriaPropuesta.Trim().Length < 3)
                    {
                        error =
                            "Ingrese el nombre de la subcategoría propuesta.";
                    }
                    else if (EsAprobador &&
                             Descripcion.Trim().Length < 8)
                    {
                        error =
                            "Ingrese una descripción de al menos 8 caracteres para crear la subcategoría.";
                    }
                }
                else if (SubcategoriaSeleccionada == null)
                {
                    error = "Seleccione una subcategoría específica.";
                }
            }

            if (string.IsNullOrEmpty(error) &&
                !EsAprobador &&
                Motivo.Trim().Length < 8)
            {
                error =
                    "Explique la clasificación con al menos 8 caracteres.";
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
            string.Equals(
                valor,
                "APROBADOR",
                StringComparison.OrdinalIgnoreCase)
                ? "APROBADOR"
                : "ANALIZADOR";

        private void Set(
            ref string campo,
            string? valor,
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
