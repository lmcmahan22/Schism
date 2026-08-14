using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Schiism.WPF.Behaviors;

public static class LostFocusBehavior
{
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.RegisterAttached(
            "Command",
            typeof(ICommand),
            typeof(LostFocusBehavior),
            new PropertyMetadata(null, OnCommandChanged));

    public static void SetCommand(
        DependencyObject element,
        ICommand value)
    {
        element.SetValue(CommandProperty, value);
    }

    public static ICommand GetCommand(
        DependencyObject element)
    {
        return (ICommand)element.GetValue(CommandProperty);
    }

    private static void OnCommandChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is not TextBox textBox)
        {
            return;
        }

        textBox.LostFocus -= TextBox_LostFocus;

        if (e.NewValue is ICommand)
        {
            textBox.LostFocus += TextBox_LostFocus;
        }
    }

    private static void TextBox_LostFocus(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            return;
        }

        ICommand command = GetCommand(textBox);

        if (command.CanExecute(null))
        {
            command.Execute(null);
        }
    }
}
