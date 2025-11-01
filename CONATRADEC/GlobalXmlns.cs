using System.Windows.Input;
// Permite usar la interfaz ICommand en todo el proyecto (por ejemplo, en bindings de comandos en XAML o ViewModels).

// ============================================================================
// Archivo: GlobalUsings.cs (o similar)
// Propósito:
//     Define los espacios de nombres XML (xmlns) que estarán disponibles de forma
//     global en todos los archivos XAML del proyecto.
//     Esto simplifica la referencia a las clases y páginas del proyecto,
//     evitando tener que escribir el "clr-namespace" cada vez.
// ============================================================================

// ============================================================================
//     [assembly: XmlnsDefinition()]
//     Asocia un espacio de nombres XAML ("http://schemas.microsoft.com/dotnet/maui/global")
//     con uno o más namespaces del código C#.
//     Así, cualquier Page o control dentro de esos namespaces
//     puede ser utilizado directamente desde XAML.
// ============================================================================

// Primer namespace global (raíz del proyecto)
[assembly: XmlnsDefinition("http://schemas.microsoft.com/dotnet/maui/global", "CONATRADEC")]

// Segundo namespace global (subcarpeta que contiene tus Pages o Views)
[assembly: XmlnsDefinition("http://schemas.microsoft.com/dotnet/maui/global", "CONATRADEC.Pages")]

// ============================================================================
// Ejemplo práctico de uso:
//     Antes (sin este archivo):
//         xmlns:views="clr-namespace:CONATRADEC.Views"
//
//     Después (con este archivo):
//         xmlns:views="http://schemas.microsoft.com/dotnet/maui/global"
//
//     Esto permite usar:
//         <views:MainPage />
//     sin tener que definir el namespace manualmente en cada XAML.
// ============================================================================

