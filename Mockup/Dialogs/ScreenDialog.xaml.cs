using CommunityToolkit.Mvvm.ComponentModel;
using Mockup.Resources;
using Mockup.Services;
using Mockup.Snapshots;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using VIA.WPF.Localization;

namespace Mockup.Dialogs;

[ObservableObject]
public partial class ScreenDialog : ModalDialogWindow
{
    [ObservableProperty]
    private Band? selectedBand;

    [ObservableProperty]
    private BandPage? selectedPage;

    [ObservableProperty]
    private ICollectionView? customBandsView;

    public ScreenDialog()
    {
        InitializeComponent();
        Loaded += ScreenEditor_Loaded;
    }

    private static string DialogText(string key, string fallbackText)
    {
        return XLocalizationService.Current.GetString(
            UserFlowResources.ResourceManager,
            key,
            fallbackText);
    }

    private void ScreenEditor_Loaded(object sender, RoutedEventArgs e)
    {
        InputName.Focus();

        if (DataContext is not Screen screen)
            return;

        var view = CollectionViewSource.GetDefaultView(screen.Bands);
        view.Filter = o => o is Band b && b.BandType == BandType.Custom;
        CustomBandsView = view;

        SelectedBand ??= screen.Bands.FirstOrDefault(b => b.BandType == BandType.Custom);
        SelectedPage = SelectedBand?.ActivePage;

        screen.RecalculateBandLayout();
    }

    partial void OnSelectedBandChanged(Band? value)
    {
        if (DataContext is not Screen screen)
            return;

        if (value?.Pages != null && value.Pages.Count > 0)
        {
            value.EnsureInitialPage();
            SelectedPage = value.ActivePage;
        }
        else
        {
            SelectedPage = null;
        }

        screen.RecalculateBandLayout();
    }

    partial void OnSelectedPageChanged(BandPage? value)
    {
        if (DataContext is not Screen screen)
            return;

        var band = SelectedBand;
        if (band == null || value == null)
            return;

        int idx = band.Pages.IndexOf(value);
        if (idx >= 0 && idx != band.ActivePageIndex)
            band.ActivePageIndex = idx;

        if (band.UniformPageHeight)
            band.SyncPageHeights();

        screen.RecalculateBandLayout();
    }

