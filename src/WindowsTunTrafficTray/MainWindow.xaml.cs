using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MessageBox = System.Windows.MessageBox;
using Point = System.Windows.Point;

namespace WindowsTunTrafficTray;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly AppSettingsService _settingsService = new();
    private readonly UsageHistoryService _historyService = new();
    private readonly TrafficAggregator _aggregator = new();
    private readonly DispatcherTimer _timer;
    private readonly Queue<double> _downloadRateSamples = new();
    private readonly Queue<double> _uploadRateSamples = new();
    private AppSettings _settings;
    private MihomoClient _client;
    private string _selectedFilter = "All";
    private string _searchText = "";
    private string _statusText = "\u6b63\u5728\u542f\u52a8...";
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
    public IReadOnlyList<ConnectionRow> ConnectionRows => ApplySearch(FlattenRows(Rows));

    public string NameHeader => BuildHeader("\u8fdb\u7a0b / \u4e3b\u673a", UsageSortColumn.Name);
    public string ChainHeader => BuildHeader("\u4ee3\u7406\u94fe\u8def", UsageSortColumn.Chain);
    public string DownloadHeader => BuildHeader("\u4e0b\u8f7d\u91cf", UsageSortColumn.Download);
    public string UploadHeader => BuildHeader("\u4e0a\u4f20\u91cf", UsageSortColumn.Upload);
    public string DownloadRateHeader => BuildHeader("\u4e0b\u8f7d\u901f\u5ea6", UsageSortColumn.DownloadRate);
    public string UploadRateHeader => BuildHeader("\u4e0a\u4f20\u901f\u5ea6", UsageSortColumn.UploadRate);
    public string PathHeader => BuildHeader("\u8def\u5f84", UsageSortColumn.Path);
    public string AllSummaryText => BuildSummary(_aggregator.TotalDownload, _aggregator.TotalUpload);
    public string ProxySummaryText => BuildSummary(_aggregator.ProxyDownload, _aggregator.ProxyUpload);
    public string DirectSummaryText => BuildSummary(_aggregator.DirectDownload, _aggregator.DirectUpload);
    public string TotalDownloadText => ByteFormatter.Format(_aggregator.TotalDownload);
    public string TotalUploadText => ByteFormatter.Format(_aggregator.TotalUpload);
    public string ActiveConnectionsText => Rows.Sum(row => row.Children.Sum(child => child.Children.Count)).ToString();
    public string SelectedViewTitle => SelectedFilter switch
    {
        "Proxy" => "\u4ee3\u7406\u8fde\u63a5",
        "Direct" => "\u76f4\u8fde\u8fde\u63a5",
        _ => "\u5168\u90e8\u8fde\u63a5"
    };
    public string SelectedDownloadText => ByteFormatter.Format(GetSelectedDownload());
    public string SelectedUploadText => ByteFormatter.Format(GetSelectedUpload());
    public string AllFilterBackground => IsSelectedFilter("All") ? "#1683F8" : "#F3F4F6";
    public string ProxyFilterBackground => IsSelectedFilter("Proxy") ? "#1683F8" : "#F3F4F6";
    public string DirectFilterBackground => IsSelectedFilter("Direct") ? "#1683F8" : "#F3F4F6";
    public string AllFilterForeground => IsSelectedFilter("All") ? "#FFFFFF" : "#111827";
    public string ProxyFilterForeground => IsSelectedFilter("Proxy") ? "#FFFFFF" : "#111827";
    public string DirectFilterForeground => IsSelectedFilter("Direct") ? "#FFFFFF" : "#111827";
    public PointCollection DownloadSeriesPoints => BuildSeries(_downloadRateSamples);
    public PointCollection UploadSeriesPoints => BuildSeries(_uploadRateSamples);
    public string SessionUploadRateValue => SplitValue(ByteFormatter.Format(_aggregator.TotalUploadRate));
    public string SessionUploadRateUnit => $"{SplitUnit(ByteFormatter.Format(_aggregator.TotalUploadRate))}/s";
    public string SessionDownloadRateValue => SplitValue(ByteFormatter.Format(_aggregator.TotalDownloadRate));
    public string SessionDownloadRateUnit => $"{SplitUnit(ByteFormatter.Format(_aggregator.TotalDownloadRate))}/s";
    public string SessionTotalValue => SplitValue(ByteFormatter.Format(_aggregator.TotalDownload + _aggregator.TotalUpload));
    public string SessionTotalUnit => SplitUnit(ByteFormatter.Format(_aggregator.TotalDownload + _aggregator.TotalUpload));

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
            OnUsageChanged();
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

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText == value)
            {
                return;
            }

            _searchText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ConnectionRows));
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
        OnUsageChanged();
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
            var delta = _aggregator.Apply(snapshot);
            _historyService.AddDelta(DateOnly.FromDateTime(DateTime.Now), delta);
            AddRateSample(_aggregator.TotalDownloadRate, _aggregator.TotalUploadRate);
            StatusText = $"\u5df2\u8fde\u63a5\u3002\u6d3b\u8dc3\u8fde\u63a5\uff1a{snapshot.Count}\u3002";
        }
        catch (UnauthorizedAccessException)
        {
            AddRateSample(0, 0);
            StatusText = "\u8ba4\u8bc1\u5931\u8d25\u3002\u8bf7\u5728\u8bbe\u7f6e\u4e2d\u586b\u5199 Mihomo \u5bc6\u94a5\u3002";
        }
        catch (HttpRequestException ex)
        {
            AddRateSample(0, 0);
            StatusText = $"\u65e0\u6cd5\u8fde\u63a5 Mihomo \u63a7\u5236\u5668\uff1a{ex.Message}";
        }
        catch (TaskCanceledException)
        {
            AddRateSample(0, 0);
            StatusText = "Mihomo \u63a7\u5236\u5668\u54cd\u5e94\u8d85\u65f6\u3002";
        }
        catch (Exception ex)
        {
            AddRateSample(0, 0);
            StatusText = $"\u9519\u8bef\uff1a{ex.Message}";
        }

        OnUsageChanged();
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => RefreshNow();

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        _aggregator.Reset();
        OnUsageChanged();
        RefreshNow();
    }

    private void Records_Click(object sender, RoutedEventArgs e)
    {
        var window = new HistoryWindow(_historyService.Records) { Owner = this };
        window.ShowDialog();
    }

    private void Settings_Click(object sender, RoutedEventArgs e) => OpenSettings();

    private void FilterAll_Click(object sender, RoutedEventArgs e) => SelectedFilter = "All";

    private void FilterProxy_Click(object sender, RoutedEventArgs e) => SelectedFilter = "Proxy";

    private void FilterDirect_Click(object sender, RoutedEventArgs e) => SelectedFilter = "Direct";

    private void SortName_Click(object sender, RoutedEventArgs e) => SortBy(UsageSortColumn.Name);

    private void SortChain_Click(object sender, RoutedEventArgs e) => SortBy(UsageSortColumn.Chain);

    private void SortDownload_Click(object sender, RoutedEventArgs e) => SortBy(UsageSortColumn.Download);

    private void SortUpload_Click(object sender, RoutedEventArgs e) => SortBy(UsageSortColumn.Upload);

    private void SortDownloadRate_Click(object sender, RoutedEventArgs e) => SortBy(UsageSortColumn.DownloadRate);

    private void SortUploadRate_Click(object sender, RoutedEventArgs e) => SortBy(UsageSortColumn.UploadRate);

    private void SortPath_Click(object sender, RoutedEventArgs e) => SortBy(UsageSortColumn.Path);

    private void ConnectionsGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var row = FindParent<DataGridRow>((DependencyObject)e.OriginalSource);
        if (row == null)
        {
            return;
        }

        row.IsSelected = true;
        row.Focus();
    }

    private void OpenProcessLocation_Click(object sender, RoutedEventArgs e)
    {
        if (ConnectionsGrid.SelectedItem is not ConnectionRow { IsProcess: true } row || string.IsNullOrWhiteSpace(row.Path))
        {
            MessageBox.Show(this, "\u8bf7\u5148\u53f3\u952e\u9009\u62e9\u4e00\u4e2a\u8fdb\u7a0b\u884c\u3002", "\u6253\u5f00\u8fdb\u7a0b\u4f4d\u7f6e", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!File.Exists(row.Path))
        {
            MessageBox.Show(this, "\u8fdb\u7a0b\u6587\u4ef6\u4e0d\u5b58\u5728\u6216\u8def\u5f84\u65e0\u6cd5\u8bbf\u95ee\u3002", "\u6253\u5f00\u8fdb\u7a0b\u4f4d\u7f6e", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{row.Path}\"")
        {
            UseShellExecute = true
        });
    }

    private void CloseProcess_Click(object sender, RoutedEventArgs e)
    {
        if (ConnectionsGrid.SelectedItem is not ConnectionRow { IsProcess: true } row)
        {
            MessageBox.Show(this, "\u8bf7\u5148\u53f3\u952e\u9009\u62e9\u4e00\u4e2a\u8fdb\u7a0b\u884c\u3002", "\u5173\u95ed\u8fdb\u7a0b", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            this,
            $"\u786e\u5b9a\u8981\u5173\u95ed {row.Name} \u5417\uff1f\u672a\u4fdd\u5b58\u7684\u6570\u636e\u53ef\u80fd\u4f1a\u4e22\u5931\u3002",
            "\u5173\u95ed\u8fdb\u7a0b",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        var killed = KillProcesses(row);
        MessageBox.Show(
            this,
            killed == 0 ? "\u6ca1\u6709\u627e\u5230\u53ef\u5173\u95ed\u7684\u5339\u914d\u8fdb\u7a0b\u3002" : $"\u5df2\u8bf7\u6c42\u5173\u95ed {killed} \u4e2a\u8fdb\u7a0b\u3002",
            "\u5173\u95ed\u8fdb\u7a0b",
            MessageBoxButton.OK,
            killed == 0 ? MessageBoxImage.Information : MessageBoxImage.None);
    }

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

        OnUsageChanged();
        OnPropertyChanged(nameof(NameHeader));
        OnPropertyChanged(nameof(ChainHeader));
        OnPropertyChanged(nameof(DownloadHeader));
        OnPropertyChanged(nameof(UploadHeader));
        OnPropertyChanged(nameof(DownloadRateHeader));
        OnPropertyChanged(nameof(UploadRateHeader));
        OnPropertyChanged(nameof(PathHeader));
    }

    private void OnUsageChanged()
    {
        OnPropertyChanged(nameof(Rows));
        OnPropertyChanged(nameof(ConnectionRows));
        OnPropertyChanged(nameof(AllSummaryText));
        OnPropertyChanged(nameof(ProxySummaryText));
        OnPropertyChanged(nameof(DirectSummaryText));
        OnPropertyChanged(nameof(TotalDownloadText));
        OnPropertyChanged(nameof(TotalUploadText));
        OnPropertyChanged(nameof(ActiveConnectionsText));
        OnPropertyChanged(nameof(SelectedViewTitle));
        OnPropertyChanged(nameof(SelectedDownloadText));
        OnPropertyChanged(nameof(SelectedUploadText));
        OnPropertyChanged(nameof(AllFilterBackground));
        OnPropertyChanged(nameof(ProxyFilterBackground));
        OnPropertyChanged(nameof(DirectFilterBackground));
        OnPropertyChanged(nameof(AllFilterForeground));
        OnPropertyChanged(nameof(ProxyFilterForeground));
        OnPropertyChanged(nameof(DirectFilterForeground));
        OnPropertyChanged(nameof(DownloadSeriesPoints));
        OnPropertyChanged(nameof(UploadSeriesPoints));
        OnPropertyChanged(nameof(SessionUploadRateValue));
        OnPropertyChanged(nameof(SessionUploadRateUnit));
        OnPropertyChanged(nameof(SessionDownloadRateValue));
        OnPropertyChanged(nameof(SessionDownloadRateUnit));
        OnPropertyChanged(nameof(SessionTotalValue));
        OnPropertyChanged(nameof(SessionTotalUnit));
    }

    private string BuildHeader(string title, UsageSortColumn column)
    {
        if (_sortColumn != column)
        {
            return title;
        }

        return _sortDirection == ListSortDirection.Ascending ? $"{title} ^" : $"{title} v";
    }

    private static bool IsTextColumn(UsageSortColumn column)
    {
        return column is UsageSortColumn.Name or UsageSortColumn.Chain or UsageSortColumn.Path;
    }

    private bool IsSelectedFilter(string filter)
    {
        return string.Equals(SelectedFilter, filter, StringComparison.OrdinalIgnoreCase);
    }

    private long GetSelectedDownload()
    {
        return SelectedFilter switch
        {
            "Proxy" => _aggregator.ProxyDownload,
            "Direct" => _aggregator.DirectDownload,
            _ => _aggregator.TotalDownload
        };
    }

    private long GetSelectedUpload()
    {
        return SelectedFilter switch
        {
            "Proxy" => _aggregator.ProxyUpload,
            "Direct" => _aggregator.DirectUpload,
            _ => _aggregator.TotalUpload
        };
    }

    private static string BuildSummary(long download, long upload)
    {
        return $"\u4e0b\u8f7d {ByteFormatter.Format(download)}   \u4e0a\u4f20 {ByteFormatter.Format(upload)}";
    }

    private IReadOnlyList<ConnectionRow> ApplySearch(IReadOnlyList<ConnectionRow> rows)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return rows;
        }

        var keyword = SearchText.Trim();
        return rows
            .Where(row =>
                row.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                row.Path.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static IReadOnlyList<ConnectionRow> FlattenRows(IEnumerable<UsageNode> rows)
    {
        var result = new List<ConnectionRow>();
        foreach (var process in rows)
        {
            result.Add(ToConnectionRow(process, 0, "\u8fdb\u7a0b", true));
        }

        return result;
    }

    private static ConnectionRow ToConnectionRow(UsageNode node, int level, string type, bool isProcess)
    {
        return new ConnectionRow
        {
            Level = level,
            Type = type,
            Name = node.Name,
            Chain = node.Chain,
            Path = node.Path,
            Download = node.Download,
            Upload = node.Upload,
            DownloadRate = node.DownloadRate,
            UploadRate = node.UploadRate,
            IsProcess = isProcess
        };
    }

    private void AddRateSample(double downloadRate, double uploadRate)
    {
        EnqueueSample(_downloadRateSamples, downloadRate);
        EnqueueSample(_uploadRateSamples, uploadRate);
    }

    private static void EnqueueSample(Queue<double> samples, double value)
    {
        const int sampleLimit = 34;
        samples.Enqueue(Math.Max(0, value));
        while (samples.Count > sampleLimit)
        {
            samples.Dequeue();
        }
    }

    private static PointCollection BuildSeries(IReadOnlyCollection<double> samples)
    {
        const double width = 170;
        const double height = 58;
        const double padding = 3;
        var points = new PointCollection();
        if (samples.Count == 0)
        {
            return points;
        }

        var max = Math.Max(1, samples.Max());
        var index = 0;
        foreach (var sample in samples)
        {
            var x = samples.Count == 1 ? 0 : index * width / (samples.Count - 1);
            var normalized = sample / max;
            var y = height - padding - (height - padding * 2) * normalized;
            points.Add(new Point(x, y));
            index++;
        }

        return points;
    }

    private static string SplitValue(string formatted)
    {
        var parts = formatted.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0 ? "0" : parts[0];
    }

    private static string SplitUnit(string formatted)
    {
        var parts = formatted.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length < 2 ? "B" : parts[1];
    }

    private static T? FindParent<T>(DependencyObject current)
        where T : DependencyObject
    {
        while (current != null)
        {
            if (current is T typed)
            {
                return typed;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private static int KillProcesses(ConnectionRow row)
    {
        var processName = Path.GetFileNameWithoutExtension(row.Path);
        if (string.IsNullOrWhiteSpace(processName))
        {
            processName = Path.GetFileNameWithoutExtension(row.Name);
        }

        if (string.IsNullOrWhiteSpace(processName))
        {
            return 0;
        }

        var killed = 0;
        foreach (var process in Process.GetProcessesByName(processName))
        {
            using (process)
            {
                if (!MatchesProcessPath(process, row.Path))
                {
                    continue;
                }

                process.Kill();
                killed++;
            }
        }

        return killed;
    }

    private static bool MatchesProcessPath(Process process, string expectedPath)
    {
        if (string.IsNullOrWhiteSpace(expectedPath))
        {
            return true;
        }

        try
        {
            return string.Equals(process.MainModule?.FileName, expectedPath, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return true;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
