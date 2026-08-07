using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace CONATRADEC.Views
{
    /// <summary>
    /// Clasifica una fotografía con la estructura oficial del Álbum Botánico:
    /// Categoría -> Subcategoría específica -> Fotografías.
    ///
    /// La categoría siempre proviene del catálogo oficial. La subcategoría se
    /// busca primero en el catálogo y solo puede proponerse una nueva cuando no
    /// exista una coincidencia adecuada.
    /// </summary>
    public partial class JerarquiaAlbumFotografiaPage : ContentPage
    {
        private readonly int diagnosticoId;
        private readonly string etapa;
        private readonly AlbumBotanicoApiService albumApi = new();
        private readonly AlbumJerarquiaApiService jerarquiaApi = new();
        private readonly TaskCompletionSource<bool> resultadoTcs = new();
        private readonly List<AlbumRegistroJerarquiaResponse>
            subcategoriasCatalogo = [];

        private CategoriaAlbumBotanicoResponse? categoriaSeleccionada;
        private AlbumRegistroJerarquiaResponse? subcategoriaSeleccionada;
        private bool proponerSubcategoria;
        private bool isBusy;
        private bool inicializada;
        private int versionCarga;

        private string buscarSubcategoria = string.Empty;
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
            AlternarPropuestaCommand = new Command(
                AlternarPropuesta,
                () => !IsBusy);
            LimpiarBusquedaCommand = new Command(
                () => BuscarSubcategoria = string.Empty,
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
        public Command AlternarPropuestaCommand { get; }
        public Command LimpiarBusquedaCommand { get; }
        public Task<bool> ResultadoTask => resultadoTcs.Task;

        public bool EsAprobador => etapa == "APROBADOR";

        public bool TieneJerarquiaActual =>
            JerarquiaActual?.TieneClasificacion == true;

        public string EtapaTexto => EsAprobador
            ? "Aprobador · seleccione la clasificación oficial antes de decidir"
            : "Analizador · confirme una clasificación existente o proponga únicamente la subcategoría que falte";

        public string AyudaEtapa => EsAprobador
            ? "El aprobador puede convertir una propuesta de subcategoría en catálogo oficial. La categoría debe existir previamente en el Álbum Botánico."
            : "La categoría nunca se crea desde la inspección. Una subcategoría nueva queda solo como propuesta hasta que el aprobador la confirme.";

        public string TextoGuardar => EsAprobador
            ? "Confirmar clasificación"
            : ProponerSubcategoria
                ? "Guardar propuesta"
                : "Guardar clasificación";

        public string TextoAlternarPropuesta => ProponerSubcategoria
            ? "← Volver al catálogo"
            : "+ Proponer nueva subcategoría";

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
                AlternarPropuestaCommand.ChangeCanExecute();
                LimpiarBusquedaCommand.ChangeCanExecute();
            }
        }

        public bool NotIsBusy => !IsBusy;
        public bool UsarSubcategoriaExistente => !ProponerSubcategoria;
        public bool RequiereDescripcionCreacion =>
            EsAprobador && ProponerSubcategoria;

        /// <summary>
        /// Se conserva para compatibilidad con DTO antiguos, pero nunca se
        /// habilita desde esta interfaz. Las categorías se administran en el
        /// Álbum Botánico.
        /// </summary>
        public bool ProponerCategoria => false;

        public CategoriaAlbumBotanicoResponse? CategoriaSeleccionada
        {
            get => categoriaSeleccionada;
            set
            {
                if (ReferenceEquals(categoriaSeleccionada, value))
                    return;

                categoriaSeleccionada = value;
                OnPropertyChanged();

                BuscarSubcategoria = string.Empty;
                _ = CargarSubcategoriasEspecificasAsync(
                    value?.CategoriaAlbumBotanicoId);
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

        public bool ProponerSubcategoria
        {
            get => proponerSubcategoria;
            private set
            {
                if (proponerSubcategoria == value)
                    return;

                proponerSubcategoria = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UsarSubcategoriaExistente));
                OnPropertyChanged(nameof(RequiereDescripcionCreacion));
                OnPropertyChanged(nameof(TextoGuardar));
                OnPropertyChanged(nameof(TextoAlternarPropuesta));

                if (value)
                {
                    subcategoriaSeleccionada = null;
                    OnPropertyChanged(nameof(SubcategoriaSeleccionada));

                    if (string.IsNullOrWhiteSpace(SubcategoriaPropuesta))
                    {
                        SubcategoriaPropuesta = LimpiarNombreDiagnostico(
                            PrimerTexto(
                                JerarquiaActual?.Ficha,
                                Fotografia.ResultadoIA?.ClasificacionAlbumPropuesta,
                                Fotografia.ResultadoIA?.DiagnosticoVisible));
                    }
                }
            }
        }

        public string BuscarSubcategoria
        {
            get => buscarSubcategoria;
            set
            {
                if (!Set(ref buscarSubcategoria, value))
                    return;

                AplicarFiltroSubcategorias();
            }
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

                if (categoria == null)
                {
                    string nombre = PrimerTexto(
                        JerarquiaActual?.Categoria,
                        Fotografia.ResultadoIA?.CategoriaAlbumPropuesta,
                        MapearCategoriaDesdeIA());

                    string buscada = NormalizarClave(nombre);
                    categoria = Categorias.FirstOrDefault(item =>
                        NormalizarClave(item.NombreCategoria) == buscada);
                }

                if (categoria != null)
                {
                    categoriaSeleccionada = categoria;
                    OnPropertyChanged(nameof(CategoriaSeleccionada));
                    await CargarSubcategoriasEspecificasAsync(
                        categoria.CategoriaAlbumBotanicoId);
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

            SubcategoriaPropuesta = LimpiarNombreDiagnostico(
                PrimerTexto(
                    actual?.Ficha,
                    ia?.ClasificacionAlbumPropuesta,
                    ia?.DiagnosticoVisible));
            NombreCientifico = PrimerTexto(
                actual?.NombreCientifico,
                ia?.NombreCientificoSugerido);
            Motivo = PrimerTexto(
                actual?.Motivo,
                ia?.MotivoAlbumPropuesta,
                "Clasificación revisada por el analizador.");
            Descripcion = ia?.ResumenImagen ?? string.Empty;
            Sintomas = string.Join(
                Environment.NewLine,
                ia?.SintomasVisibles ?? []);

            ProponerSubcategoria =
                actual?.FichaEsPropuesta == true ||
                ia?.RequiereGestionAlbum == true;
        }

        private async Task CargarSubcategoriasEspecificasAsync(
            int? categoriaId)
        {
            int version = Interlocked.Increment(ref versionCarga);

            subcategoriasCatalogo.Clear();
            SubcategoriasEspecificas.Clear();
            subcategoriaSeleccionada = null;
            OnPropertyChanged(nameof(SubcategoriaSeleccionada));

            if (categoriaId is not > 0)
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

                subcategoriasCatalogo.AddRange(
                    (resultado.Data ?? [])
                        .Where(item => item.Activo)
                        .OrderBy(item => item.Titulo));

                AplicarFiltroSubcategorias();

                int? id = JerarquiaActual?.AlbumBotanicoCafeId ??
                    Fotografia.ResultadoIA?.AlbumBotanicoCafeIdSugerido;

                AlbumRegistroJerarquiaResponse? seleccion =
                    subcategoriasCatalogo.FirstOrDefault(item =>
                        item.AlbumBotanicoCafeId == id);

                if (seleccion == null)
                {
                    string nombre = LimpiarNombreDiagnostico(
                        PrimerTexto(
                            JerarquiaActual?.Ficha,
                            Fotografia.ResultadoIA?.ClasificacionAlbumPropuesta,
                            Fotografia.ResultadoIA?.DiagnosticoVisible));
                    string clave = NormalizarClave(nombre);

                    seleccion = subcategoriasCatalogo.FirstOrDefault(item =>
                        NormalizarClave(item.Titulo) == clave);
                }

                if (seleccion != null &&
                    JerarquiaActual?.FichaEsPropuesta != true)
                {
                    subcategoriaSeleccionada = seleccion;
                    OnPropertyChanged(nameof(SubcategoriaSeleccionada));
                    NombreCientifico =
                        seleccion.NombreCientifico ?? string.Empty;
                    ProponerSubcategoria = false;
                }
                else if (subcategoriasCatalogo.Count == 0)
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

        private void AplicarFiltroSubcategorias()
        {
            string filtro = NormalizarClave(BuscarSubcategoria);

            IEnumerable<AlbumRegistroJerarquiaResponse> items =
                subcategoriasCatalogo;

            if (!string.IsNullOrWhiteSpace(filtro))
            {
                items = items.Where(item =>
                    NormalizarClave(item.Titulo).Contains(
                        filtro,
                        StringComparison.Ordinal) ||
                    NormalizarClave(item.NombreCientifico).Contains(
                        filtro,
                        StringComparison.Ordinal));
            }

            SubcategoriasEspecificas.Clear();
            foreach (AlbumRegistroJerarquiaResponse item in items)
                SubcategoriasEspecificas.Add(item);
        }

        private void AlternarPropuesta()
        {
            if (IsBusy)
                return;

            ProponerSubcategoria = !ProponerSubcategoria;

            if (!ProponerSubcategoria)
            {
                BuscarSubcategoria = string.Empty;
                AplicarFiltroSubcategorias();
            }
        }

        private async Task GuardarAsync()
        {
            if (IsBusy)
                return;

            if (!ValidarFormulario(out string error))
            {
                await DisplayAlert(
                    "Revise la clasificación",
                    error,
                    "Aceptar");
                return;
            }

            if (ProponerSubcategoria &&
                !await ValidarPropuestaContraCatalogoAsync())
            {
                return;
            }

            bool confirmar = await DisplayAlert(
                EsAprobador
                    ? "Confirmar clasificación"
                    : ProponerSubcategoria
                        ? "Guardar propuesta"
                        : "Guardar clasificación",
                EsAprobador
                    ? ProponerSubcategoria
                        ? "La propuesta se convertirá en una subcategoría oficial dentro de la categoría seleccionada."
                        : "La fotografía quedará vinculada con la subcategoría oficial seleccionada."
                    : ProponerSubcategoria
                        ? "La nueva subcategoría quedará como propuesta para que el aprobador la revise."
                        : "La fotografía quedará vinculada con una subcategoría existente del catálogo.",
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
                    CategoriaAlbumBotanicoId =
                        CategoriaSeleccionada?.CategoriaAlbumBotanicoId,
                    SubcategoriaAlbumBotanicoId = subcategoriaId,
                    AlbumBotanicoCafeId = subcategoriaId,
                    ProponerCategoria = false,
                    ProponerSubcategoria = ProponerSubcategoria,
                    ProponerFicha = false,
                    CategoriaPropuesta = string.Empty,
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
            if (CategoriaSeleccionada == null)
            {
                error =
                    "Seleccione una categoría existente. Las categorías nuevas se administran desde el Álbum Botánico.";
                return false;
            }

            if (ProponerSubcategoria)
            {
                if (SubcategoriaPropuesta.Trim().Length < 3)
                {
                    error = "Ingrese el nombre de la subcategoría propuesta.";
                    return false;
                }

                if (EsAprobador && Descripcion.Trim().Length < 8)
                {
                    error =
                        "Ingrese una descripción de al menos 8 caracteres para crear la subcategoría oficial.";
                    return false;
                }
            }
            else if (SubcategoriaSeleccionada == null)
            {
                error =
                    "Seleccione una subcategoría del catálogo o use «Proponer nueva subcategoría».";
                return false;
            }

            if (!EsAprobador && Motivo.Trim().Length < 8)
            {
                error =
                    "Explique la clasificación con al menos 8 caracteres.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private async Task<bool> ValidarPropuestaContraCatalogoAsync()
        {
            string propuesta = SubcategoriaPropuesta.Trim();
            string clave = NormalizarClave(propuesta);

            AlbumRegistroJerarquiaResponse? exacta =
                subcategoriasCatalogo.FirstOrDefault(item =>
                    NormalizarClave(item.Titulo) == clave ||
                    (!string.IsNullOrWhiteSpace(NombreCientifico) &&
                     NormalizarClave(item.NombreCientifico) ==
                        NormalizarClave(NombreCientifico)));

            if (exacta != null)
            {
                SubcategoriaSeleccionada = exacta;
                ProponerSubcategoria = false;
                BuscarSubcategoria = exacta.Titulo;

                await DisplayAlert(
                    "Subcategoría ya existente",
                    $"Ya existe «{exacta.Titulo}» dentro de esta categoría. Se utilizará el registro existente para evitar una duplicidad.",
                    "Aceptar");
                return true;
            }

            AlbumRegistroJerarquiaResponse? similar =
                subcategoriasCatalogo
                    .Select(item => new
                    {
                        Item = item,
                        Similitud = CalcularSimilitud(
                            clave,
                            NormalizarClave(item.Titulo))
                    })
                    .Where(item => item.Similitud >= 0.84d)
                    .OrderByDescending(item => item.Similitud)
                    .Select(item => item.Item)
                    .FirstOrDefault();

            if (similar == null)
                return true;

            bool usarExistente = await DisplayAlert(
                "Posible duplicado",
                $"Existe una subcategoría muy similar: «{similar.Titulo}». ¿Desea utilizar la existente en lugar de crear «{propuesta}»?",
                "Usar existente",
                "Mantener propuesta");

            if (!usarExistente)
                return true;

            SubcategoriaSeleccionada = similar;
            ProponerSubcategoria = false;
            BuscarSubcategoria = similar.Titulo;
            return true;
        }

        private string MapearCategoriaDesdeIA()
        {
            string categoriaIa =
                Fotografia.ResultadoIA?.CategoriaPrincipal ?? string.Empty;

            return categoriaIa switch
            {
                "ENFERMEDAD" => "Enfermedades",
                "PLAGA" => "Plagas",
                "ALTERACION_NUTRICIONAL" => "Alteraciones nutricionales",
                "ESTRES_ABIOTICO" => "Estrés abiótico",
                "DANO_MECANICO" => "Daños mecánicos",
                "NO_APLICA" when
                    Fotografia.ResultadoIA?.EsAparentementeSana == true =>
                        "Plantas sanas",
                _ => Fotografia.ResultadoIA?.CategoriaAlbumPropuesta ??
                    string.Empty
            };
        }

        private async void OnImagenTapped(object? sender, TappedEventArgs e)
        {
            var visor = new VisorFotografiaFitosanitariaPage(
                [Fotografia],
                0);
            await Navigation.PushModalAsync(visor, animated: false);
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

        private bool Set(
            ref string campo,
            string? valor,
            [CallerMemberName] string? nombre = null)
        {
            string nuevo = valor ?? string.Empty;
            if (campo == nuevo)
                return false;

            campo = nuevo;
            OnPropertyChanged(nombre);
            return true;
        }

        private static string PrimerTexto(params string?[] valores) =>
            valores.FirstOrDefault(valor =>
                !string.IsNullOrWhiteSpace(valor))?.Trim() ?? string.Empty;

        private static string LimpiarNombreDiagnostico(string? valor)
        {
            string texto = (valor ?? string.Empty).Trim();
            int parentesis = texto.IndexOf('(');
            if (parentesis > 0)
                texto = texto[..parentesis].Trim();

            int separador = texto.IndexOf(" - ", StringComparison.Ordinal);
            if (separador > 0)
                texto = texto[..separador].Trim();

            return texto;
        }

        private static string NormalizarClave(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return string.Empty;

            string texto = valor
                .Trim()
                .ToUpperInvariant()
                .Normalize(NormalizationForm.FormD);

            var builder = new StringBuilder(texto.Length);
            bool espacioPendiente = false;

            foreach (char caracter in texto)
            {
                UnicodeCategory categoria =
                    CharUnicodeInfo.GetUnicodeCategory(caracter);

                if (categoria == UnicodeCategory.NonSpacingMark)
                    continue;

                if (char.IsLetterOrDigit(caracter))
                {
                    if (espacioPendiente && builder.Length > 0)
                        builder.Append(' ');

                    builder.Append(caracter);
                    espacioPendiente = false;
                }
                else if (builder.Length > 0)
                {
                    espacioPendiente = true;
                }
            }

            return builder.ToString().Trim();
        }

        private static double CalcularSimilitud(string izquierda, string derecha)
        {
            string a = izquierda.Replace(" ", string.Empty);
            string b = derecha.Replace(" ", string.Empty);

            if (a.Length == 0 || b.Length == 0)
                return 0d;

            if (string.Equals(a, b, StringComparison.Ordinal))
                return 1d;

            int distancia = DistanciaLevenshtein(a, b);
            return 1d - distancia / (double)Math.Max(a.Length, b.Length);
        }

        private static int DistanciaLevenshtein(string a, string b)
        {
            int[] anterior = Enumerable.Range(0, b.Length + 1).ToArray();
            int[] actual = new int[b.Length + 1];

            for (int i = 1; i <= a.Length; i++)
            {
                actual[0] = i;

                for (int j = 1; j <= b.Length; j++)
                {
                    int costo = a[i - 1] == b[j - 1] ? 0 : 1;
                    actual[j] = Math.Min(
                        Math.Min(actual[j - 1] + 1, anterior[j] + 1),
                        anterior[j - 1] + costo);
                }

                (anterior, actual) = (actual, anterior);
            }

            return anterior[b.Length];
        }
    }
}
