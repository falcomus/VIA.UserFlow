// ======================================================================================
// FILE: Mockup.UIControls/PropertyEditor.xaml.cs
//
// ZWECK:
//   Temporäres PropertyGrid für den Designer.
//
// STAND:
//   - Führende Quelle für Bearbeitung ist SelectedControls
//   - Control bleibt als Fallback für Single-Selection / Altstellen erhalten
//   - Properties werden per Reflection aus [ControlProp]-Properties aufgebaut
//   - Gruppierung erfolgt über CategoryAttribute
//   - Änderungen werden auf alle selektierten Controls angewendet
//   - Controls ohne passende Property werden ignoriert
//
// HINWEIS:
//   - SelectedItem wird bewusst ausgeblendet
//   - Items (List<string> / ObservableCollection<string>) werden als Mehrzeilen-Text editiert
//   - Color-Änderung setzt Variant automatisch auf Custom, sofern vorhanden
//   - Bei Selektionswechsel werden Property-Änderungen kurz gesperrt
//   - Editoren schreiben nur noch über Events zurück, nicht mehr über direkte TwoWay-Bindings
//   - DateTime wird über DatePicker bearbeitet
// ======================================================================================

using CommunityToolkit.Mvvm.ComponentModel;
using Mockup.AssetSystem;
using Mockup.Dialogs;
using Mockup.Domain.Registry;
using Mockup.Messages;
using Mockup.Snapshots;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using VIA.WPF.Controls;
using SkiaColor = SkiaSharp.SKColor;

namespace Mockup.UIControls;

[ObservableObject]
public partial class PropertyEditor : UserControl
{
    #region ### Fields ###

    private bool _isRefreshingFromControl;
    private bool _isHandlingSelectionChange;
    private bool _suspendApplyAfterSelectionChange;
    private DispatcherOperation? _pendingPositionRefreshOperation;

    private INotifyCollectionChanged? _observedSelectedControls;
    private readonly List<INotifyPropertyChanged> _observedTargets = new();
    private readonly HashSet<PropertyItemTemp> _activeTextEditSnapshotItems = [];
    private readonly HashSet<PropertyItemTemp> _activeNumericEditSnapshotItems = [];
    private readonly Dictionary<TextBox, DispatcherTimer> _liveTextTimers = new();
    private readonly Dictionary<PropertyItemTemp, DispatcherTimer> _numericEditTimers = new();
    private readonly Dictionary<XDatePicker, EventHandler> _datePickerValueHandlers = new();
    private Type? _currentSchemaType;

    #endregion

    #region ### Header Text ###

    public string SelectionHeaderText
    {
        get
        {
            var selected = GetSelectedControls();

            if (selected.Count > 1)
                return "<Multiple Controls selected>";

            var active = ActiveControl;
            if (active == null)
                return "<no control selected>";

            return active.TypeKey;
        }
    }

    #endregion

    #region ### Ctor ###

    public PropertyEditor()
    {
        InitializeComponent();
        Unloaded += PropertyEditor_Unloaded;
    }

    #endregion

    #region ### Cleanup ###

    private void PropertyEditor_Unloaded(object sender, RoutedEventArgs e)
    {
        UnhookSelectedControlsCollectionChanged();
        UnsubscribeFromCurrentTargets();

        if (_pendingPositionRefreshOperation?.Status == DispatcherOperationStatus.Pending)
            _pendingPositionRefreshOperation.Abort();

        _pendingPositionRefreshOperation = null;

        foreach (var timer in _liveTextTimers.Values)
            timer.Stop();

        foreach (var timer in _numericEditTimers.Values)
            timer.Stop();

        _liveTextTimers.Clear();
        _numericEditTimers.Clear();
        _activeTextEditSnapshotItems.Clear();
        _activeNumericEditSnapshotItems.Clear();

        foreach (var pair in _datePickerValueHandlers)
            DependencyPropertyDescriptor.FromProperty(XDatePicker.SelectedDateProperty, typeof(XDatePicker))?.RemoveValueChanged(pair.Key, pair.Value);

        _datePickerValueHandlers.Clear();
    }

    #endregion

    #region ### DependencyProperties ###

    public DesignControl? Control
    {
        get => (DesignControl?)GetValue(ControlProperty);
        set => SetValue(ControlProperty, value);
    }

    public static readonly DependencyProperty ControlProperty =
        DependencyProperty.Register(
            nameof(Control),
            typeof(DesignControl),
            typeof(PropertyEditor),
            new PropertyMetadata(null, OnSelectionSourceChanged));

    public IList? SelectedControls
    {
        get => (IList?)GetValue(SelectedControlsProperty);
        set => SetValue(SelectedControlsProperty, value);
    }

    public static readonly DependencyProperty SelectedControlsProperty =
        DependencyProperty.Register(
            nameof(SelectedControls),
            typeof(IList),
            typeof(PropertyEditor),
            new PropertyMetadata(null, OnSelectionSourceChanged));

    #endregion

