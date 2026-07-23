using System.ComponentModel;
using System.Windows;

namespace Mockup.Helper;

public static class DesignModeHelper
{
    private static bool? _isInDesignMode;

    /// <summary>
    /// True, wenn Code im Visual Studio Designer (oder Blend) ausgeführt wird.
    /// </summary>
    public static bool IsInDesignMode
    {
        get
        {
            if (_isInDesignMode.HasValue)
                return _isInDesignMode.Value;

            // Methode 1: DependencyObject + DesignerProperties
            var prop = DesignerProperties.IsInDesignModeProperty;
            _isInDesignMode = (bool)DependencyPropertyDescriptor
                .FromProperty(prop, typeof(FrameworkElement))
                .Metadata.DefaultValue;

            // Methode 2 (fallback): Check den IsInDesignTool Flag
            if (!_isInDesignMode.Value)
            {
                _isInDesignMode =
                    DesignerProperties.GetIsInDesignMode(new DependencyObject());
            }

            return _isInDesignMode.Value;
        }
    }
}
