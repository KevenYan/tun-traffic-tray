using System.IO;
using System.Text.Json;

namespace WindowsTunTrafficTray;

public sealed class UsageHistoryService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _historyPath;
    private readonly List<DailyUsageRecord> _records;

    public UsageHistoryService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _historyPath = Path.Combine(appData, "WindowsTunTrafficTray", "history.json");
        _records = LoadRecords();
    }

    public IReadOnlyList<DailyUsageRecord> Records => _records
        .OrderByDescending(record => record.Date)
        .ToList();

    public void AddDelta(DateOnly date, UsageDelta delta)
    {
        if (delta.IsEmpty)
        {
            return;
        }

        var record = _records.FirstOrDefault(item => item.Date == date);
        if (record is null)
        {
            record = new DailyUsageRecord { Date = date };
            _records.Add(record);
        }

        record.AllDownload += delta.AllDownload;
        record.AllUpload += delta.AllUpload;
        record.ProxyDownload += delta.ProxyDownload;
        record.ProxyUpload += delta.ProxyUpload;
        record.DirectDownload += delta.DirectDownload;
        record.DirectUpload += delta.DirectUpload;

        foreach (var appDelta in delta.Applications)
        {
            var app = record.Applications.FirstOrDefault(item =>
                item.Process.Equals(appDelta.Process, StringComparison.OrdinalIgnoreCase) &&
                item.ProcessPath.Equals(appDelta.ProcessPath, StringComparison.OrdinalIgnoreCase));

            if (app is null)
            {
                app = new ApplicationUsageRecord
                {
                    Process = appDelta.Process,
                    ProcessPath = appDelta.ProcessPath
                };
                record.Applications.Add(app);
            }

            app.AllDownload += appDelta.AllDownload;
            app.AllUpload += appDelta.AllUpload;
            app.ProxyDownload += appDelta.ProxyDownload;
            app.ProxyUpload += appDelta.ProxyUpload;
            app.DirectDownload += appDelta.DirectDownload;
            app.DirectUpload += appDelta.DirectUpload;
        }

        Save();
    }

    private List<DailyUsageRecord> LoadRecords()
    {
        if (!File.Exists(_historyPath))
        {
            return [];
        }

        var json = File.ReadAllText(_historyPath);
        return JsonSerializer.Deserialize<List<DailyUsageRecord>>(json) ?? [];
    }

    private void Save()
    {
        var directory = Path.GetDirectoryName(_historyPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_historyPath, JsonSerializer.Serialize(_records, JsonOptions));
    }
}

public sealed class DailyUsageRecord
{
    public DateOnly Date { get; set; }
    public long AllDownload { get; set; }
    public long AllUpload { get; set; }
    public long ProxyDownload { get; set; }
    public long ProxyUpload { get; set; }
    public long DirectDownload { get; set; }
    public long DirectUpload { get; set; }
    public List<ApplicationUsageRecord> Applications { get; set; } = [];

    public string DateText => Date.ToString("yyyy-MM-dd");
    public string AllText => FormatPair(AllDownload, AllUpload);
    public string ProxyText => FormatPair(ProxyDownload, ProxyUpload);
    public string DirectText => FormatPair(DirectDownload, DirectUpload);
    public long AllTotal => AllDownload + AllUpload;
    public long ProxyTotal => ProxyDownload + ProxyUpload;
    public long DirectTotal => DirectDownload + DirectUpload;
    public string AllDownloadText => ByteFormatter.Format(AllDownload);
    public string AllUploadText => ByteFormatter.Format(AllUpload);
    public string ProxyDownloadText => ByteFormatter.Format(ProxyDownload);
    public string ProxyUploadText => ByteFormatter.Format(ProxyUpload);
    public string DirectDownloadText => ByteFormatter.Format(DirectDownload);
    public string DirectUploadText => ByteFormatter.Format(DirectUpload);

    private static string FormatPair(long download, long upload)
    {
        return $"Down {ByteFormatter.Format(download)}   Up {ByteFormatter.Format(upload)}";
    }
}

public sealed class ApplicationUsageRecord
{
    public string Process { get; set; } = "";
    public string ProcessPath { get; set; } = "";
    public long AllDownload { get; set; }
    public long AllUpload { get; set; }
    public long ProxyDownload { get; set; }
    public long ProxyUpload { get; set; }
    public long DirectDownload { get; set; }
    public long DirectUpload { get; set; }

    public string DisplayName => string.IsNullOrWhiteSpace(Process) ? "Unknown process" : Process;
    public string AllText => FormatPair(AllDownload, AllUpload);
    public string ProxyText => FormatPair(ProxyDownload, ProxyUpload);
    public string DirectText => FormatPair(DirectDownload, DirectUpload);
    public long AllTotal => AllDownload + AllUpload;
    public long ProxyTotal => ProxyDownload + ProxyUpload;
    public long DirectTotal => DirectDownload + DirectUpload;
    public string AllDownloadText => ByteFormatter.Format(AllDownload);
    public string AllUploadText => ByteFormatter.Format(AllUpload);
    public string ProxyDownloadText => ByteFormatter.Format(ProxyDownload);
    public string ProxyUploadText => ByteFormatter.Format(ProxyUpload);
    public string DirectDownloadText => ByteFormatter.Format(DirectDownload);
    public string DirectUploadText => ByteFormatter.Format(DirectUpload);

    private static string FormatPair(long download, long upload)
    {
        return $"Down {ByteFormatter.Format(download)}   Up {ByteFormatter.Format(upload)}";
    }
}

public sealed record UsageDelta(
    long AllDownload,
    long AllUpload,
    long ProxyDownload,
    long ProxyUpload,
    long DirectDownload,
    long DirectUpload,
    IReadOnlyList<ApplicationUsageDelta> Applications)
{
    public static UsageDelta Empty { get; } = new(0, 0, 0, 0, 0, 0, []);

    public bool IsEmpty => AllDownload == 0 && AllUpload == 0;
}

public sealed record ApplicationUsageDelta(
    string Process,
    string ProcessPath,
    long AllDownload,
    long AllUpload,
    long ProxyDownload,
    long ProxyUpload,
    long DirectDownload,
    long DirectUpload);