    #region ### Public State ###

    public ObservableCollection<PropertyGroupTemp> PropertyGroups { get; } = new();
    public ObservableCollection<PropertyCategoryTemp> PropertyCategories { get; } = new();
    public ObservableCollection<PropertyItemTemp> VisibleProperties { get; } = new();

    private PropertyCategoryTemp? _selectedPropertyCategory;
    public PropertyCategoryTemp? SelectedPropertyCategory
    {
        get => _selectedPropertyCategory;
        set
        {
            if (ReferenceEquals(_selectedPropertyCategory, value))
                return;

            _selectedPropertyCategory = value;
            OnPropertyChanged(nameof(SelectedPropertyCategory));
            RefreshPropertyFilter();
        }
    }

    #endregion

    #region ### Selection Source ###

    private static void OnSelectionSourceChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        var control = (PropertyEditor)d;
        control.HandleSelectionSourceChanged();
    }

    private void HandleSelectionSourceChanged()
    {
        UnhookSelectedControlsCollectionChanged();

        if (SelectedControls is INotifyCollectionChanged incc)
        {
            _observedSelectedControls = incc;
            _observedSelectedControls.CollectionChanged += SelectedControls_CollectionChanged;
        }

        RefreshSelectionUI();
    }

    private void SelectedControls_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshSelectionUI();
    }

    private void RefreshSelectionUI()
    {
        _isHandlingSelectionChange = true;
        _suspendApplyAfterSelectionChange = true;

        try
        {
            if (RequiresPropertyGroupRebuild())
            {
                RebuildPropertyGroups();
            }
            else
            {
                UnsubscribeFromCurrentTargets();
                SubscribeToCurrentTargets();
            }

            RefreshVisibleValues();
            OnPropertyChanged(nameof(SelectionHeaderText));
        }
        finally
        {
            _isHandlingSelectionChange = false;

            Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() => _suspendApplyAfterSelectionChange = false));
        }
    }

    private void UnhookSelectedControlsCollectionChanged()
    {
        if (_observedSelectedControls != null)
        {
            _observedSelectedControls.CollectionChanged -= SelectedControls_CollectionChanged;
            _observedSelectedControls = null;
        }
    }

    private DesignControl? ActiveControl
    {
        get
        {
            var selected = GetSelectedControls().FirstOrDefault();
            return selected ?? Control;
        }
    }

    internal DesignControl? GetActiveControl() => ActiveControl;

    private List<DesignControl> GetSelectedControls()
    {
        if (SelectedControls == null || SelectedControls.Count == 0)
            return [];

        return SelectedControls
            .Cast<object>()
            .OfType<DesignControl>()
            .ToList();
    }

    private List<DesignControl> GetTargets()
    {
        var selected = GetSelectedControls();
        if (selected.Count > 0)
            return selected;

        if (Control != null)
            return [Control];

        return [];
    }

    #endregion

    #region ### Build / Refresh ###

    private bool RequiresPropertyGroupRebuild()
    {
        var activeControl = ActiveControl;

        if (activeControl == null)
            return PropertyGroups.Count > 0 || _currentSchemaType != null;

        var schemaType = activeControl.GetType();

        if (_currentSchemaType != schemaType)
            return true;

        return PropertyGroups.Count == 0;
    }

    private void RebuildPropertyGroups()
    {
        string selectedCategoryName = SelectedPropertyCategory?.Name ?? PropertyCategoryTemp.AllCategoryName;

        UnsubscribeFromCurrentTargets();
        PropertyGroups.Clear();
        PropertyCategories.Clear();
        VisibleProperties.Clear();

        var activeControl = ActiveControl;
        _currentSchemaType = activeControl?.GetType();

        if (activeControl == null)
        {
            SelectedPropertyCategory = null;
            OnPropertyChanged(nameof(SelectionHeaderText));
            return;
        }

        SubscribeToCurrentTargets();

        var groupedProps = activeControl
            .GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanRead && p.CanWrite)
            .Where(IsControlProp)
            .Where(p => !string.Equals(p.Name, "SelectedItem", StringComparison.Ordinal))
            .Select(p => new PropertyItemTemp(this, p))
            .Where(p => p.EditorKind != PropertyEditorKind.None)
            .GroupBy(p => p.Category)
            .OrderBy(g => GetCategorySortKey(g.Key))
            .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var group in groupedProps)
        {
            var groupVm = new PropertyGroupTemp
            {
                Name = group.Key
            };

            foreach (var item in group.OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase))
                groupVm.Items.Add(item);

            PropertyGroups.Add(groupVm);
        }

        int propertyCount = PropertyGroups.Sum(group => group.Items.Count);
        PropertyCategories.Add(new PropertyCategoryTemp(PropertyCategoryTemp.AllCategoryName, propertyCount));

        foreach (var group in PropertyGroups)
            PropertyCategories.Add(new PropertyCategoryTemp(group.Name, group.Items.Count));

        SelectedPropertyCategory = PropertyCategories.FirstOrDefault(
            category => string.Equals(category.Name, selectedCategoryName, StringComparison.OrdinalIgnoreCase))
            ?? PropertyCategories.FirstOrDefault();

        OnPropertyChanged(nameof(SelectionHeaderText));
    }

    private void RefreshVisibleValues()
    {
        var activeControl = ActiveControl;
        if (activeControl == null)
        {
            OnPropertyChanged(nameof(SelectionHeaderText));
            return;
        }

        _isRefreshingFromControl = true;
        try
        {
            foreach (var group in PropertyGroups)
            {
                foreach (var item in group.Items)
                    item.RefreshFromControl();
            }
        }
        finally
        {
            _isRefreshingFromControl = false;
            OnPropertyChanged(nameof(SelectionHeaderText));
        }
    }

    private void RefreshPropertyFilter()
    {
        VisibleProperties.Clear();

        string? selectedCategory = SelectedPropertyCategory?.Name;
        bool showAll = string.IsNullOrWhiteSpace(selectedCategory)
            || string.Equals(selectedCategory, PropertyCategoryTemp.AllCategoryName, StringComparison.OrdinalIgnoreCase);

        IEnumerable<PropertyGroupTemp> groups = showAll
            ? PropertyGroups
            : PropertyGroups.Where(
                group => string.Equals(group.Name, selectedCategory, StringComparison.OrdinalIgnoreCase));

        foreach (var group in groups)
        {
            foreach (var item in group.Items)
                VisibleProperties.Add(item);
        }
    }

    private void SubscribeToCurrentTargets()
    {
        foreach (var target in GetTargets().OfType<INotifyPropertyChanged>())
        {
            if (_observedTargets.Contains(target))
                continue;

            target.PropertyChanged += Control_PropertyChanged;
            _observedTargets.Add(target);
        }
    }

    private void UnsubscribeFromCurrentTargets()
    {
        foreach (var target in _observedTargets)
            target.PropertyChanged -= Control_PropertyChanged;

        _observedTargets.Clear();
    }

    private void Control_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isHandlingSelectionChange)
            return;

        if (e.PropertyName == nameof(DesignControl.X)
            || e.PropertyName == nameof(DesignControl.Y)
            || e.PropertyName == nameof(DesignControl.Width)
            || e.PropertyName == nameof(DesignControl.Height))
        {
            QueuePositionRefresh();
            return;
        }

        RefreshVisibleValues();
    }

    private void QueuePositionRefresh()
    {
        if (_pendingPositionRefreshOperation?.Status == DispatcherOperationStatus.Pending)
            return;

        _pendingPositionRefreshOperation = Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new Action(() =>
            {
                _pendingPositionRefreshOperation = null;

                if (_isHandlingSelectionChange)
                    return;

                RefreshVisibleValues();
            }));
    }

    #endregion

    #region ### Apply ###

    internal void ApplyPropertyValue(PropertyItemTemp item, object? value, bool force = false)
    {
        if (!force && (_isRefreshingFromControl || _isHandlingSelectionChange || _suspendApplyAfterSelectionChange))
            return;

        foreach (var target in GetTargets())
            TrySetProperty(target, item.Property, value);

        RefreshVisibleValues();
        MSG.UI.InvalidateDesigner();
    }

    private static void TrySetProperty(DesignControl target, PropertyInfo property, object? value)
    {
        if (!property.CanWrite)
            return;

        try
        {
            object? converted = ConvertValue(value, property.PropertyType);
            property.SetValue(target, converted);
        }
        catch
        {
            // bewusst ignorieren
        }
    }

    #endregion

    #region ### Reflection Helpers ###

    private static bool IsControlProp(PropertyInfo property)
    {
        return property
            .GetCustomAttributes(true)
            .Any(a => a.GetType().Name == "ControlPropAttribute");
    }

    private static int GetCategorySortKey(string category)
    {
        return category switch
        {
            "Appearance" => 0,
            "Behavior" => 1,
            "Content" => 2,
            "Font" => 3,
            "Icon" => 4,
            "Layout" => 5,
            "State" => 6,
            "Typography" => 7,
            _ => 100
        };
    }

    #endregion

    #region ### Conversion ###

    private static object? ConvertValue(object? value, Type targetType)
    {
        if (value == null)
            return null;

        var effectiveType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (effectiveType.IsInstanceOfType(value))
            return value;

        if (effectiveType.IsEnum)
        {
            if (value is string enumText)
                return Enum.Parse(effectiveType, enumText, true);

            return Enum.ToObject(effectiveType, value);
        }

        if (effectiveType == typeof(DateTime))
        {
            if (value is DateTime dt)
                return dt;

            if (value is string s)
            {
                if (DateTime.TryParse(s, CultureInfo.CurrentCulture, DateTimeStyles.None, out var parsed))
                    return parsed;

                if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                    return parsed;
            }

            return DateTime.Now;
        }

        if (effectiveType == typeof(List<string>))
        {
            if (value is string text)
                return ParseItemsText(text);

            if (value is IEnumerable<string> listItems)
                return listItems.ToList();

            return new List<string>();
        }

        if (effectiveType == typeof(ObservableCollection<string>))
        {
            if (value is string text)
                return new ObservableCollection<string>(ParseItemsText(text));

            if (value is IEnumerable<string> ocItems)
                return new ObservableCollection<string>(ocItems);

            return new ObservableCollection<string>();
        }

        if (effectiveType == typeof(string))
            return value.ToString() ?? string.Empty;

        if (effectiveType == typeof(int))
            return Convert.ToInt32(value, CultureInfo.InvariantCulture);

        if (effectiveType == typeof(float))
            return Convert.ToSingle(value, CultureInfo.InvariantCulture);

        if (effectiveType == typeof(double))
            return Convert.ToDouble(value, CultureInfo.InvariantCulture);

        if (effectiveType == typeof(decimal))
            return Convert.ToDecimal(value, CultureInfo.InvariantCulture);

        if (effectiveType == typeof(bool))
            return Convert.ToBoolean(value, CultureInfo.InvariantCulture);

        if (effectiveType == typeof(Color))
        {
            if (value is Color colorValue)
                return colorValue;

            var converted = ColorConverter.ConvertFromString(value.ToString());
            return converted is Color color ? color : Colors.Transparent;
        }

        if (effectiveType == typeof(SkiaColor))
        {
            if (value is SkiaColor skiaColor)
                return skiaColor;

            if (value is Color wpfColor)
                return new SkiaColor(wpfColor.R, wpfColor.G, wpfColor.B, wpfColor.A);

            return SkiaSharp.SKColors.Transparent;
        }

        if (effectiveType == typeof(Thickness))
        {
            if (value is Thickness thicknessValue)
                return thicknessValue;

            string text = (value?.ToString() ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(text))
                return new Thickness(0);

            text = text.Replace(";", ",");

            var thicknessConverter = new ThicknessConverter();
            var converted = thicknessConverter.ConvertFromInvariantString(text);

            return converted is Thickness thickness ? thickness : new Thickness(0);
        }

        if (effectiveType == typeof(FontWeight))
        {
            if (value is FontWeight fontWeightValue)
                return fontWeightValue;

            var text = value?.ToString();
            if (string.IsNullOrWhiteSpace(text))
                return FontWeights.Normal;

            var fontWeightConverter = new FontWeightConverter();
            var converted = fontWeightConverter.ConvertFromString(text);
            return converted is FontWeight fontWeight ? fontWeight : FontWeights.Normal;
        }

        return Convert.ChangeType(value, effectiveType, CultureInfo.InvariantCulture);
    }

    private static List<string> ParseItemsText(string? text)
    {
        return (text ?? string.Empty)
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
    }

    #endregion

    #region ### Editor Event Handlers ###

    private bool IsEditorWriteBlocked()
    {
        return _isRefreshingFromControl
            || _isHandlingSelectionChange
            || _suspendApplyAfterSelectionChange;
    }

    private void TextEditor_GotKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
    {
        if (sender is not TextBox tb || tb.Tag is not PropertyItemTemp item)
            return;

        _activeTextEditSnapshotItems.Remove(item);
        EnsureLiveTextTimer(tb);
    }

    private void TextEditor_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox tb || tb.Tag is not PropertyItemTemp item)
            return;

        if (IsEditorWriteBlocked())
            return;

        var timer = EnsureLiveTextTimer(tb);
        timer.Stop();
        timer.Start();
    }

    private void TextEditor_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox tb || tb.Tag is not PropertyItemTemp item)
            return;

        FlushLiveTextEditor(tb, item);
        _activeTextEditSnapshotItems.Remove(item);
    }

    private DispatcherTimer EnsureLiveTextTimer(TextBox editor)
    {
        if (_liveTextTimers.TryGetValue(editor, out var existing))
            return existing;

        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(220)
        };

        timer.Tick += (_, _) =>
        {
            timer.Stop();

            if (editor.Tag is PropertyItemTemp item)
                ApplyLiveTextEditorValue(editor, item);
        };

        _liveTextTimers[editor] = timer;
        editor.Unloaded += TextEditor_Unloaded;

        return timer;
    }

    private void TextEditor_Unloaded(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox tb)
            return;

        if (_liveTextTimers.TryGetValue(tb, out var timer))
        {
            timer.Stop();
            _liveTextTimers.Remove(tb);
        }

        tb.Unloaded -= TextEditor_Unloaded;
    }

    private void FlushLiveTextEditor(TextBox editor, PropertyItemTemp item)
    {
        if (_liveTextTimers.TryGetValue(editor, out var timer))
            timer.Stop();

        ApplyLiveTextEditorValue(editor, item);
    }

    private void ApplyLiveTextEditorValue(TextBox editor, PropertyItemTemp item)
    {
        if (IsEditorWriteBlocked())
            return;

        string newText = editor.Text ?? string.Empty;
        if (string.Equals(item.CurrentTextValue, newText, StringComparison.Ordinal))
            return;

        PushSnapshotForPropertyChange(item, isTextEditSession: true);
        item.SetCurrentValueFromEditor(newText);
    }

    private void ItemsEditor_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox tb || tb.Tag is not PropertyItemTemp item)
            return;

        if (IsEditorWriteBlocked())
            return;

        string newText = tb.Text ?? string.Empty;
        if (string.Equals(item.CurrentItemsText, newText, StringComparison.Ordinal))
            return;

        PushSnapshotForPropertyChange(item);
        item.SetCurrentValueFromEditor(newText);
    }

    private void NumericEditor_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not XNumberBox editor)
            return;

        var descriptor = DependencyPropertyDescriptor.FromProperty(XNumberBox.ValueProperty, typeof(XNumberBox));
        descriptor.RemoveValueChanged(editor, NumericEditor_ValueChanged);
        descriptor.AddValueChanged(editor, NumericEditor_ValueChanged);
    }

    private void NumericEditor_Unloaded(object sender, RoutedEventArgs e)
    {
        if (sender is not XNumberBox editor)
            return;

        var descriptor = DependencyPropertyDescriptor.FromProperty(XNumberBox.ValueProperty, typeof(XNumberBox));
        descriptor.RemoveValueChanged(editor, NumericEditor_ValueChanged);
    }

    private void NumericEditor_ValueChanged(object? sender, EventArgs e)
    {
        if (sender is not XNumberBox editor || editor.Tag is not PropertyItemTemp item || editor.Value is not double value)
            return;

        if (ShouldDeferSizeTextEntry(editor, item))
            return;

        ApplyNumericEditorValue(item, value);
    }

    private void NumericEditor_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not XNumberBox editor || editor.Tag is not PropertyItemTemp item)
            return;

        CommitNumericEditorValue(editor, item);
        EndNumericEditSession(item);
    }

    private void NumericEditor_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (sender is not XNumberBox editor || editor.Tag is not PropertyItemTemp item)
            return;

        if (e.Key == System.Windows.Input.Key.Enter)
        {
            CommitNumericEditorValue(editor, item);
            EndNumericEditSession(item);
            return;
        }

        if (e.Key == System.Windows.Input.Key.Escape)
            EndNumericEditSession(item);
    }

    private void CommitNumericEditorValue(XNumberBox editor, PropertyItemTemp item)
    {
        if (editor.Value is double value)
            ApplyNumericEditorValue(item, value);
    }

    private void ApplyNumericEditorValue(PropertyItemTemp item, double value)
    {
        if (IsEditorWriteBlocked())
            return;

        if (Math.Abs(item.CurrentNumericValue - value) < 0.0001d)
            return;

        PushSnapshotForPropertyChange(item, isNumericEditSession: true);
        item.SetCurrentValueFromEditor(value);
        ArmNumericEditSessionTimeout(item);
    }

    private static bool ShouldDeferSizeTextEntry(XNumberBox editor, PropertyItemTemp item)
    {
        if (item.Property.Name != nameof(DesignControl.Width)
            && item.Property.Name != nameof(DesignControl.Height))
        {
            return false;
        }

        return System.Windows.Input.Keyboard.FocusedElement is TextBox focusedTextBox
            && IsVisualDescendantOf(focusedTextBox, editor);
    }

    private static bool IsVisualDescendantOf(
        DependencyObject descendant,
        DependencyObject ancestor)
    {
        DependencyObject? current = descendant;

        while (current != null)
        {
            if (ReferenceEquals(current, ancestor))
                return true;

            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }

    private void ArmNumericEditSessionTimeout(PropertyItemTemp item)
    {
        if (!_numericEditTimers.TryGetValue(item, out var timer))
        {
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(850)
            };

            timer.Tick += (_, _) =>
            {
                timer.Stop();
                _numericEditTimers.Remove(item);
                _activeNumericEditSnapshotItems.Remove(item);
            };

            _numericEditTimers[item] = timer;
        }

        timer.Stop();
        timer.Start();
    }

    private void EndNumericEditSession(PropertyItemTemp item)
    {
        if (_numericEditTimers.TryGetValue(item, out var timer))
        {
            timer.Stop();
            _numericEditTimers.Remove(item);
        }

        _activeNumericEditSnapshotItems.Remove(item);
    }

    private void DateTimeEditor_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not XDatePicker datePicker || _datePickerValueHandlers.ContainsKey(datePicker))
            return;

        EventHandler handler = (_, _) => DateTimeEditor_SelectedDateChanged(datePicker);
        _datePickerValueHandlers.Add(datePicker, handler);
        DependencyPropertyDescriptor.FromProperty(XDatePicker.SelectedDateProperty, typeof(XDatePicker))?.AddValueChanged(datePicker, handler);
    }

    private void DateTimeEditor_Unloaded(object sender, RoutedEventArgs e)
    {
        if (sender is not XDatePicker datePicker || !_datePickerValueHandlers.Remove(datePicker, out var handler))
            return;

        DependencyPropertyDescriptor.FromProperty(XDatePicker.SelectedDateProperty, typeof(XDatePicker))?.RemoveValueChanged(datePicker, handler);
    }

    private void DateTimeEditor_SelectedDateChanged(XDatePicker datePicker)
    {
        if (datePicker.Tag is not PropertyItemTemp item || IsEditorWriteBlocked())
            return;

        if (Equals(item.CurrentDateTimeValue, datePicker.SelectedDate))
            return;

        PushSnapshotForPropertyChange(item);
        item.SetCurrentValueFromEditor(datePicker.SelectedDate);
    }

    private void BoolEditor_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox cb || cb.Tag is not PropertyItemTemp item)
            return;

        if (IsEditorWriteBlocked())
            return;

        bool newValue = cb.IsChecked == true;
        if (item.CurrentBoolValue == newValue)
            return;

        PushSnapshotForPropertyChange(item);
        item.SetCurrentValueFromEditor(newValue);
    }

    private void EnumEditor_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox cb || cb.Tag is not PropertyItemTemp item)
            return;

        if (IsEditorWriteBlocked())
            return;

        if (Equals(item.CurrentValue, cb.SelectedItem))
            return;

        PushSnapshotForPropertyChange(item);
        item.SetCurrentValueFromEditor(cb.SelectedItem);
    }

    private void FontWeightEditor_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox cb || cb.Tag is not PropertyItemTemp item)
            return;

        if (IsEditorWriteBlocked())
            return;

        if (Equals(item.CurrentValue, cb.SelectedItem))
            return;

        PushSnapshotForPropertyChange(item);
        item.SetCurrentValueFromEditor(cb.SelectedItem);
    }

    private void PushSnapshotForPropertyChange(
        PropertyItemTemp item,
        bool isTextEditSession = false,
        bool isNumericEditSession = false)
    {
        if (GetTargets().Count == 0)
            return;

        if (isTextEditSession)
        {
            if (!_activeTextEditSnapshotItems.Add(item))
                return;
        }

        if (isNumericEditSession)
        {
            if (!_activeNumericEditSnapshotItems.Add(item))
                return;
        }

        var vm = global::Mockup.MockupService.Mockup;
        if (vm == null)
            return;

        SnapshotContext? context = vm.MainTabSelectedIndex switch
        {
            1 when vm.CurrentScreen != null => SnapshotContext.Screen,
            2 when vm.CurrentTemplate != null => SnapshotContext.Template,
            3 when vm.CurrentPopup != null => SnapshotContext.Popup,
            _ => null,
        };

        if (context != null)
            vm.PushSnapshot(context.Value, SnapshotLabels.ControlPropChanged);
    }

    #endregion

    #region ### Dialog Actions ###
    private void SelectImageRef_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is not PropertyItemTemp item)
            return;

        ImageRef? current = item.CurrentValue as ImageRef;

        var dialog = new ImageRefDialog(current);

        Window? owner = Window.GetWindow(fe);

        if (owner == null || ReferenceEquals(owner, dialog))
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (ReferenceEquals(window, dialog))
                    continue;

                if (!window.IsVisible || window.WindowState == WindowState.Minimized)
                    continue;

                if (window.IsActive)
                {
                    owner = window;
                    break;
                }
            }
        }

        if ((owner == null || ReferenceEquals(owner, dialog))
            && Application.Current.MainWindow != null
            && !ReferenceEquals(Application.Current.MainWindow, dialog)
            && Application.Current.MainWindow.IsVisible
            && Application.Current.MainWindow.WindowState != WindowState.Minimized)
        {
            owner = Application.Current.MainWindow;
        }

        if (owner != null && !ReferenceEquals(owner, dialog))
        {
            dialog.Owner = owner;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
        else
        {
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        bool? accepted = dialog.ShowDialog();
        if (accepted != true)
            return;

        PushSnapshotForPropertyChange(item);
        item.SetCurrentValueFromEditor(dialog.SelectedImageRef);
    }

    private void PickColor_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is not PropertyItemTemp item)
            return;

        Color currentColor = item.CurrentValue switch
        {
            Color color => color,
            SkiaColor color => Color.FromArgb(color.Alpha, color.Red, color.Green, color.Blue),
            _ => Colors.Transparent
        };

        var dialog = new XColorPickerDialog { SelectedColor = currentColor };

        // XColorPickerDialog is a real modal XWindow. Give it the active host just
        // like ImageRefDialog, otherwise it can open behind the main window.
        Window? owner = Window.GetWindow(fe) ?? Window.GetWindow(this);

        if ((owner == null || ReferenceEquals(owner, dialog))
            && Application.Current.MainWindow != null
            && !ReferenceEquals(Application.Current.MainWindow, dialog)
            && Application.Current.MainWindow.IsVisible
            && Application.Current.MainWindow.WindowState != WindowState.Minimized)
        {
            owner = Application.Current.MainWindow;
        }

        if (owner != null && !ReferenceEquals(owner, dialog))
        {
            dialog.Owner = owner;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
        else
        {
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        bool? accepted = dialog.ShowDialog();
        if (accepted != true)
            return;

        PushSnapshotForPropertyChange(item);
        object selectedColor = item.Property.PropertyType == typeof(SkiaColor)
            ? new SkiaColor(
                dialog.SelectedColor.R,
                dialog.SelectedColor.G,
                dialog.SelectedColor.B,
                dialog.SelectedColor.A)
            : dialog.SelectedColor;

        item.SetCurrentValueFromEditor(selectedColor);
        // A modal picker is an explicit user commit. It must not be discarded by
        // the short selection-refresh guard that protects editor controls.
        ApplyPropertyValue(item, selectedColor, force: true);

        foreach (var target in GetTargets())
        {
            var variantProp = target.GetType().GetProperty(
                "Variant",
                BindingFlags.Instance | BindingFlags.Public
            );

            if (variantProp == null || !variantProp.CanWrite)
                continue;

            var variantType =
                Nullable.GetUnderlyingType(variantProp.PropertyType) ?? variantProp.PropertyType;

            if (!variantType.IsEnum)
                continue;

            try
            {
                var customValue = Enum.Parse(variantType, "CUSTOM", true);
                variantProp.SetValue(target, customValue);
            }
            catch
            {
                try
                {
                    var customValue = Enum.Parse(variantType, "Custom", true);
                    variantProp.SetValue(target, customValue);
                }
                catch
                {
                    // bewusst ignorieren
                }
            }
        }

        RefreshVisibleValues();
        MSG.UI.InvalidateDesigner();
    }

    private void ClearValue_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is not PropertyItemTemp item)
            return;

        if (item.CurrentValue == null)
            return;

        PushSnapshotForPropertyChange(item);
        item.SetCurrentValueFromEditor(null);
    }

    #endregion
}

