using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AVCNDB.WPF.Controls;

/// <summary>
/// Zone de texte acceptant uniquement certains caractères
/// </summary>
public class CharTextBox : TextBox
{
    public static readonly DependencyProperty AllowedPatternProperty =
        DependencyProperty.Register(
            nameof(AllowedPattern),
            typeof(CharPattern),
            typeof(CharTextBox),
            new PropertyMetadata(CharPattern.Any));

    public static readonly DependencyProperty CustomPatternProperty =
        DependencyProperty.Register(
            nameof(CustomPattern),
            typeof(string),
            typeof(CharTextBox),
            new PropertyMetadata(string.Empty));

    public CharPattern AllowedPattern
    {
        get => (CharPattern)GetValue(AllowedPatternProperty);
        set => SetValue(AllowedPatternProperty, value);
    }

    public string CustomPattern
    {
        get => (string)GetValue(CustomPatternProperty);
        set => SetValue(CustomPatternProperty, value);
    }

    public CharTextBox()
    {
        DataObject.AddPastingHandler(this, OnPasteHandler);
    }

    protected override void OnPreviewTextInput(TextCompositionEventArgs e)
    {
        e.Handled = !IsTextAllowed(e.Text);
        base.OnPreviewTextInput(e);
    }

    private void OnPasteHandler(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(typeof(string)))
        {
            var text = (string?)e.DataObject.GetData(typeof(string));
            if (text == null || !IsTextAllowed(text))
            {
                e.CancelCommand();
            }
        }
        else
        {
            e.CancelCommand();
        }
    }

    private bool IsTextAllowed(string text)
    {
        var pattern = AllowedPattern switch
        {
            CharPattern.Numeric => @"^[0-9]+$",
            CharPattern.Decimal => @"^[0-9.,]+$",
            CharPattern.Alpha => @"^[a-zA-ZàâäéèêëïîôùûüçÀÂÄÉÈÊËÏÎÔÙÛÜÇ\s]+$",
            CharPattern.AlphaNumeric => @"^[a-zA-Z0-9àâäéèêëïîôùûüçÀÂÄÉÈÊËÏÎÔÙÛÜÇ\s]+$",
            CharPattern.Phone => @"^[0-9+\-\s()]+$",
            CharPattern.Email => @"^[a-zA-Z0-9@._\-]+$",
            CharPattern.Barcode => @"^[0-9]+$",
            CharPattern.Custom => CustomPattern,
            _ => @".*"
        };

        return Regex.IsMatch(text, pattern);
    }
}

/// <summary>
/// Types de patterns de caractères autorisés
/// </summary>
public enum CharPattern
{
    Any,
    Numeric,
    Decimal,
    Alpha,
    AlphaNumeric,
    Phone,
    Email,
    Barcode,
    Custom
}
