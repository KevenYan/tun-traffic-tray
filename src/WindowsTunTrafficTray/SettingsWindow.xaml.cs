using System.Windows;

namespace WindowsTunTrafficTray;

public partial class SettingsWindow : Window
{
    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        Settings = settings.Clone();
        DataContext = Settings;
        SecretBox.Password = Settings.Secret;
    }

    public AppSettings Settings { get; private set; }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        Settings.Secret = SecretBox.Password;

        if (!Uri.TryCreate(Settings.ControllerUrl, UriKind.Absolute, out _))
        {
            System.Windows.MessageBox.Show(this, "Controller URL is invalid.", "Settings", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (Settings.PollIntervalSeconds < 1)
        {
            Settings.PollIntervalSeconds = 1;
        }

        DialogResult = true;
    }
}
