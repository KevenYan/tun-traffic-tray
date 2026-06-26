using System.ComponentModel;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;

namespace WindowsTunTrafficTray;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly AppSettingsService _settingsService = new();
    private readonly TrafficAggregator _aggregator = new();
    private readonly DispatcherTimer _timer;
    private AppSettings _settings;
    private MihomoClient _client;
    private string _selectedFilter = "All";
    private string _statusText = "Starting...";

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        _settings = _settingsService.Load();
        _client = new MihomoClient(_settings);

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(Math.Max(1, _settings.PollIntervalSeconds)) };
        _timer.Tick += async (_, _) => await PollAsync();
        _timer.Start();

        Loaded += async (_, _) => await PollAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<UsageNode> Rows => _aggregator.BuildRows(SelectedFilter);

    public string SelectedFilter
    {
        get => _selectedFilter;
        set
        {
            if (_selectedFilter == value)
            {
                return;
            }

            _selectedFilter = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Rows));
        }
    }

    public string StatusText
    {
        get => _statusText;
        set
        {
            _statusText = value;
            OnPropertyChanged();
        }
    }

    public async void RefreshNow()
    {
        await PollAsync();
    }

    public void OpenSettings()
    {
        var window = new SettingsWindow(_settings) { Owner = this };
        if (window.ShowDialog() != true)
        {
            return;
        }

        _settings = window.Settings;
        _settingsService.Save(_settings);
        _client = new MihomoClient(_settings);
        _timer.Interval = TimeSpan.FromSeconds(Math.Max(1, _settings.PollIntervalSeconds));
        _aggregator.Reset();
        OnPropertyChanged(nameof(Rows));
        RefreshNow();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }

    private async Task PollAsync()
    {
        try
        {
            var snapshot = await _client.GetConnectionsAsync();
            _aggregator.Apply(snapshot);
            StatusText = $"Connected. Active connections: {snapshot.Count}. Total: {ByteFormatter.Format(_aggregator.TotalDownload)} down, {ByteFormatter.Format(_aggregator.TotalUpload)} up.";
        }
        catch (UnauthorizedAccessException)
        {
            StatusText = "Unauthorized. Open Settings and enter the Mihomo secret.";
        }
        catch (HttpRequestException ex)
        {
            StatusText = $"Cannot reach Mihomo controller: {ex.Message}";
        }
        catch (TaskCanceledException)
        {
            StatusText = "Mihomo controller timed out.";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }

        OnPropertyChanged(nameof(Rows));
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => RefreshNow();

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        _aggregator.Reset();
        OnPropertyChanged(nameof(Rows));
        RefreshNow();
    }

    private void Settings_Click(object sender, RoutedEventArgs e) => OpenSettings();

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
