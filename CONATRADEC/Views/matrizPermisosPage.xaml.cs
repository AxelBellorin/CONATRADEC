using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using Microsoft.Maui.Controls;

namespace CONATRADEC.Views
{
    public partial class matrizPermisosPage : ContentPage
    {
        private readonly List<Expander> expanders = new();
        private readonly MatrizPermisosViewModel viewModel = new();

        public matrizPermisosPage()
        {
            InitializeComponent();

            BindingContext = viewModel;
            Shell.SetFlyoutBehavior(this, FlyoutBehavior.Disabled);
        }

        private void PermisosList_ChildAdded(
            object sender,
            ElementEventArgs e)
        {
            if (e.Element is Expander expander)
            {
                AgregarExpander(expander);
                return;
            }

            if (e.Element is not Layout layout)
                return;

            foreach (var child in layout.Children)
            {
                if (child is Expander nestedExpander)
                    AgregarExpander(nestedExpander);
            }
        }

        private void AgregarExpander(Expander expander)
        {
            if (!expanders.Contains(expander))
                expanders.Add(expander);
        }

        private void OnExpanderExpandedChanged(
            object sender,
            ExpandedChangedEventArgs e)
        {
            if (!e.IsExpanded || sender is not Expander actual)
                return;

            foreach (var expander in expanders)
            {
                if (!ReferenceEquals(expander, actual) &&
                    expander.IsExpanded)
                {
                    expander.IsExpanded = false;
                }
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                viewModel.LoadPagePermissions("matrizPermisosPage");

                if (!viewModel.CanView)
                {
                    await GlobalService.MostrarToastAsync(
                        "No tiene permisos para ver la matriz de permisos.");

                    await Shell.Current.GoToAsync("//MainPage");
                    return;
                }

                viewModel.UsuarioPuedeEditar = viewModel.CanEdit;

                expanders.Clear();

                await viewModel.InicializarAsync();
            }
            catch (Exception)
            {
                await GlobalService.MostrarToastAsync(
                    "Ocurrió un error inesperado al abrir la matriz de permisos.");
            }
        }

        protected override void OnDisappearing()
        {
            viewModel.CancelarOperaciones();
            base.OnDisappearing();
        }
    }
}
