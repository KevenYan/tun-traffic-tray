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
    private UsageSortColumn _sortColumn = UsageSortColumn.Download;
    private ListSortDirection _sortDirection = ListSortDirection.Descending;

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

    public IReadOnlyList<UsageNode> Rows => _aggregator.BuildRows(SelectedFilter, _sortColumn, _sortDirection);

    public string NameHeader => BuildHeader("Process / Host", UsageSortColumn.Name);
    public string ChainHeader => BuildHeader("Proxy / Chain", UsageSortColumn.Chain);
    public string DownloadHeader => BuildHeader("Download", UsageSortColumn.Download);
    public string UploadHeader => BuildHeader("Upload", UsageSortColumn.Upload);
    public string DownloadRateHeader => BuildHeader("Down Speed", UsageSortColumn.DownloadRate);
    public string UploadRateHeader => BuildHeader("Up Speed", UsageSortColumn.UploadRate);
    public string PathHeader => BuildHeader("Path", UsageSortColumn.Path);

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

    private void SortName_Click(object sender, RoutedEventArgs e) => SortBy(UsageSortColumn.Name);

    private void SortChain_Click(object sender, RoutedEventArgs e) => SortBy(UsageSortColumn.Chain);

    private void SortDownload_Click(object sender, RoutedEventArgs e) => SortBy(UsageSortColumn.Download);

    private void SortUpload_Click(object sender, RoutedEventArgs e) => SortBy(UsageSortColumn.Upload);

    private void SortDownloadRate_Click(object sender, RoutedEventArgs e) => SortBy(UsageSortColumn.DownloadRate);

    private void SortUploadRate_Click(object sender, RoutedEventArgs e) => SortBy(UsageSortColumn.UploadRate);

    private void SortPath_Click(object sender, RoutedEventArgs e) => SortBy(UsageSortColumn.Path);

    private void SortBy(UsageSortColumn column)
    {
        if (_sortColumn == column)
        {
            _sortDirection = _sortDirection == ListSortDirection.Ascending
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;
        }
        else
        {
            _sortColumn = column;
            _sortDirection = IsTextColumn(column) ? ListSortDirection.Ascending : ListSortDirection.Descending;
        }

        OnPropertyChanged(nameof(Rows));
        OnPropertyChanged(nameof(NameHeader));
        OnPropertyChanged(nameof(ChainHeader));
        OnPropertyChanged(nameof(DownloadHeader));
        OnPropertyChanged(nameof(UploadHeader));
        OnPropertyChanged(nameof(DownloadRateHeader));
        OnPropertyChanged(nameof(UploadRateHeader));
        OnPropertyChanged(nameof(PathHeader));
    }

    private string BuildHeader(string title, UsageSortColumn column)
    {
        if (_sortColumn != column)
        {
            return title;
        }

        return _sortDirection == ListSortDirection.Ascending ? $"{title} ↑" : $"{title} ↓";
    }

    private static bool IsTextColumn(UsageSortColumn column)
    {
        return column is UsageSortColumn.Name or UsageSortColumn.Chain or UsageSortColumn.Path;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
