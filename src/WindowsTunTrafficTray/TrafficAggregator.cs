using System.ComponentModel;

namespace WindowsTunTrafficTray;

public sealed class TrafficAggregator
{
    private readonly Dictionary<string, ConnectionSample> _lastSamples = [];
    private readonly Dictionary<UsageKey, UsageCounter> _counters = [];
    private DateTimeOffset _lastPoll = DateTimeOffset.UtcNow;

    public long TotalDownload => _counters.Values.Sum(counter => counter.Download);
    public long TotalUpload => _counters.Values.Sum(counter => counter.Upload);

    public void Apply(IReadOnlyList<MihomoConnection> connections)
    {
        var now = DateTimeOffset.UtcNow;
        var seconds = Math.Max(0.1, (now - _lastPoll).TotalSeconds);
        _lastPoll = now;
        var seenIds = new HashSet<string>();

        foreach (var counter in _counters.Values)
        {
            counter.DownloadRate = 0;
            counter.UploadRate = 0;
        }

        foreach (var connection in connections)
        {
            if (string.IsNullOrWhiteSpace(connection.Id))
            {
                continue;
            }

            seenIds.Add(connection.Id);

            var sample = new ConnectionSample(connection.Download, connection.Upload);
            if (!_lastSamples.TryGetValue(connection.Id, out var previous))
            {
                _lastSamples[connection.Id] = sample;
                continue;
            }

            var downloadDelta = Math.Max(0, sample.Download - previous.Download);
            var uploadDelta = Math.Max(0, sample.Upload - previous.Upload);
            _lastSamples[connection.Id] = sample;

            if (downloadDelta == 0 && uploadDelta == 0)
            {
                continue;
            }

            var key = UsageKey.From(connection);
            if (!_counters.TryGetValue(key, out var counter))
            {
                counter = new UsageCounter(key);
                _counters[key] = counter;
            }

            counter.Download += downloadDelta;
            counter.Upload += uploadDelta;
            counter.DownloadRate = downloadDelta / seconds;
            counter.UploadRate = uploadDelta / seconds;
        }

        foreach (var id in _lastSamples.Keys.Where(id => !seenIds.Contains(id)).ToList())
        {
            _lastSamples.Remove(id);
        }
    }

    public void Reset()
    {
        _lastSamples.Clear();
        _counters.Clear();
        _lastPoll = DateTimeOffset.UtcNow;
    }

    public IReadOnlyList<UsageNode> BuildRows(string filter, UsageSortColumn sortColumn, ListSortDirection sortDirection)
    {
        var counters = _counters.Values
            .Where(counter => MatchesFilter(counter.Key, filter))
            .ToList();

        var rows = counters
            .GroupBy(counter => new { counter.Key.Process, counter.Key.ProcessPath })
            .Select(group =>
            {
                var processName = string.IsNullOrWhiteSpace(group.Key.Process) ? "Unknown process" : group.Key.Process;
                var node = new UsageNode
                {
                    Name = processName,
                    Path = group.Key.ProcessPath,
                    Chain = BuildProcessChain(group.Select(item => item.Key.Chain)),
                    Download = group.Sum(item => item.Download),
                    Upload = group.Sum(item => item.Upload),
                    DownloadRate = group.Sum(item => item.DownloadRate),
                    UploadRate = group.Sum(item => item.UploadRate)
                };

                node.Children.AddRange(group
                    .GroupBy(item => item.Key.Chain)
                    .Select(chainGroup =>
                    {
                        var chainName = string.IsNullOrWhiteSpace(chainGroup.Key) ? "Unknown chain" : chainGroup.Key;
                        var chainNode = new UsageNode
                        {
                            Name = chainName,
                            Chain = chainName,
                            Download = chainGroup.Sum(item => item.Download),
                            Upload = chainGroup.Sum(item => item.Upload),
                            DownloadRate = chainGroup.Sum(item => item.DownloadRate),
                            UploadRate = chainGroup.Sum(item => item.UploadRate)
                        };

                        chainNode.Children.AddRange(SortNodes(chainGroup
                            .Select(item => new UsageNode
                            {
                                Name = item.Key.Host,
                                Path = item.Key.Remote,
                                Chain = item.Key.Chain,
                                Download = item.Download,
                                Upload = item.Upload,
                                DownloadRate = item.DownloadRate,
                                UploadRate = item.UploadRate
                            }), sortColumn, sortDirection));

                        return chainNode;
                    }));

                SortChildren(node, sortColumn, sortDirection);
                return node;
            })
            .ToList();

        return SortNodes(rows, sortColumn, sortDirection);
    }

