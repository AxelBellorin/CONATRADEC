// ================================================================
//  Archivo: cargoFormPage.xaml.cs
//  Propósito:
//     Code-behind (código detrás) de la vista cargoFormPage.xaml.
//     Gestiona la inicialización del BindingContext y recibe parámetros
//     desde la navegación Shell (por ejemplo, el modo del formulario y el cargo actual).
// ================================================================

namespace CONATRADEC.Views;

using static CONATRADEC.Models.FormMode;  // Importa el enum FormModeSelect directamente
using CONATRADEC.Models;                  // Importa los modelos (CargoRequest, etc.)
using CONATRADEC.ViewModels;              // Importa el ViewModel asociado (CargoFormViewModel)

// ================================================================
// Atributos QueryProperty
// ---------------------------------------------------------------
// Permiten que la página reciba parámetros desde la navegación Shell.
// Ejemplo:
//     await Shell.Current.GoToAsync("//CargoFormPage", parameters);
// Donde 'parameters' contiene las claves "Mode" y "Cargo".
// ================================================================
[QueryProperty(nameof(Mode), "Mode")]     // Recibe el modo del formulario (Create, Edit, View)
[QueryProperty(nameof(Cargo), "Cargo")]   // Recibe el objeto Cargo (CargoRequest)

// ================================================================
//  Clase principal de la página
// ================================================================
public partial class cargoFormPage : ContentPage
{
    // ------------------------------------------------------------
    // ViewModel principal de esta vista (MVVM Pattern)
    // ------------------------------------------------------------
    private CargoFormViewModel viewModel = new CargoFormViewModel();

    // ============================================================
    //  Propiedad: Mode
    // ------------------------------------------------------------
    // Se asigna automáticamente cuando se navega hacia esta página.
    // Actualiza la propiedad Mode del ViewModel para controlar el comportamiento:
    // - Create: Campos editables y botón "Guardar".
    // - Edit:   Carga datos y permite modificar.
    // - View:   Solo lectura, sin botones de edición.
    // ============================================================
    public FormModeSelect Mode
    {
        set => viewModel.Mode = value;
    }

    // ============================================================
    // Propiedad: Cargo
    // ------------------------------------------------------------
    // Recibe un objeto CargoRequest con la información del cargo
    // que será editado o mostrado.
    // ============================================================
    public CargoRequest Cargo
    {
        set => viewModel.Cargo = value;
    }

    // ============================================================
    // Constructor de la página
    // ------------------------------------------------------------
    // - Deshabilita el menú lateral (Flyout) mientras está activa.
    // - Inicializa los componentes del XAML.
    // - Asigna el ViewModel al contexto de enlace (BindingContext),
    //   permitiendo que la vista acceda a sus propiedades y comandos.
    // ============================================================
    public cargoFormPage()
    {
        // Deshabilita el menú lateral de Shell (evita abrirlo en formularios)
        Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;

        // Conecta la vista con su ViewModel (MVVM binding)
        BindingContext = viewModel;

        // Carga el diseño visual (cargoFormPage.xaml)
        InitializeComponent();
    }
}