#region ### PropertyGroupTemp ###

public sealed class PropertyGroupTemp : ObservableObject
{
    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public ObservableCollection<PropertyItemTemp> Items { get; } = [];
}

#endregion

#region ### PropertyCategoryTemp ###

public sealed class PropertyCategoryTemp
{
    public const string AllCategoryName = "All";

    public PropertyCategoryTemp(string name, int count)
    {
        Name = name;
        Count = count;
    }

    public string Name { get; }
    public int Count { get; }
}

#endregion

#region ### PropertyItemTemp ###

public sealed class PropertyItemTemp : ObservableObject
{
    #region ### Fields ###

    private readonly PropertyEditor _owner;
    private object? _currentValue;

    #endregion

    #region ### Ctor ###

    public PropertyItemTemp(PropertyEditor owner, PropertyInfo property)
    {
        _owner = owner;
        Property = property;

        Category = property.GetCustomAttribute<CategoryAttribute>()?.Category ?? "Misc";

        DisplayName =
            property.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? property.Name;

        EditorKind = DetermineEditorKind(property.PropertyType);

        var effectiveType =
            Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        if (effectiveType.IsEnum)
            EnumValues = Enum.GetValues(effectiveType).Cast<object>().ToList();

        if (effectiveType == typeof(FontWeight))
        {
            FontWeightValues =
            [
                FontWeights.Thin,
                FontWeights.ExtraLight,
                FontWeights.Light,
                FontWeights.Normal,
                FontWeights.Medium,
                FontWeights.SemiBold,
                FontWeights.Bold,
                FontWeights.ExtraBold,
                FontWeights.Black
            ];
        }

        RefreshFromControl();
    }

