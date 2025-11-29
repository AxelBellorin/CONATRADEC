using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using CONATRADEC.Models;
using CONATRADEC.ViewModels;
using Microsoft.Maui.Controls;

namespace CONATRADEC.Views;

public partial class matrizPermisosPage : ContentPage
{
    // LISTA PARA GUARDAR TODOS LOS EXPANDERS GENERADOS EN LA COLLECTIONVIEW
    private readonly List<Expander> _expanders = new();

    public matrizPermisosPage()
    {
        Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
        BindingContext = new MatrizPermisosViewModel();
        InitializeComponent();
    }

    // REGISTRA CADA EXPANDER QUE APARECE EN EL LISTADO
    private void PermisosList_ChildAdded(object sender, ElementEventArgs e)
    {
        // Busca expanders dentro del DataTemplate
        if (e.Element is Expander expander)
        {
            if (!_expanders.Contains(expander))
                _expanders.Add(expander);
        }
        else
        {
            // También puede venir dentro de un Grid -> lo buscamos
            if (e.Element is Layout layout)
            {
                foreach (var child in layout.Children)
                {
                    if (child is Expander nestedExpander && !_expanders.Contains(nestedExpander))
                        _expanders.Add(nestedExpander);
                }
            }
        }
    }

    // SE EJECUTA CUANDO UN EXPANDER SE EXPANDE
    private void OnExpanderExpandedChanged(object sender, ExpandedChangedEventArgs e)
    {
        // Solo actuamos cuando se EXPANDE (true)
        if (!e.IsExpanded)
            return;

        var actual = sender as Expander;

        // Cerrar todos los demás Expanders
        foreach (var expander in _expanders)
        {
            if (expander != actual && expander.IsExpanded)
                expander.IsExpanded = false;
        }
    }

    // LIMPIA LISTA CUANDO ENTRAMOS A LA PÁGINA (POR SEGURIDAD)
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _expanders.Clear();
    }
}