    private static void PushScreenSnapshot(string label)
    {
        global::Mockup.MockupService.Mockup.PushSnapshot(SnapshotContext.Screen, label);
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is Screen screen)
            screen.RecalculateBandLayout();

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void PickBackground_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not Screen screen)
            return;

        var dialog = new XColorPickerDialog { Owner = this, SelectedColor = screen.Background };

        bool? accepted = dialog.ShowDialog();
        if (accepted != true)
            return;

        screen.Background = dialog.SelectedColor;
    }

    private void PickHeaderColor_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not Screen screen)
            return;

        var band = screen.HeaderBand;
        if (band == null)
            return;

        var dlg = new XColorPickerDialog { Owner = this, SelectedColor = band.HeaderBackground };

        if (dlg.ShowDialog() != true)
            return;

        PushScreenSnapshot(SnapshotLabels.BandPropChanged);
        band.HeaderBackground = dlg.SelectedColor;
        screen.RecalculateBandLayout();

        BindingOperations
            .GetBindingExpression(HeaderColorPreview, Border.BackgroundProperty)
            ?.UpdateTarget();
    }

    private void PickFooterColor_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not Screen screen)
            return;

        var band = screen.FooterBand;
        if (band == null)
            return;

        var dlg = new XColorPickerDialog { Owner = this, SelectedColor = band.FooterBackground };

        if (dlg.ShowDialog() != true)
            return;

        PushScreenSnapshot(SnapshotLabels.BandPropChanged);
        band.FooterBackground = dlg.SelectedColor;
        screen.RecalculateBandLayout();

        BindingOperations
            .GetBindingExpression(FooterColorPreview, Border.BackgroundProperty)
            ?.UpdateTarget();
    }

    private void PickBandColor_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not Screen screen)
            return;

        var band = SelectedBand;
        if (band == null)
            return;

        var dlg = new XColorPickerDialog { Owner = this, SelectedColor = band.BandBackground };

        if (dlg.ShowDialog() != true)
            return;

        PushScreenSnapshot(SnapshotLabels.BandPropChanged);
        band.BandBackground = dlg.SelectedColor;
        screen.RecalculateBandLayout();
    }

    private void AddBand_Click(object sender, RoutedEventArgs e) =>
        AddBandCore(isExpandable: false);

    private void AddBandCore(bool isExpandable)
    {
        if (DataContext is not Screen screen)
            return;

        int insertIndex = screen.Bands.Count;
        var footer = screen.Bands.FirstOrDefault(b => b.BandType == BandType.Footer);
        if (footer != null)
            insertIndex = screen.Bands.IndexOf(footer);

        if (SelectedBand != null && SelectedBand.BandType == BandType.Custom)
        {
            int ctxIdx = screen.Bands.IndexOf(SelectedBand);
            if (ctxIdx >= 0)
                insertIndex = Math.Min(ctxIdx + 1, insertIndex);
        }

        var band = new Band
        {
            BandType = BandType.Custom,
            Title = Band.DEFAULT_TITLE,
            HeaderBackground = Colors.LightGray,
            IsExpandable = isExpandable,
            IsExpanded = !isExpandable,
            UniformPageHeight = true,
            Height = Screen.DefaultBandHeight,
            SavedExpandedHeight = Screen.DefaultBandHeight,
            Width = screen.Width,
            X = 0,
            ParentScreen = screen,
        };

        if (isExpandable)
            band.SavedExpandedHeight = Math.Max(band.SavedExpandedHeight, 90);

        band.AddNewPage();

        if (band.ActivePage != null)
            band.ActivePage.Height = band.Height;

        PushScreenSnapshot(SnapshotLabels.BandAdded);

        screen.Bands.Insert(insertIndex, band);

        screen.RecalculateBandLayout();
        //XXX CustomBandsView?.Refresh();

        SelectedBand = band;
        SelectedPage = band.ActivePage;
    }

    private void DeleteBand_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not Screen screen)
            return;

        var band = SelectedBand;
        if (band == null || band.BandType != BandType.Custom)
            return;

        int customCount = screen.Bands.Count(b => b.BandType == BandType.Custom);
        if (customCount <= 1)
        {
            XNotifications.Info(DialogText("Dialog.Screen.AtLeastOneBand", "At least one band is required!"));
            return;
        }

        int idx = screen.Bands.IndexOf(band);
        if (idx < 0)
            return;

        PushScreenSnapshot(SnapshotLabels.BandDeleted);

        screen.Bands.RemoveAt(idx);

        screen.RecalculateBandLayout();
        CustomBandsView?.Refresh();

        SelectedBand = screen
            .Bands.Where(b => b.BandType == BandType.Custom)
            .ElementAtOrDefault(Math.Max(0, Math.Min(customCount - 2, idx - 1)));

        SelectedPage = SelectedBand?.ActivePage;
    }

    private void MoveBandUp_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not Screen screen)
            return;

        var band = SelectedBand;
        if (band == null || band.BandType != BandType.Custom)
            return;

        int idx = screen.Bands.IndexOf(band);
        if (idx <= 0)
            return;

        var header = screen.Bands.FirstOrDefault(b => b.BandType == BandType.Header);
        int minIdx = header != null ? screen.Bands.IndexOf(header) + 1 : 0;
        if (idx <= minIdx)
            return;

        PushScreenSnapshot(SnapshotLabels.BandMoved);

        screen.Bands.Move(idx, idx - 1);

        screen.RecalculateBandLayout();
        //XXX CustomBandsView?.Refresh();
    }

    private void MoveBandDown_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not Screen screen)
            return;

        var band = SelectedBand;
        if (band == null || band.BandType != BandType.Custom)
            return;

        int idx = screen.Bands.IndexOf(band);
        if (idx < 0 || idx >= screen.Bands.Count - 1)
            return;

        var footer = screen.Bands.FirstOrDefault(b => b.BandType == BandType.Footer);
        int maxIdx = footer != null ? screen.Bands.IndexOf(footer) - 1 : screen.Bands.Count - 1;
        if (idx >= maxIdx)
            return;

        PushScreenSnapshot(SnapshotLabels.BandMoved);

        screen.Bands.Move(idx, idx + 1);

        screen.RecalculateBandLayout();
        //XXX CustomBandsView?.Refresh();
    }

    private void AddPage_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not Screen screen)
            return;

        var band = SelectedBand;
        if (band == null)
            return;

        PushScreenSnapshot(SnapshotLabels.PageAdded);

        var page = band.AddNewPage();

        band.ActivePageIndex = band.Pages.Count - 1;
        SelectedPage = page;

        if (band.UniformPageHeight)
            band.SyncPageHeights();

        screen.RecalculateBandLayout();
    }

    private void DeletePage_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not Screen screen)
            return;

        var band = SelectedBand;
        if (band == null)
            return;

        if (band.Pages.Count <= 1)
        {
            XNotifications.Info(DialogText("Dialog.Screen.AtLeastOnePage", "At least one page is required!"));
            return;
        }

        int idx = band.ActivePageIndex;
        PushScreenSnapshot(SnapshotLabels.PageDeleted);
        band.RemovePageAt(idx);

        SelectedPage = band.ActivePage;

        if (band.UniformPageHeight)
            band.SyncPageHeights();

        screen.RecalculateBandLayout();
    }

}
