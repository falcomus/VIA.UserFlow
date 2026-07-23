using System.Windows;

namespace UserFlow;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
    }

    public void UpdateStatus(string message, double? percent)
    {
        PART_StatusText.Text = string.IsNullOrWhiteSpace(message)
            ? "The launch is being prepared..."
            : message;

        if (percent.HasValue)
        {
            double value = Math.Clamp(percent.Value, 0d, 100d);
            PART_ProgressBar.IsIndeterminate = false;
            PART_ProgressBar.Value = value;
            PART_PercentText.Text = $"{value:0} %";
        }
        else
        {
            PART_ProgressBar.IsIndeterminate = true;
            PART_PercentText.Text = string.Empty;
        }
    }

    public void SetHeading(string heading)
    {
        PART_HeadingText.Text = string.IsNullOrWhiteSpace(heading)
            ? "Startup process"
            : heading;
    }
}
