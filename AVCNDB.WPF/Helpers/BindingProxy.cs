using System.Windows;

namespace AVCNDB.WPF.Helpers;

/// <summary>
/// A Freezable-based proxy that allows DataContext bindings to cross
/// visual-tree boundaries (e.g. inside PopupBox, ContextMenu, Popup).
/// Usage: add as a <DataGrid.Resources> StaticResource, then bind
/// commands via {Binding Data.MyCommand, Source={StaticResource proxy}}.
/// </summary>
public class BindingProxy : Freezable
{
    public static readonly DependencyProperty DataProperty =
        DependencyProperty.Register(
            nameof(Data),
            typeof(object),
            typeof(BindingProxy),
            new UIPropertyMetadata(null));

    public object Data
    {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    protected override Freezable CreateInstanceCore() => new BindingProxy();
}
