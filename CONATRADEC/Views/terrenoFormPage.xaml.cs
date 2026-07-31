using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using Microsoft.Maui.Controls;
using System.Globalization;
using System.Text;

namespace CONATRADEC.Views
{
    public partial class terrenoFormPage : ContentPage
    {
        private readonly TerrenoFormFotosSegurasViewModel
            viewModel;

        private readonly TerrenoApiService
            terrenoApiService = new();

        private readonly FotoTerrenoApiService
            fotoTerrenoApiService = new();

        private bool actualizandoNumero;

        private bool actualizandoCoordenadasTexto;

        public terrenoFormPage()
        {
            InitializeComponent();

            /*
             * Se conserva el ViewModel que libera de forma segura las
             * fotografías temporales. El guardado envía únicamente la
             * relación normalizada mediante propietarioId.
             */
            viewModel =
                new TerrenoFormFotosSegurasViewModel();

            BindingContext = viewModel;

            viewModel.RefrescarMapaAction =
                (latitud, longitud) =>
                {
                    ActualizarMiniMapa(
                        latitud,
                        longitud);

                    SincronizarEntradasCoordenadas(
                        latitud,
                        longitud);
                };
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await viewModel.InicializarAsync();

            AplicarPropietarioSeleccionado();

            CargarMiniMapa();

            SincronizarEntradasCoordenadas(
                viewModel.Latitud,
                viewModel.Longitud);
        }

        private void AplicarPropietarioSeleccionado()
        {
            PropietarioResponse? propietario =
                PropietarioSeleccionService
                    .Consumir();

            if (propietario == null)
                return;

            viewModel.Terreno ??=
                new TerrenoRequest();

            var propietarioTerreno =
                new TerrenoPropietarioResponse
                {
                    PropietarioId =
                        propietario.PropietarioId,
                    Identificacion =
                        propietario.Identificacion,
                    NombreCompleto =
                        propietario.NombreCompleto,
                    Telefono =
                        propietario.Telefono,
                    Correo =
                        propietario.Correo,
                    Direccion =
                        propietario.Direccion
                };

            viewModel.PropietarioSeleccionado =
                propietarioTerreno;
        }

        private async void
            BtnSeleccionarPropietario_Clicked(
                object sender,
                EventArgs e)
        {
            if (viewModel.IsReadOnly ||
                viewModel.IsBusy)
            {
                return;
            }

            if (!ModoSesionService.EsEnLinea)
            {
                await AppNotificationService
                    .ShowWarningAsync(
                        "La selección de propietarios " +
                        "requiere conexión a internet.");

                return;
            }

            /*
             * TerrenoFormViewModel conserva el estado temporal dentro de
             * Terreno cuando una pantalla secundaria cubre el formulario.
             */
            viewModel.Terreno =
                CrearEstadoTemporalFormulario();

            PropietarioSeleccionService.Limpiar();

            await Shell.Current.GoToAsync(
                AppRoutes.Propietarios,
                true,
                new Dictionary<string, object>
                {
                    ["ModoSeleccion"] = "true"
                });
        }

