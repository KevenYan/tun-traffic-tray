using System.Windows;

namespace WindowsTunTrafficTray;

public sealed class ConnectionRow
{
    public int Level { get; init; }
    public string Type { get; init; } = "";
    public string Name { get; init; } = "";
    public string Chain { get; init; } = "";
    public string Path { get; init; } = "";
    public long Download { get; init; }
    public long Upload { get; init; }
    public double DownloadRate { get; init; }
    public double UploadRate { get; init; }
    public bool IsProcess { get; init; }

    public Thickness IndentMargin => new(Level * 18, 0, 0, 0);
    public FontWeight RowFontWeight => Level == 0 ? FontWeights.SemiBold : FontWeights.Normal;
    public string DownloadText => ByteFormatter.Format(Download);
    public string UploadText => ByteFormatter.Format(Upload);
    public string DownloadRateText => $"{ByteFormatter.Format(DownloadRate)}/s";
    public string UploadRateText => $"{ByteFormatter.Format(UploadRate)}/s";
}
