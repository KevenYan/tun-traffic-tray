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
            System.Windows.MessageBox.Show(this, "\u63a7\u5236\u5668\u5730\u5740\u65e0\u6548\u3002", "\u8bbe\u7f6e", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (Settings.PollIntervalSeconds < 1)
        {
            Settings.PollIntervalSeconds = 1;
        }

        DialogResult = true;
    }
}
