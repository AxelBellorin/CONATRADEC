// ================================================================
//  Archivo: paisFormPage.xaml.cs
//  Propósito:
//     Code-behind (código detrás) de la vista paisFormPage.xaml.
//     Gestiona la inicialización del BindingContext y recibe parámetros
//     desde la navegación Shell (por ejemplo, el modo del formulario y el país actual).
// ================================================================

namespace CONATRADEC.Views;

using static CONATRADEC.Models.FormMode;  // Importa el enum FormModeSelect directamente
using CONATRADEC.Models;                  // Importa los modelos (PaisRequest, etc.)
using CONATRADEC.ViewModels;              // Importa el ViewModel asociado (PaisFormViewModel)

// ================================================================
// Atributos QueryProperty
// ---------------------------------------------------------------
// Permiten que la página reciba parámetros desde la navegación Shell.
// Ejemplo:
//     await Shell.Current.GoToAsync("//PaisFormPage", parameters);
// Donde 'parameters' contiene las claves "Mode" y "Pais".
// ================================================================
[QueryProperty(nameof(Mode), "Mode")]   // Recibe el modo del formulario (Create, Edit, View)
[QueryProperty(nameof(Pais), "Pais")]   // Recibe el objeto País (PaisRequest)
[QueryProperty(nameof(Departamento), "Departamento")]   // Recibe el objeto Departamento (DepartamentoRequest)

// ================================================================
//  Clase principal de la página
// ================================================================
public partial class departamentoFormPage : ContentPage
{
    // ------------------------------------------------------------
    // ViewModel principal de esta vista (patrón MVVM)
    // ------------------------------------------------------------
    private readonly DepartamentoFormViewModel viewModel = new DepartamentoFormViewModel();

    // ============================================================
    //  Propiedad: Mode
    // ------------------------------------------------------------
    // Se asigna automáticamente cuando se navega hacia esta página.
    // Actualiza la propiedad Mode del ViewModel para controlar el comportamiento:
    // - Create: Campos editables y botón "Guardar".
    // - Edit:   Carga datos y permite modificar.
    // - View:   Solo lectura, sin botón Guardar.
    // ============================================================
    public FormModeSelect Mode
    {
        set => viewModel.Mode = value;
    }

    // ============================================================
    // Propiedad: Pais
    // ------------------------------------------------------------
    // Recibe un objeto PaisRequest con la información del país
    // que será creado, editado o mostrado.
    // ============================================================
    public PaisRequest Pais
    {
        set => viewModel.PaisRequest = value;
    }

    public DepartamentoRequest Departamento
    {
        set => viewModel.Departamento = value;
    }

    // ============================================================
    // Constructor de la página
    // ------------------------------------------------------------
    // - Deshabilita el menú lateral (Flyout) mientras está activa.
    // - Inicializa los componentes del XAML.
    // - Asigna el ViewModel al contexto de enlace (BindingContext),
    //   permitiendo que la vista acceda a sus propiedades y comandos.
    // ============================================================
    public departamentoFormPage()
    {
        // Deshabilita el menú lateral de Shell (evita abrirlo en formularios)
        Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;

        // Conecta la vista con su ViewModel (MVVM binding)
        BindingContext = viewModel;

        // Carga el diseño visual (paisFormPage.xaml)
        InitializeComponent();
    }
}