    private static bool MatchesFilter(UsageKey key, string filter)
    {
        return filter switch
        {
            "Proxy" => !key.Chain.Equals("DIRECT", StringComparison.OrdinalIgnoreCase),
            "Direct" => key.Chain.Equals("DIRECT", StringComparison.OrdinalIgnoreCase),
            _ => true
        };
    }

    private static string BuildProcessChain(IEnumerable<string> chains)
    {
        var unique = chains.Where(chain => !string.IsNullOrWhiteSpace(chain)).Distinct().Take(3).ToList();
        return unique.Count == 0 ? "" : string.Join(", ", unique);
    }

    private static void SortChildren(UsageNode node, UsageSortColumn sortColumn, ListSortDirection sortDirection)
    {
        var sorted = SortNodes(node.Children, sortColumn, sortDirection).ToList();
        node.Children.Clear();
        node.Children.AddRange(sorted);
    }

    private static IReadOnlyList<UsageNode> SortNodes(IEnumerable<UsageNode> nodes, UsageSortColumn sortColumn, ListSortDirection sortDirection)
    {
        IOrderedEnumerable<UsageNode> ordered = sortColumn switch
        {
            UsageSortColumn.Name => SortText(nodes, item => item.Name, sortDirection),
            UsageSortColumn.Chain => SortText(nodes, item => item.Chain, sortDirection),
            UsageSortColumn.Download => SortNumber(nodes, item => item.Download, sortDirection),
            UsageSortColumn.Upload => SortNumber(nodes, item => item.Upload, sortDirection),
            UsageSortColumn.DownloadRate => SortNumber(nodes, item => item.DownloadRate, sortDirection),
            UsageSortColumn.UploadRate => SortNumber(nodes, item => item.UploadRate, sortDirection),
            UsageSortColumn.Path => SortText(nodes, item => item.Path, sortDirection),
            _ => SortNumber(nodes, item => item.Download + item.Upload, ListSortDirection.Descending)
        };

        return ordered.ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static IOrderedEnumerable<UsageNode> SortText(
        IEnumerable<UsageNode> nodes,
        Func<UsageNode, string> selector,
        ListSortDirection direction)
    {
        return direction == ListSortDirection.Ascending
            ? nodes.OrderBy(selector, StringComparer.OrdinalIgnoreCase)
            : nodes.OrderByDescending(selector, StringComparer.OrdinalIgnoreCase);
    }

    private static IOrderedEnumerable<UsageNode> SortNumber(
        IEnumerable<UsageNode> nodes,
        Func<UsageNode, double> selector,
        ListSortDirection direction)
    {
        return direction == ListSortDirection.Ascending
            ? nodes.OrderBy(selector)
            : nodes.OrderByDescending(selector);
    }
}

public sealed class UsageCounter
{
    public UsageCounter(UsageKey key)
    {
        Key = key;
    }

    public UsageKey Key { get; }
    public long Download { get; set; }
    public long Upload { get; set; }
    public double DownloadRate { get; set; }
    public double UploadRate { get; set; }
}

public sealed record UsageKey(string Process, string ProcessPath, string Host, string Remote, string Chain)
{
    public static UsageKey From(MihomoConnection connection)
    {
        var metadata = connection.Metadata;
        var host = string.IsNullOrWhiteSpace(metadata.Host)
            ? $"{metadata.RemoteDestination}:{metadata.DestinationPort}".Trim(':')
            : $"{metadata.Host}:{metadata.DestinationPort}".Trim(':');

        return new UsageKey(
            metadata.Process,
            metadata.ProcessPath,
            string.IsNullOrWhiteSpace(host) ? "Unknown host" : host,
            metadata.RemoteDestination,
            BuildRoute(connection.Chains));
    }

    private static string BuildRoute(IReadOnlyList<string> chains)
    {
        if (chains.Count == 0)
        {
            return "";
        }

        return string.Join(" / ", chains.AsEnumerable().Reverse());
    }
}

public sealed record ConnectionSample(long Download, long Upload);

public enum UsageSortColumn
{
    Name,
    Chain,
    Download,
    Upload,
    DownloadRate,
    UploadRate,
    Path
}
