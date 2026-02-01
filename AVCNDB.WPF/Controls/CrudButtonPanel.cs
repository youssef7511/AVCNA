using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AVCNDB.WPF.Controls;

/// <summary>
/// Contrôle de boutons CRUD (Create, Read, Update, Delete)
/// </summary>
public class CrudButtonPanel : UserControl
{
    public static readonly DependencyProperty ShowNewButtonProperty =
        DependencyProperty.Register(nameof(ShowNewButton), typeof(bool), typeof(CrudButtonPanel), 
            new PropertyMetadata(true));

    public static readonly DependencyProperty ShowEditButtonProperty =
        DependencyProperty.Register(nameof(ShowEditButton), typeof(bool), typeof(CrudButtonPanel), 
            new PropertyMetadata(true));

    public static readonly DependencyProperty ShowDeleteButtonProperty =
        DependencyProperty.Register(nameof(ShowDeleteButton), typeof(bool), typeof(CrudButtonPanel), 
            new PropertyMetadata(true));

    public static readonly DependencyProperty ShowSaveButtonProperty =
        DependencyProperty.Register(nameof(ShowSaveButton), typeof(bool), typeof(CrudButtonPanel), 
            new PropertyMetadata(false));

    public static readonly DependencyProperty ShowCancelButtonProperty =
        DependencyProperty.Register(nameof(ShowCancelButton), typeof(bool), typeof(CrudButtonPanel), 
            new PropertyMetadata(false));

    public static readonly DependencyProperty NewCommandProperty =
        DependencyProperty.Register(nameof(NewCommand), typeof(ICommand), typeof(CrudButtonPanel));

    public static readonly DependencyProperty EditCommandProperty =
        DependencyProperty.Register(nameof(EditCommand), typeof(ICommand), typeof(CrudButtonPanel));

    public static readonly DependencyProperty DeleteCommandProperty =
        DependencyProperty.Register(nameof(DeleteCommand), typeof(ICommand), typeof(CrudButtonPanel));

    public static readonly DependencyProperty SaveCommandProperty =
        DependencyProperty.Register(nameof(SaveCommand), typeof(ICommand), typeof(CrudButtonPanel));

    public static readonly DependencyProperty CancelCommandProperty =
        DependencyProperty.Register(nameof(CancelCommand), typeof(ICommand), typeof(CrudButtonPanel));

    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(CrudButtonPanel));

    public bool ShowNewButton
    {
        get => (bool)GetValue(ShowNewButtonProperty);
        set => SetValue(ShowNewButtonProperty, value);
    }

    public bool ShowEditButton
    {
        get => (bool)GetValue(ShowEditButtonProperty);
        set => SetValue(ShowEditButtonProperty, value);
    }

    public bool ShowDeleteButton
    {
        get => (bool)GetValue(ShowDeleteButtonProperty);
        set => SetValue(ShowDeleteButtonProperty, value);
    }

    public bool ShowSaveButton
    {
        get => (bool)GetValue(ShowSaveButtonProperty);
        set => SetValue(ShowSaveButtonProperty, value);
    }

    public bool ShowCancelButton
    {
        get => (bool)GetValue(ShowCancelButtonProperty);
        set => SetValue(ShowCancelButtonProperty, value);
    }

    public ICommand NewCommand
    {
        get => (ICommand)GetValue(NewCommandProperty);
        set => SetValue(NewCommandProperty, value);
    }

    public ICommand EditCommand
    {
        get => (ICommand)GetValue(EditCommandProperty);
        set => SetValue(EditCommandProperty, value);
    }

    public ICommand DeleteCommand
    {
        get => (ICommand)GetValue(DeleteCommandProperty);
        set => SetValue(DeleteCommandProperty, value);
    }

    public ICommand SaveCommand
    {
        get => (ICommand)GetValue(SaveCommandProperty);
        set => SetValue(SaveCommandProperty, value);
    }

    public ICommand CancelCommand
    {
        get => (ICommand)GetValue(CancelCommandProperty);
        set => SetValue(CancelCommandProperty, value);
    }

    public object CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    static CrudButtonPanel()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CrudButtonPanel),
            new FrameworkPropertyMetadata(typeof(CrudButtonPanel)));
    }
}