        private async void BtnGuardar_Clicked(
            object sender,
            EventArgs e)
        {
            if (viewModel.IsBusy ||
                viewModel.IsReadOnly)
            {
                return;
            }

            if (!ModoSesionService.EsEnLinea)
            {
                await AppNotificationService
                    .ShowWarningAsync(
                        "Crear o editar terrenos requiere " +
                        "conexión a internet.");

                return;
            }

            string? error = ValidarFormulario();

            if (error != null)
            {
                await AppNotificationService
                    .ShowWarningAsync(error);

                return;
            }

            bool confirmar =
                viewModel.Mode ==
                    FormMode.FormModeSelect.Create
                    ? await AppNotificationService
                        .ConfirmSaveAsync(
                            "el terreno")
                    : await AppNotificationService
                        .ConfirmUpdateAsync(
                            "el terreno");

            if (!confirmar)
                return;

            viewModel.IsBusy = true;

            try
            {
                TerrenoRequest request =
                    ConstruirRequestNormalizado();

                int terrenoId;

                if (viewModel.Mode ==
                    FormMode.FormModeSelect.Create)
                {
                    ApiResult<TerrenoResponse> resultado =
                        await terrenoApiService
                            .CreateTerrenoRetornandoResultAsync(
                                request);

                    TerrenoResponse? creado =
                        resultado.Data;

                    if (!resultado.Success ||
                        creado?.TerrenoId
                            is null or <= 0)
                    {
                        await AppNotificationService
                            .ShowErrorAsync(
                                resultado.Message);

                        return;
                    }

                    terrenoId =
                        creado.TerrenoId.Value;

                    viewModel.CodigoTerreno =
                        creado.CodigoTerreno;
                }
                else
                {
                    if (viewModel.Terreno?.TerrenoId
                        is null or <= 0)
                    {
                        await AppNotificationService
                            .ShowErrorAsync(
                                "No se encontró el terreno " +
                                "que desea actualizar.");

                        return;
                    }

                    request.TerrenoId =
                        viewModel.Terreno.TerrenoId;

                    request.CodigoTerreno =
                        viewModel.Terreno.CodigoTerreno;

                    ApiResult<bool> resultado =
                        await terrenoApiService
                            .UpdateTerrenoResultAsync(
                                request);

                    if (!resultado.Success ||
                        resultado.Data != true)
                    {
                        await AppNotificationService
                            .ShowErrorAsync(
                                resultado.Message);

                        return;
                    }

                    terrenoId =
                        viewModel.Terreno
                            .TerrenoId.Value;
                }

                ApiResult<bool> fotos =
                    await fotoTerrenoApiService
                        .SubirFotosResultAsync(
                            terrenoId,
                            viewModel.FotosTerreno);

                if (fotos.Success)
                {
                    foreach (FotoTerrenoItem foto
                             in viewModel.FotosTerreno
                                 .Where(item =>
                                     item.EsNueva))
                    {
                        foto.EsNueva = false;
                        foto.TerrenoId = terrenoId;
                    }
                }

                await Shell.Current.GoToAsync(
                    AppRoutes.Terrenos);

                if (fotos.Success)
                {
                    await AppNotificationService
                        .ShowSuccessAsync(
                            viewModel.Mode ==
                                FormMode.FormModeSelect.Create
                                ? "Terreno guardado correctamente."
                                : "Terreno actualizado correctamente.");
                }
                else
                {
                    await AppNotificationService
                        .ShowWarningAsync(
                            "El terreno se guardó, pero no se " +
                            "pudieron subir todas las fotografías.");
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);

                await AppNotificationService
                    .ShowErrorAsync(
                        "Ocurrió un error inesperado al " +
                        "guardar el terreno.");
            }
            finally
            {
                viewModel.IsBusy = false;
            }
        }

        private TerrenoRequest CrearEstadoTemporalFormulario()
        {
            return new TerrenoRequest
            {
                TerrenoId =
                    viewModel.Terreno?.TerrenoId,

                CodigoTerreno =
                    viewModel.Terreno?.CodigoTerreno,

                PropietarioId =
                    viewModel.PropietarioSeleccionado?
                        .PropietarioId ??
                    viewModel.Terreno?.PropietarioId,

                Propietario =
                    viewModel.PropietarioSeleccionado,

                DireccionTerreno =
                    viewModel.DireccionTerreno,

                ExtensionManzanaTerreno =
                    viewModel.ExtensionManzanaTerreno,

                CantidadQuintalesOro =
                    viewModel.CantidadQuintalesOro,

                CantidadPlantasTerreno =
                    viewModel.CantidadPlantasTerreno,

                FechaIngresoTerreno =
                    viewModel.FechaIngresoTerreno,

                MunicipioId =
                    viewModel
                        .MunicipioSeleccionado?
                        .MunicipioId ??
                    viewModel.Terreno?
                        .MunicipioId,

                Latitud = viewModel.Latitud,
                Longitud = viewModel.Longitud
            };
        }

