using CONATRADEC.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Controls; // por si acaso
using System.Globalization;

namespace CONATRADEC.Views
{
    public partial class terrenoFormPage : ContentPage
    {
        private readonly TerrenoFormViewModel viewModel;

        public terrenoFormPage()
        {
            InitializeComponent();
            viewModel = new TerrenoFormViewModel();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await viewModel.InicializarAsync();
            CargarMiniMapa();
        }

        private void CargarMiniMapa()
        {
            double lat = viewModel.Latitud ?? 12.1364;
            double lon = viewModel.Longitud ?? -86.2510;

            var html = BuildLeafletHtml(lat, lon);
            MiniMapaWeb.Source = new HtmlWebViewSource
            {
                Html = html
            };
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
#map {{ width:100%; height:100%; }}
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
