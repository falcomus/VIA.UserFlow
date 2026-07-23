// ==========================================================================================
// DATEI: Mockup.ViewModel/MockupViewModel.cs
// ==========================================================================================
// Diese Datei ist das Main Partial aller MockupViewModel.
// Im Constructor werden alle anderen Partials per "Init" Aufruf initialisiert. (falls nötig)
// ==========================================================================================

using CommunityToolkit.Mvvm.ComponentModel;

namespace Mockup.ViewModel;

public partial class MockupViewModel : ObservableObject
{
    #region === KONSTRUKTOR & INITIALISIERUNG ===

    // Da "Partials" nur einen parameterlosen Konstruktor haben können, werden hier im (einzigen) Konstruktor 
    // für jedes Partiel Init Methoden aufgerufen. So kann die Initialisierung sinngemäß an Ort und Stelle vorgenommen werden.
    // Wichtig: Die Reihenfolge der Init Aufrufe beachten!

    partial void InitGrouping();
    partial void InitSettings();
    partial void InitContextMenu();


    public MockupViewModel()
    {
        InitGrouping();
        InitSettings();
        InitContextMenu();
    }

    #endregion

}

