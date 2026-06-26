namespace WindowsTunTrafficTray;

public sealed class UsageNode
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public string Chain { get; set; } = "";
    public long Download { get; set; }
    public long Upload { get; set; }
    public double DownloadRate { get; set; }
    public double UploadRate { get; set; }
    public List<UsageNode> Children { get; } = [];

    public string DownloadText => ByteFormatter.Format(Download);
    public string UploadText => ByteFormatter.Format(Upload);
    public string DownloadRateText => $"{ByteFormatter.Format(DownloadRate)}/s";
    public string UploadRateText => $"{ByteFormatter.Format(UploadRate)}/s";
}
