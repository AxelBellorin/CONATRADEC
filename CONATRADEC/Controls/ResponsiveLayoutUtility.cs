using Microsoft.Maui;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;

namespace CONATRADEC.Controls
{
    /// <summary>
    /// Utilidades visuales para adaptar composiciones existentes al ancho real
    /// disponible. No modifica datos, comandos, permisos ni navegación.
    /// </summary>
    internal static class ResponsiveLayoutUtility
    {
        public static T? FindDescendant<T>(
            IVisualTreeElement root,
            Func<T, bool> predicate)
            where T : class, IVisualTreeElement
        {
            foreach (IVisualTreeElement child in root.GetVisualChildren())
            {
                if (child is T typed && predicate(typed))
                    return typed;

                T? nested = FindDescendant(child, predicate);

                if (nested != null)
                    return nested;
            }

            return null;
        }

        public static IEnumerable<T> FindDescendants<T>(
            IVisualTreeElement root)
            where T : class, IVisualTreeElement
        {
            foreach (IVisualTreeElement child in root.GetVisualChildren())
            {
                if (child is T typed)
                    yield return typed;

                foreach (T nested in FindDescendants<T>(child))
                    yield return nested;
            }
        }

        public static T? FindAncestor<T>(Element? element)
            where T : Element
        {
            Element? current = element?.Parent;

            while (current != null)
            {
                if (current is T typed)
                    return typed;

                current = current.Parent;
            }

            return null;
        }

        public static Border? FindSectionCard(
            IVisualTreeElement root,
            string title)
        {
            Label? label = FindDescendant<Label>(
                root,
                current =>
                    string.Equals(
                        current.Text?.Trim(),
                        title,
                        StringComparison.OrdinalIgnoreCase));

            if (label == null)
                return null;

            Element? current = label.Parent;

            while (current != null)
            {
                if (current is Border border &&
                    border.Parent is Grid)
                {
                    return border;
                }

                current = current.Parent;
            }

            return null;
        }

        public static Grid? FindNearestGridByLabel(
            IVisualTreeElement root,
            string labelText)
        {
            Label? label = FindDescendant<Label>(
                root,
                current =>
                    string.Equals(
                        current.Text?.Trim(),
                        labelText,
                        StringComparison.OrdinalIgnoreCase));

            return label == null
                ? null
                : FindAncestor<Grid>(label);
        }

        public static View? FindDirectChildContaining(
            Grid grid,
            IVisualTreeElement descendant)
        {
            foreach (IView child in grid.Children)
            {
                if (child is not View view)
                    continue;

                if (ReferenceEquals(view, descendant) ||
                    Contains(view, descendant))
                {
                    return view;
                }
            }

            return null;
        }

        public static bool Contains(
            IVisualTreeElement root,
            IVisualTreeElement target)
        {
            foreach (IVisualTreeElement child in root.GetVisualChildren())
            {
                if (ReferenceEquals(child, target) ||
                    Contains(child, target))
                {
                    return true;
                }
            }

            return false;
        }

        public static void ConfigureStackedPair(
            Grid grid,
            View first,
            View second)
        {
            grid.ColumnDefinitions.Clear();
            grid.RowDefinitions.Clear();

            grid.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Star));
            grid.RowDefinitions.Add(
                new RowDefinition(GridLength.Auto));
            grid.RowDefinitions.Add(
                new RowDefinition(GridLength.Auto));

            Grid.SetRow(first, 0);
            Grid.SetColumn(first, 0);
            Grid.SetColumnSpan(first, 1);

            Grid.SetRow(second, 1);
            Grid.SetColumn(second, 0);
            Grid.SetColumnSpan(second, 1);
        }

        public static void ConfigureHorizontalPair(
            Grid grid,
            View first,
            View second,
            GridLength firstWidth,
            GridLength secondWidth)
        {
            grid.ColumnDefinitions.Clear();
            grid.RowDefinitions.Clear();

            grid.ColumnDefinitions.Add(
                new ColumnDefinition(firstWidth));
            grid.ColumnDefinitions.Add(
                new ColumnDefinition(secondWidth));
            grid.RowDefinitions.Add(
                new RowDefinition(GridLength.Auto));

            Grid.SetRow(first, 0);
            Grid.SetColumn(first, 0);
            Grid.SetColumnSpan(first, 1);

            Grid.SetRow(second, 0);
            Grid.SetColumn(second, 1);
            Grid.SetColumnSpan(second, 1);
        }
    }
}
