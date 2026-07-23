using VIA.WPF.Windowing;

namespace Mockup.Dialogs;

/// <summary>
/// Themed modal dialog base.
/// The caller owns the host dim overlay. Acquiring it here makes an owned
/// dialog compete with the main-window overlay during source initialization.
/// </summary>
public class ModalDialogWindow : XWindow { }