    #endregion

    #region ### Metadata ###

    public PropertyInfo Property { get; }
    public string Category { get; }
    public string DisplayName { get; }
    public PropertyEditorKind EditorKind { get; }
    public IReadOnlyList<object>? EnumValues { get; }
    public IReadOnlyList<object>? FontWeightValues { get; private set; }

    #endregion

    #region ### Editor Flags ###

    public bool IsTextEditor => EditorKind == PropertyEditorKind.Text;
    public bool IsItemsEditor => EditorKind == PropertyEditorKind.Items;
    public bool IsNumericEditor => EditorKind == PropertyEditorKind.Numeric;
    public bool IsDateTimeEditor => EditorKind == PropertyEditorKind.DateTime;
    public bool IsBoolEditor => EditorKind == PropertyEditorKind.Bool;
    public bool IsEnumEditor => EditorKind == PropertyEditorKind.Enum;
    public bool IsImageRefEditor => EditorKind == PropertyEditorKind.ImageRef;
    public bool IsColorEditor => EditorKind == PropertyEditorKind.Color;
    public bool IsFontWeightEditor => EditorKind == PropertyEditorKind.FontWeight;
    public bool IsReadOnlyEditor => EditorKind == PropertyEditorKind.ReadOnly;

    #endregion

    #region ### Values ###

