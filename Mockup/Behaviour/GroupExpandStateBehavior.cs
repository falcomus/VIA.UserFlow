// ======================================================================================
// FILE: Mockup/Behaviors/GroupExpandStateBehavior.cs
//
// ZWECK:
// - Persistiert Expander.IsExpanded pro Gruppe bei ICollectionView-Grouping.
// - Gruppennamen kommen aus CollectionViewGroup.Name (Binding: {Binding Name})
// - State wird im ViewModel (Host) über Dictionary gespeichert.
//
// XAML (GroupStyle-Expander):
//   xmlns:behav="clr-namespace:Mockup.Behaviors"
//
//   <Expander
//       Header="{Binding Name}"
//       behav:GroupExpandStateBehavior.Key="{Binding Name}"
//       behav:GroupExpandStateBehavior.Host="{Binding DataContext, RelativeSource={RelativeSource AncestorType=UserControl}}"
//       ... />
// ======================================================================================

using System.Windows;
using System.Windows.Controls;

namespace Mockup.Behaviors;

public interface IGroupExpandStateHost
{
    bool GetGroupExpanded(string key);
    void SetGroupExpanded(string key, bool expanded);
}

public static class GroupExpandStateBehavior
{
    #region === ATTACHED: Host ===========================================================

    public static readonly DependencyProperty HostProperty =
        DependencyProperty.RegisterAttached(
            "Host",
            typeof(object),
            typeof(GroupExpandStateBehavior),
            new PropertyMetadata(null, OnParamsChanged));

    public static void SetHost(DependencyObject element, object value)
        => element.SetValue(HostProperty, value);

    public static object GetHost(DependencyObject element)
        => element.GetValue(HostProperty);

    #endregion

    #region === ATTACHED: Key ============================================================

    public static readonly DependencyProperty KeyProperty =
        DependencyProperty.RegisterAttached(
            "Key",
            typeof(string),
            typeof(GroupExpandStateBehavior),
            new PropertyMetadata(null, OnParamsChanged));

    public static void SetKey(DependencyObject element, string value)
        => element.SetValue(KeyProperty, value);

    public static string GetKey(DependencyObject element)
        => (string)element.GetValue(KeyProperty);

    #endregion

    #region === INTERNAL FLAGS ===========================================================

    private static readonly DependencyProperty IsHookedProperty =
        DependencyProperty.RegisterAttached(
            "IsHooked",
            typeof(bool),
            typeof(GroupExpandStateBehavior),
            new PropertyMetadata(false));

    private static void SetIsHooked(DependencyObject element, bool value)
        => element.SetValue(IsHookedProperty, value);

    private static bool GetIsHooked(DependencyObject element)
        => (bool)element.GetValue(IsHookedProperty);

    private static readonly DependencyProperty IsUpdatingProperty =
        DependencyProperty.RegisterAttached(
            "IsUpdating",
            typeof(bool),
            typeof(GroupExpandStateBehavior),
            new PropertyMetadata(false));

    private static void SetIsUpdating(DependencyObject element, bool value)
        => element.SetValue(IsUpdatingProperty, value);

    private static bool GetIsUpdating(DependencyObject element)
        => (bool)element.GetValue(IsUpdatingProperty);

    #endregion

    #region === DP CALLBACKS =============================================================

    private static void OnParamsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Expander exp)
            return;

        EnsureHooked(exp);
        ApplyFromHost(exp);
    }

    #endregion

    #region === HOOK / UNHOOK ============================================================

    private static void EnsureHooked(Expander exp)
    {
        if (GetIsHooked(exp))
            return;

        exp.Loaded += Exp_Loaded;
        exp.Unloaded += Exp_Unloaded;
        exp.Expanded += Exp_Expanded;
        exp.Collapsed += Exp_Collapsed;

        SetIsHooked(exp, true);
    }

    private static void Exp_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is Expander exp)
            ApplyFromHost(exp);
    }

    private static void Exp_Unloaded(object sender, RoutedEventArgs e)
    {
        if (sender is not Expander exp)
            return;

        exp.Loaded -= Exp_Loaded;
        exp.Unloaded -= Exp_Unloaded;
        exp.Expanded -= Exp_Expanded;
        exp.Collapsed -= Exp_Collapsed;

        SetIsHooked(exp, false);
    }

    #endregion

    #region === APPLY / STORE ============================================================

    private static void ApplyFromHost(Expander exp)
    {
        if (GetIsUpdating(exp))
            return;

        var key = GetKey(exp);
        if (string.IsNullOrWhiteSpace(key))
            return;

        if (GetHost(exp) is not IGroupExpandStateHost host)
            return;

        bool desired;
        try
        {
            desired = host.GetGroupExpanded(key);
        }
        catch
        {
            return;
        }

        if (exp.IsExpanded == desired)
            return;

        SetIsUpdating(exp, true);
        exp.IsExpanded = desired;
        SetIsUpdating(exp, false);
    }

    private static void StoreToHost(Expander exp, bool expanded)
    {
        if (GetIsUpdating(exp))
            return;

        var key = GetKey(exp);
        if (string.IsNullOrWhiteSpace(key))
            return;

        if (GetHost(exp) is not IGroupExpandStateHost host)
            return;

        try
        {
            host.SetGroupExpanded(key, expanded);
        }
        catch
        {
            // UI darf nicht crashen
        }
    }

    #endregion

    #region === EVENTS ===================================================================

    private static void Exp_Expanded(object sender, RoutedEventArgs e)
    {
        if (sender is Expander exp)
            StoreToHost(exp, true);
    }

    private static void Exp_Collapsed(object sender, RoutedEventArgs e)
    {
        if (sender is Expander exp)
            StoreToHost(exp, false);
    }

    #endregion
}
