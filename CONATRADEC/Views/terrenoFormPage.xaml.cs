using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using System.Globalization;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views;

public partial class terrenoFormPage : ContentPage
{
    private readonly TerrenoFormViewModel vm;

    public terrenoFormPage()
    {
        InitializeComponent();
        vm = new TerrenoFormViewModel();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await vm.InicializarAsync();

        //CargarMiniMapa();
    }

//    private void CargarMiniMapa()
//    {
//#if WINDOWS
//        MiniMapa.IsVisible = false;
//        MiniMapaWeb.IsVisible = true;

//        MiniMapaWeb.Source = new HtmlWebViewSource
//        {
//            Html = BuildLeafletHtml(vm.Latitud ?? 12.1364,
//                                    vm.Longitud ?? -86.2510)
//        };
//#else
//        MiniMapa.IsVisible = true;
//        MiniMapaWeb.IsVisible = false;

//        var loc = new Location(vm.Latitud ?? 12.1364, vm.Longitud ?? -86.2510);

//        MiniMapa.MoveToRegion(MapSpan.FromCenterAndRadius(loc, Distance.FromKilometers(1)));

//        MiniMapa.Pins.Clear();
//        MiniMapa.Pins.Add(new Pin
//        {
//            Label = "Terreno",
//            Location = loc
//        });
//#endif
//    }

    private string BuildLeafletHtml(double lat, double lon)
    {
        return @$"
<!DOCTYPE html>
<html>
<head>
<meta name='viewport' content='width=device-width, initial-scale=1'>
<link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css' />
<script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
<style>
html, body, #map {{ height: 100%; margin: 0; }}
</style>
</head>

<body>
<div id='map'></div>
<script>
var map = L.map('map').setView([{lat}, {lon}], 16);
L.tileLayer('https://tile.openstreetmap.org/{{z}}/{{x}}/{{y}}.png').addTo(map);
L.marker([{lat}, {lon}]).addTo(map);
</script>
</body>
</html>";
    }
}