        private TerrenoRequest
            ConstruirRequestNormalizado()
        {
            int propietarioId =
                viewModel.PropietarioSeleccionado?
                    .PropietarioId ??
                viewModel.Terreno?
                    .PropietarioId ??
                0;

            return new TerrenoRequest
            {
                TerrenoId =
                    viewModel.Terreno?.TerrenoId,

                CodigoTerreno =
                    viewModel.Mode ==
                        FormMode.FormModeSelect.Create
                        ? null
                        : viewModel.Terreno?
                            .CodigoTerreno,

                PropietarioId =
                    propietarioId,

                DireccionTerreno =
                    viewModel
                        .DireccionTerreno?
                        .Trim(),

                ExtensionManzanaTerreno =
                    viewModel
                        .ExtensionManzanaTerreno,

                CantidadQuintalesOro =
                    viewModel
                        .CantidadQuintalesOro ?? 0,

                CantidadPlantasTerreno =
                    viewModel
                        .CantidadPlantasTerreno ?? 0,

                FechaIngresoTerreno =
                    viewModel.Terreno?
                        .FechaIngresoTerreno ??
                    DateOnly.FromDateTime(
                        DateTime.Today),

                MunicipioId =
                    viewModel
                        .MunicipioSeleccionado?
                        .MunicipioId ??
                    viewModel.Terreno?
                        .MunicipioId ??
                    0,

                Latitud = viewModel.Latitud,
                Longitud = viewModel.Longitud
            };
        }

        private string? ValidarFormulario()
        {
            int propietarioId =
                viewModel.PropietarioSeleccionado?
                    .PropietarioId ??
                viewModel.Terreno?.PropietarioId ??
                0;

            if (propietarioId <= 0)
            {
                return
                    "Debe seleccionar un propietario registrado.";
            }

            if (string.IsNullOrWhiteSpace(
                    viewModel.DireccionTerreno))
            {
                return
                    "La dirección del terreno es obligatoria.";
            }

            if (viewModel.ExtensionManzanaTerreno
                is null or <= 0)
            {
                return
                    "La extensión debe ser mayor que cero.";
            }

            if (viewModel.CantidadQuintalesOro
                is < 0)
            {
                return
                    "La cantidad de quintales no puede " +
                    "ser negativa.";
            }

            if (viewModel.CantidadPlantasTerreno
                is < 0)
            {
                return
                    "La cantidad de plantas no puede " +
                    "ser negativa.";
            }

            int municipioId =
                viewModel.MunicipioSeleccionado?
                    .MunicipioId ??
                viewModel.Terreno?.MunicipioId ??
                0;

            if (municipioId <= 0)
                return "Debe seleccionar un municipio.";

            if (!viewModel.Latitud.HasValue ||
                !viewModel.Longitud.HasValue)
            {
                return
                    "Debe definir la ubicación del terreno.";
            }

            if (viewModel.Latitud.Value
                is < -90 or > 90)
            {
                return
                    "La latitud debe estar entre -90 y 90.";
            }

            if (viewModel.Longitud.Value
                is < -180 or > 180)
            {
                return
                    "La longitud debe estar entre -180 y 180.";
            }

            return null;
        }

        private async void BtnAbrirEnMaps_Clicked(
            object sender,
            EventArgs e)
        {
            if (viewModel.Latitud.HasValue &&
                viewModel.Longitud.HasValue)
            {
                await viewModel.AbrirEnGoogleMaps(
                    viewModel.Latitud.Value,
                    viewModel.Longitud.Value);
            }
        }

        private void CargarMiniMapa()
        {
            double latitud =
                viewModel.Latitud ??
                12.1364;

            double longitud =
                viewModel.Longitud ??
                -86.2510;

            MiniMapaWeb.Source =
                new HtmlWebViewSource
                {
                    Html = BuildLeafletHtml(
                        latitud,
                        longitud)
                };
        }

        private void ActualizarMiniMapa(
            double? latitud,
            double? longitud)
        {
            if (!latitud.HasValue ||
                !longitud.HasValue)
            {
                return;
            }

            string html =
                BuildLeafletHtml(
                    latitud.Value,
                    longitud.Value);

            MainThread.BeginInvokeOnMainThread(
                () =>
                {
                    MiniMapaWeb.Source =
                        new HtmlWebViewSource
                        {
                            Html = html
                        };
                });
        }

