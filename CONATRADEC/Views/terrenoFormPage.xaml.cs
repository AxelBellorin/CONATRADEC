using CONATRADEC.ViewModels;
using Microsoft.Maui.Controls;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CONATRADEC.Views
{
    public partial class terrenoFormPage : ContentPage
    {
        private static readonly Regex CedulaRegex = new(
            @"^\d{3}-\d{6}-\d{4}[A-Z]$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly TerrenoFormViewModel viewModel;

        private bool actualizandoCedula;
        private bool actualizandoNumero;
        private bool actualizandoCoordenadasTexto;

        public terrenoFormPage()
        {
            InitializeComponent();

            viewModel = new TerrenoFormViewModel();
            BindingContext = viewModel;

            viewModel.RefrescarMapaAction = (lat, lon) =>
            {
                ActualizarMiniMapa(lat, lon);
                SincronizarEntradasCoordenadas(lat, lon);
            };
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await viewModel.InicializarAsync();
            CargarMiniMapa();
            SincronizarEntradasCoordenadas(
                viewModel.Latitud,
                viewModel.Longitud);
        }

        /*
         * No se cancela ninguna solicitud en OnDisappearing.
         *
         * .NET MAUI ejecuta OnDisappearing también al abrir el selector de mapa,
         * la galería o una aplicación externa. Cancelar aquí provoca que las
         * solicitudes HTTP de países, departamentos, municipios o fotografías
         * terminen con TaskCanceledException aunque el usuario solo esté
         * seleccionando una ubicación.
         *
         * Las solicitudes ya se reemplazan/cancelan de forma controlada dentro
         * del ViewModel cuando comienza una operación nueva.
         */

        private void CargarMiniMapa()
        {
            double lat = viewModel.Latitud ?? 12.1364;
            double lon = viewModel.Longitud ?? -86.2510;

            MiniMapaWeb.Source = new HtmlWebViewSource
            {
                Html = BuildLeafletHtml(lat, lon)
            };
        }

        private void ActualizarMiniMapa(double? lat, double? lon)
        {
            if (lat == null || lon == null)
                return;

            string html = BuildLeafletHtml(lat.Value, lon.Value);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                MiniMapaWeb.Source = new HtmlWebViewSource
                {
                    Html = html
                };
            });
        }

        private void SincronizarEntradasCoordenadas(
            double? lat,
            double? lon)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                LatitudEntry.Text = lat?.ToString(
                    "0.########",
                    CultureInfo.InvariantCulture) ?? string.Empty;

                LongitudEntry.Text = lon?.ToString(
                    "0.########",
                    CultureInfo.InvariantCulture) ?? string.Empty;

                if (!lat.HasValue || !lon.HasValue)
                    return;

                string coordenadas =
                    $"{lat.Value.ToString("0.########", CultureInfo.InvariantCulture)}, " +
                    $"{lon.Value.ToString("0.########", CultureInfo.InvariantCulture)}";

                // Cuando el usuario está pegando una coordenada se conserva
                // temporalmente lo que escribe. GPS y selección en mapa sí
                // actualizan este campo de forma automática.
                if (CoordenadasEntry.IsFocused)
                    return;

                actualizandoCoordenadasTexto = true;

                try
                {
                    CoordenadasEntry.Text = coordenadas;
                }
                finally
                {
                    actualizandoCoordenadasTexto = false;
                }
            });
        }

        private async void BtnAbrirEnMaps_Clicked(
            object sender,
            EventArgs e)
        {
            if (BindingContext is TerrenoFormViewModel vm &&
                vm.Latitud != null &&
                vm.Longitud != null)
            {
                await vm.AbrirEnGoogleMaps(
                    vm.Latitud.Value,
                    vm.Longitud.Value);
            }
        }

        private async void BtnGuardar_Clicked(
            object sender,
            EventArgs e)
        {
            string cedula =
                IdentificacionEntry.Text?.Trim().ToUpperInvariant()
                ?? string.Empty;

            if (!CedulaRegex.IsMatch(cedula))
            {
                await DisplayAlert(
                    "Identificación inválida",
                    "La identificación debe tener el formato 001-080701-1050R.",
                    "Aceptar");

                IdentificacionEntry.Focus();
                return;
            }

            viewModel.IdentificacionPropietarioTerreno = cedula;

            if (viewModel.SaveCommand.CanExecute(null))
                viewModel.SaveCommand.Execute(null);
        }

        private void IdentificacionEntry_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            if (actualizandoCedula || sender is not Entry entry)
                return;

            string textoFormateado = FormatearCedula(e.NewTextValue);

            if (string.Equals(
                    entry.Text,
                    textoFormateado,
                    StringComparison.Ordinal))
            {
                return;
            }

            actualizandoCedula = true;

            try
            {
                entry.Text = textoFormateado;
                entry.CursorPosition = textoFormateado.Length;
            }
            finally
            {
                actualizandoCedula = false;
            }
        }

        private static string FormatearCedula(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return string.Empty;

            string limpio = new(
                valor
                    .ToUpperInvariant()
                    .Where(char.IsLetterOrDigit)
                    .ToArray());

            string digitos = new(
                limpio
                    .Where(char.IsDigit)
                    .Take(13)
                    .ToArray());

            char? letra = limpio
                .Where(char.IsLetter)
                .Cast<char?>()
                .LastOrDefault();

            var resultado = new StringBuilder(16);

            int primerGrupo = Math.Min(3, digitos.Length);
            resultado.Append(digitos.AsSpan(0, primerGrupo));

            if (digitos.Length >= 3)
                resultado.Append('-');

            if (digitos.Length > 3)
            {
                int segundoGrupo = Math.Min(6, digitos.Length - 3);
                resultado.Append(digitos.AsSpan(3, segundoGrupo));
            }

            if (digitos.Length >= 9)
                resultado.Append('-');

            if (digitos.Length > 9)
            {
                int tercerGrupo = Math.Min(4, digitos.Length - 9);
                resultado.Append(digitos.AsSpan(9, tercerGrupo));
            }

            if (digitos.Length == 13 && letra.HasValue)
                resultado.Append(letra.Value);

            return resultado.ToString();
        }

        private void DecimalDosDigitos_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            if (actualizandoNumero || sender is not Entry entry)
                return;

            string textoFiltrado = FiltrarDecimalDosDigitos(e.NewTextValue);

            if (string.Equals(
                    entry.Text,
                    textoFiltrado,
                    StringComparison.Ordinal))
            {
                return;
            }

            actualizandoNumero = true;

            try
            {
                entry.Text = textoFiltrado;
                entry.CursorPosition = textoFiltrado.Length;
            }
            finally
            {
                actualizandoNumero = false;
            }
        }

        private static string FiltrarDecimalDosDigitos(string? valor)
        {
            if (string.IsNullOrEmpty(valor))
                return string.Empty;

            string separador = CultureInfo.CurrentCulture
                .NumberFormat
                .NumberDecimalSeparator;

            var resultado = new StringBuilder();
            bool tieneSeparador = false;
            int cantidadDecimales = 0;

            foreach (char caracter in valor)
            {
                if (char.IsDigit(caracter))
                {
                    if (tieneSeparador && cantidadDecimales >= 2)
                        continue;

                    resultado.Append(caracter);

                    if (tieneSeparador)
                        cantidadDecimales++;

                    continue;
                }

                if ((caracter == '.' || caracter == ',') &&
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
            if (actualizandoNumero || sender is not Entry entry)
                return;

            string textoFiltrado = new(
                (e.NewTextValue ?? string.Empty)
                    .Where(char.IsDigit)
                    .ToArray());

            if (string.Equals(
                    entry.Text,
                    textoFiltrado,
                    StringComparison.Ordinal))
            {
                return;
            }

            actualizandoNumero = true;

            try
            {
                entry.Text = textoFiltrado;
                entry.CursorPosition = textoFiltrado.Length;
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

            viewModel.CoordenadasTexto = e.NewTextValue;
        }

        private string BuildLeafletHtml(double lat, double lon)
        {
            string latStr = lat.ToString(CultureInfo.InvariantCulture);
            string lonStr = lon.ToString(CultureInfo.InvariantCulture);

            return $@"
<!DOCTYPE html>
<html>
<head>
<meta name='viewport' content='width=device-width, initial-scale=1.0'>
<link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css' />
<script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
<style>
html, body {{ margin:0; padding:0; height:100%; }}
#map {{ width:100%; height:100%; border-radius:10px; }}
</style>
</head>
<body>
<div id='map'></div>
<script>
var map = L.map('map').setView([{latStr}, {lonStr}], 16);
L.tileLayer('https://tile.openstreetmap.org/{{z}}/{{x}}/{{y}}.png', {{
    maxZoom: 19
}}).addTo(map);
var marker = L.marker([{latStr}, {lonStr}]).addTo(map);
</script>
</body>
</html>";
        }
    }
}