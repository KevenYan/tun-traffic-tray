using System.Windows;

namespace WindowsTunTrafficTray;

public partial class HistoryWindow : Window
{
    public HistoryWindow(IReadOnlyList<DailyUsageRecord> records)
    {
        InitializeComponent();
        DataContext = new HistoryWindowModel(records);
    }
}

public sealed class HistoryWindowModel
{
    public HistoryWindowModel(IReadOnlyList<DailyUsageRecord> records)
    {
        Records = records;
    }

    public IReadOnlyList<DailyUsageRecord> Records { get; }
}