        private void SincronizarEntradasCoordenadas(
            double? latitud,
            double? longitud)
        {
            MainThread.BeginInvokeOnMainThread(
                () =>
                {
                    LatitudEntry.Text =
                        latitud?.ToString(
                            "0.########",
                            CultureInfo.InvariantCulture) ??
                        string.Empty;

                    LongitudEntry.Text =
                        longitud?.ToString(
                            "0.########",
                            CultureInfo.InvariantCulture) ??
                        string.Empty;

                    if (!latitud.HasValue ||
                        !longitud.HasValue ||
                        CoordenadasEntry.IsFocused)
                    {
                        return;
                    }

                    string coordenadas =
                        latitud.Value.ToString(
                            "0.########",
                            CultureInfo.InvariantCulture) +
                        ", " +
                        longitud.Value.ToString(
                            "0.########",
                            CultureInfo.InvariantCulture);

                    actualizandoCoordenadasTexto =
                        true;

                    try
                    {
                        CoordenadasEntry.Text =
                            coordenadas;
                    }
                    finally
                    {
                        actualizandoCoordenadasTexto =
                            false;
                    }
                });
        }

        private void DecimalDosDigitos_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            if (actualizandoNumero ||
                sender is not Entry entry)
            {
                return;
            }

            string filtrado =
                FiltrarDecimalDosDigitos(
                    e.NewTextValue);

            if (entry.Text == filtrado)
                return;

            actualizandoNumero = true;

            try
            {
                entry.Text = filtrado;
                entry.CursorPosition =
                    filtrado.Length;
            }
            finally
            {
                actualizandoNumero = false;
            }
        }

        private static string
            FiltrarDecimalDosDigitos(
                string? valor)
        {
            if (string.IsNullOrEmpty(valor))
                return string.Empty;

            string separador =
                CultureInfo.CurrentCulture
                    .NumberFormat
                    .NumberDecimalSeparator;

            var resultado =
                new StringBuilder();

            bool tieneSeparador = false;
            int decimales = 0;

            foreach (char caracter in valor)
            {
                if (char.IsDigit(caracter))
                {
                    if (tieneSeparador &&
                        decimales >= 2)
                    {
                        continue;
                    }

                    resultado.Append(caracter);

                    if (tieneSeparador)
                        decimales++;

                    continue;
                }

                if ((caracter == '.' ||
                     caracter == ',') &&
                    !tieneSeparador)
                {
                    if (resultado.Length == 0)
                        resultado.Append('0');

                    resultado.Append(separador);
                    tieneSeparador = true;
                }
            }

            return resultado.ToString();
        }

        private void Entero_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            if (actualizandoNumero ||
                sender is not Entry entry)
            {
                return;
            }

            string filtrado =
                new(
                    (e.NewTextValue ??
                     string.Empty)
                    .Where(char.IsDigit)
                    .ToArray());

            if (entry.Text == filtrado)
                return;

            actualizandoNumero = true;

            try
            {
                entry.Text = filtrado;
                entry.CursorPosition =
                    filtrado.Length;
            }
            finally
            {
                actualizandoNumero = false;
            }
        }

        private void CoordenadasEntry_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            if (actualizandoCoordenadasTexto)
                return;

            viewModel.CoordenadasTexto =
                e.NewTextValue;
        }

        private static string BuildLeafletHtml(
            double latitud,
            double longitud)
        {
            string latitudTexto =
                latitud.ToString(
                    CultureInfo.InvariantCulture);

            string longitudTexto =
                longitud.ToString(
                    CultureInfo.InvariantCulture);

            return $$"""
                <!DOCTYPE html>
                <html>
                <head>
                    <meta name="viewport"
                          content="width=device-width,
                                   initial-scale=1.0">

                    <link rel="stylesheet"
                          href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css">

                    <script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js">
                    </script>

                    <style>
                        html, body
                        {
                            margin: 0;
                            padding: 0;
                            height: 100%;
                        }

                        #map
                        {
                            width: 100%;
                            height: 100%;
                            border-radius: 10px;
                        }
                    </style>
                </head>
                <body>
                    <div id="map"></div>

                    <script>
                        const map = L.map("map")
                            .setView(
                                [{{latitudTexto}}, {{longitudTexto}}],
                                16);

                        L.tileLayer(
                            "https://tile.openstreetmap.org/{z}/{x}/{y}.png",
                            {
                                maxZoom: 19
                            })
                            .addTo(map);

                        L.marker(
                            [{{latitudTexto}}, {{longitudTexto}}])
                            .addTo(map);
                    </script>
                </body>
                </html>
                """;
        }
    }
}
