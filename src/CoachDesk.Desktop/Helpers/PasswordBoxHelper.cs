using System.Windows;
using System.Windows.Controls;

namespace CoachDesk.Desktop.Helpers;

public static class PasswordBoxHelper
{
    public static readonly DependencyProperty BoundPasswordProperty =
        DependencyProperty.RegisterAttached(
            "BoundPassword",
            typeof(string),
            typeof(PasswordBoxHelper),
            new FrameworkPropertyMetadata(string.Empty, OnBoundPasswordChanged));

    private static readonly DependencyProperty IsUpdatingProperty =
        DependencyProperty.RegisterAttached(
            "IsUpdating",
            typeof(bool),
            typeof(PasswordBoxHelper));

    public static readonly DependencyProperty IsPasswordBindingEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsPasswordBindingEnabled",
            typeof(bool),
            typeof(PasswordBoxHelper),
            new PropertyMetadata(false, OnPasswordBindingEnabledChanged));

    public static string GetBoundPassword(DependencyObject element) =>
        (string)element.GetValue(BoundPasswordProperty);

    public static void SetBoundPassword(DependencyObject element, string value) =>
        element.SetValue(BoundPasswordProperty, value);

    public static bool GetIsPasswordBindingEnabled(DependencyObject element) =>
        (bool)element.GetValue(IsPasswordBindingEnabledProperty);

    public static void SetIsPasswordBindingEnabled(DependencyObject element, bool value) =>
        element.SetValue(IsPasswordBindingEnabledProperty, value);

    private static bool GetIsUpdating(DependencyObject element) =>
        (bool)element.GetValue(IsUpdatingProperty);

    private static void SetIsUpdating(DependencyObject element, bool value) =>
        element.SetValue(IsUpdatingProperty, value);

    private static void OnBoundPasswordChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is not PasswordBox passwordBox)
        {
            return;
        }

        passwordBox.PasswordChanged -= PasswordBoxPasswordChanged;
        if (!GetIsUpdating(passwordBox))
        {
            passwordBox.Password = e.NewValue as string ?? string.Empty;
        }

        passwordBox.PasswordChanged += PasswordBoxPasswordChanged;
    }

    private static void OnPasswordBindingEnabledChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is not PasswordBox passwordBox)
        {
            return;
        }

        passwordBox.PasswordChanged -= PasswordBoxPasswordChanged;
        if ((bool)e.NewValue)
        {
            passwordBox.PasswordChanged += PasswordBoxPasswordChanged;
        }
    }

    private static void PasswordBoxPasswordChanged(object sender, RoutedEventArgs e)
    {
        var passwordBox = (PasswordBox)sender;
        SetIsUpdating(passwordBox, true);
        SetBoundPassword(passwordBox, passwordBox.Password);
        SetIsUpdating(passwordBox, false);
    }
}
