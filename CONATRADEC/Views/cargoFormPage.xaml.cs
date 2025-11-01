// ================================================================
//  Archivo: cargoFormPage.xaml.cs
//  Prop�sito:
//     Code-behind (c�digo detr�s) de la vista cargoFormPage.xaml.
//     Gestiona la inicializaci�n del BindingContext y recibe par�metros
//     desde la navegaci�n Shell (por ejemplo, el modo del formulario y el cargo actual).
// ================================================================

namespace CONATRADEC.Views;

using static CONATRADEC.Models.FormMode;  // Importa el enum FormModeSelect directamente
using CONATRADEC.Models;                  // Importa los modelos (CargoRequest, etc.)
using CONATRADEC.ViewModels;              // Importa el ViewModel asociado (CargoFormViewModel)

// ================================================================
// Atributos QueryProperty
// ---------------------------------------------------------------
// Permiten que la p�gina reciba par�metros desde la navegaci�n Shell.
// Ejemplo:
//     await Shell.Current.GoToAsync("//CargoFormPage", parameters);
// Donde 'parameters' contiene las claves "Mode" y "Cargo".
// ================================================================
[QueryProperty(nameof(Mode), "Mode")]     // Recibe el modo del formulario (Create, Edit, View)
[QueryProperty(nameof(Cargo), "Cargo")]   // Recibe el objeto Cargo (CargoRequest)

// ================================================================
//  Clase principal de la p�gina
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
    // Se asigna autom�ticamente cuando se navega hacia esta p�gina.
    // Actualiza la propiedad Mode del ViewModel para controlar el comportamiento:
    // - Create: Campos editables y bot�n "Guardar".
    // - Edit:   Carga datos y permite modificar.
    // - View:   Solo lectura, sin botones de edici�n.
    // ============================================================
    public FormModeSelect Mode
    {
        set => viewModel.Mode = value;
    }

    // ============================================================
    // Propiedad: Cargo
    // ------------------------------------------------------------
    // Recibe un objeto CargoRequest con la informaci�n del cargo
    // que ser� editado o mostrado.
    // ============================================================
    public CargoRequest Cargo
    {
        set => viewModel.Cargo = value;
    }

    // ============================================================
    // Constructor de la p�gina
    // ------------------------------------------------------------
    // - Deshabilita el men� lateral (Flyout) mientras est� activa.
    // - Inicializa los componentes del XAML.
    // - Asigna el ViewModel al contexto de enlace (BindingContext),
    //   permitiendo que la vista acceda a sus propiedades y comandos.
    // ============================================================
    public cargoFormPage()
    {
        // Deshabilita el men� lateral de Shell (evita abrirlo en formularios)
        Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;

        // Conecta la vista con su ViewModel (MVVM binding)
        BindingContext = viewModel;

        // Carga el dise�o visual (cargoFormPage.xaml)
        InitializeComponent();
    }
}
