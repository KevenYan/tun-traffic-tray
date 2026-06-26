using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WindowsTunTrafficTray;

public sealed class AppSettings : INotifyPropertyChanged
{
    private string _controllerUrl = "http://127.0.0.1:9097";
    private string _secret = "";
    private int _pollIntervalSeconds = 2;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string ControllerUrl
    {
        get => _controllerUrl;
        set
        {
            _controllerUrl = value;
            OnPropertyChanged();
        }
    }

    public string Secret
    {
        get => _secret;
        set
        {
            _secret = value;
            OnPropertyChanged();
        }
    }

    public int PollIntervalSeconds
    {
        get => _pollIntervalSeconds;
        set
        {
            _pollIntervalSeconds = value;
            OnPropertyChanged();
        }
    }

    public AppSettings Clone()
    {
        return new AppSettings
        {
            ControllerUrl = ControllerUrl,
            Secret = Secret,
            PollIntervalSeconds = PollIntervalSeconds
        };
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