    public Brush CurrentBrush => CurrentValue switch
    {
        Color color => new SolidColorBrush(color),
        SkiaColor color => new SolidColorBrush(Color.FromArgb(color.Alpha, color.Red, color.Green, color.Blue)),
        _ => Brushes.Transparent
    };

    public object? CurrentValue
    {
        get => _currentValue;
        set
        {
            if (Equals(_currentValue, value))
                return;

            _currentValue = value;
            RaiseAllValuePropertiesChanged();
        }
    }

    public string CurrentTextValue
    {
        get => ConvertValueToText(CurrentValue);
        set => SetCurrentValueFromEditor(value);
    }

    public string CurrentItemsText
    {
        get => ConvertItemsToText(CurrentValue);
        set => SetCurrentValueFromEditor(value);
    }

    public double CurrentNumericValue
    {
        get => ConvertValueToDouble(CurrentValue);
        set => SetCurrentValueFromEditor(value);
    }

    public DateTime? CurrentDateTimeValue
    {
        get => ConvertValueToDateTime(CurrentValue);
        set => SetCurrentValueFromEditor(value);
    }

    public bool CurrentBoolValue
    {
        get => CurrentValue is bool b && b;
        set => SetCurrentValueFromEditor(value);
    }

    public void SetCurrentValueFromEditor(object? value)
    {
        if (Equals(_currentValue, value))
            return;

        _currentValue = value;
        RaiseAllValuePropertiesChanged();
        _owner.ApplyPropertyValue(this, value);
    }

