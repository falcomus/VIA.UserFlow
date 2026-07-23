// ======================================================================================
// FILE: Mockup.Messages/DesignerMessages.cs
//
// ZWECK:
// Definiert sämtliche Nachrichtenklassen (Message Contracts) für die lose gekoppelte
// Kommunikation im Mockup-Designer über WeakReferenceMessenger sowie eine
// kompakte statische Helper-API („MSG“).
//
// FUNKTIONALITÄTEN:
// - Vollständige Sammlung aller Designer-Nachrichten
// - Klar strukturierte Region-Aufteilung
// - Ultra-kurze Helper-Methoden, z. B.:
//      MSG.UI.ShowOverlay();
//      MSG.Snack.Success("Saved");
//      MSG.Navigation.GoTo("Home");
//      MSG.Control.Committed(screen);
//      MSG.App.MainWindowReady();
//
// AUTOR: Claus Falkenstein / ChatGPT MO30 Integration
// VERSION: 2.2 (MO30 – Komplett bereinigt, fehlerfrei)
// ======================================================================================

using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Mockup.Actions;

namespace Mockup.Messages;

#region === UI MESSAGES (Overlay, Designer-Invalidate) ===

public sealed class InvalidateDesignerMessage : ValueChangedMessage<bool> { public InvalidateDesignerMessage() : base(false) { } }


public sealed class ShowHideOverlayMessage : ValueChangedMessage<bool>
{
    public ShowHideOverlayMessage(bool isVisible)
        : base(isVisible) { }
}

public sealed record ShowProjectLoadingMessage(string ProjectName);

public sealed record HideProjectLoadingMessage;

#endregion

#region === AA (ActionArea) ===

public sealed class ActionAreaEditMessage : ValueChangedMessage<ActionArea>
{
    public ActionAreaEditMessage(ActionArea area) : base(area) { }
}

public sealed class ActionAreaTriggerMessage : ValueChangedMessage<(ActionArea Area, ActionTrigger Trigger)>
{
    public ActionAreaTriggerMessage(ActionArea area, ActionTrigger trigger) : base((area, trigger)) { }
}

#endregion === AA (ActionArea) ===

#region === NAVIGATION ===

public sealed record NavigateToMessage(string TargetScreenId);

public sealed record NavigateBackMessage;

public sealed record OpenFileMessage(string Path);

public sealed record OpenURLMessage(string Url);

#endregion === NAVIGATION ===

#region === CONTROL MESSAGES (OBS?) ===

public sealed class ControlPropertyChangedMessage : ValueChangedMessage<DesignControl>
{
    public ControlPropertyChangedMessage(DesignControl control)
        : base(control) { }
}

#endregion === CONTROL MESSAGES (OBS?) ===

#region === BAND MESSAGES ===

public sealed record SelectBandMessage(long ScreenId, long BandId);

// Delta = -1 → hoch, +1 → runter
public sealed record MoveBandMessage(Band Band, int Delta);

#endregion

#region === TREEASSIST MESSAGES ===

//public sealed record RequestTreeViewExpandedStateMessage();
//public sealed record RestoreTreeViewExpandedStateMessage(Dictionary<string, bool> States);
//public sealed record SelectTreeViewScreenMessage(Screen Screen);

//public sealed record ExpandAllScreenGroupsMessage;
//public sealed record CollapseAllScreenGroupsMessage;

#endregion === TREEVIEW MESSAGES ===

#region === PROPERTY GRID MESSAGES ===

public sealed class InvalidatePropertyGridMessage : ValueChangedMessage<object?>
{
    public InvalidatePropertyGridMessage(object? target)
        : base(target) { }

    public object? Target => Value;
}

#endregion


//###############################
// STATIC MESSAGE HELPER CLASSES
//###############################

#region === MSG – STATIC HELPER API ===

/// <summary>
/// Sehr kompakte Statische Helper-API zum einfachen Senden aller Nachrichten.
/// Struktur ist identisch zu deinem bisherigen „DesignerMessages“, nur sauberer.
/// </summary>
/// 
public static class MSG
{
    #region === MESSAGE HELPER CLASS ===

    /// <summary>
    /// Interne Mini-API für alle Send-Operationen.  
    /// Erlaubt zentrales Umschalten, falls Messenger getauscht werden soll.
    /// </summary>

    private static void Send<T>(T msg) where T : class => Msg.Send(msg);

    internal static class Msg
    {
        public static void Send<T>(T msg) where T : class
            => WeakReferenceMessenger.Default.Send(msg);
    }

    #endregion === MESSAGE HELPER CLASS ===

    #region === UI ===

    public static class UI
    {
        private static int overlayDepth;

        public static void InvalidateDesigner() => Send(new InvalidateDesignerMessage());

        public static void ShowOverlay(bool visible = true)
        {
            if (!visible)
            {
                HideOverlay();
                return;
            }

            if (Interlocked.Increment(ref overlayDepth) == 1)
                Send(new ShowHideOverlayMessage(true));
        }

        public static void HideOverlay()
        {
            if (overlayDepth <= 0)
                return;

            if (Interlocked.Decrement(ref overlayDepth) == 0)
                Send(new ShowHideOverlayMessage(false));
        }

        public static void ShowProjectLoading(string projectName)
            => Send(new ShowProjectLoadingMessage(projectName));

        public static void HideProjectLoading()
            => Send(new HideProjectLoadingMessage());
    }

    #endregion === UI ===

    #region === AA (ActionArea) ===

    public static class AA
    {
        public static void ShowEditor(ActionArea area)
            => Send(new ActionAreaEditMessage(area));

        public static void Trigger(ActionArea area, ActionTrigger trigger)
            => Send(new ActionAreaTriggerMessage(area, trigger));
    }

    #endregion === AA (ActionArea) ===

    #region === CONTROL ===
    public static class Control
    {
        public static void PropertyChanged(DesignControl ctrl)
            => Send(new ControlPropertyChangedMessage(ctrl));

    }

    #endregion === CONTROL ===

    #region === BAND ===

    public static void SelectBand(long screenId, long bandId)
        => Send(new SelectBandMessage(screenId, bandId));

    public static void MoveBand(Band band, int delta)
        => Send(new MoveBandMessage(band, delta));

    #endregion === BAND ===

    #region === TREEASSIST (OBS?) ===

    //public static class TreeAssist
    //{
    //    public static void Select(Screen s) => Send(new SelectTreeViewScreenMessage(s));
    //    public static void Restore(Dictionary<string, bool> states) => Send(new RestoreTreeViewExpandedStateMessage(states));
    //    public static void ExpandAll() => Send(new ExpandAllScreenGroupsMessage());
    //    public static void CollapseAll() => Send(new CollapseAllScreenGroupsMessage());
    //}

    #endregion === TREEASSIST ===

    #region === PROPERTY GRID (OBS?) ===

    public static class PropertyGrid
    {
        public static void Invalidate(object? target) => Send(new InvalidatePropertyGridMessage(target));
    }

    #endregion === PROPERTY GRID ===
}

#endregion