    private void RaiseAllValuePropertiesChanged()
    {
        OnPropertyChanged(nameof(CurrentValue));
        OnPropertyChanged(nameof(CurrentTextValue));
        OnPropertyChanged(nameof(CurrentItemsText));
        OnPropertyChanged(nameof(CurrentNumericValue));
        OnPropertyChanged(nameof(CurrentDateTimeValue));
        OnPropertyChanged(nameof(CurrentBoolValue));
        OnPropertyChanged(nameof(CurrentBrush));
    }

    #endregion

    #region ### Refresh ###

    public void RefreshFromControl()
    {
        var control = _owner.GetActiveControl();
        if (control == null)
            return;

        _currentValue = Property.GetValue(control);
        RaiseAllValuePropertiesChanged();
    }

    #endregion

    #region ### Editor Detection ###

    private static PropertyEditorKind DetermineEditorKind(Type type)
    {
        var effectiveType = Nullable.GetUnderlyingType(type) ?? type;

        if (effectiveType == typeof(bool))
            return PropertyEditorKind.Bool;

        if (effectiveType.IsEnum)
            return PropertyEditorKind.Enum;

        if (effectiveType == typeof(DateTime))
            return PropertyEditorKind.DateTime;

        if (effectiveType == typeof(FontWeight))
            return PropertyEditorKind.FontWeight;

        if (effectiveType == typeof(Color) || effectiveType == typeof(SkiaColor))
            return PropertyEditorKind.Color;

        if (effectiveType == typeof(List<string>))
            return PropertyEditorKind.Items;

        if (effectiveType == typeof(ObservableCollection<string>))
            return PropertyEditorKind.Items;

        if (
            effectiveType == typeof(int)
            || effectiveType == typeof(float)
            || effectiveType == typeof(double)
            || effectiveType == typeof(decimal)
        )
            return PropertyEditorKind.Numeric;

        if (effectiveType.Name == "ImageRef")
            return PropertyEditorKind.ImageRef;

        if (effectiveType == typeof(string) || effectiveType == typeof(Thickness))
            return PropertyEditorKind.Text;

        return PropertyEditorKind.ReadOnly;
    }

    #endregion

    #region ### Value Formatting ###

    private static string ConvertValueToText(object? value)
    {
        return value switch
        {
            null => string.Empty,
            Thickness t => $"{t.Left},{t.Top},{t.Right},{t.Bottom}",
            _ => value.ToString() ?? string.Empty
        };
    }

    private static string ConvertItemsToText(object? value)
    {
        if (value is IEnumerable<string> items)
            return string.Join(Environment.NewLine, items);

        return string.Empty;
    }

    private static double ConvertValueToDouble(object? value)
    {
        return value switch
        {
            null => 0d,
            int i => i,
            float f => f,
            double d => d,
            decimal m => (double)m,
            _ => 0d
        };
    }

    private static DateTime? ConvertValueToDateTime(object? value)
    {
        if (value == null)
            return null;

        if (value is DateTime dt)
            return dt;

        string text = value.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
            return null;

        if (DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out var parsed))
            return parsed;

        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
            return parsed;

        return null;
    }

    #endregion
}

#endregion

#region ### PropertyEditorKind ###

public enum PropertyEditorKind
{
    None,
    Text,
    Items,
    Numeric,
    DateTime,
    Bool,
    Enum,
    ImageRef,
    Color,
    FontWeight,
    ReadOnly
}

#endregion
