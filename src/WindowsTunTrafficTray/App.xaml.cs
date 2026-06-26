using System.Drawing;
using System.Windows;
using Forms = System.Windows.Forms;

namespace WindowsTunTrafficTray;

public partial class App : System.Windows.Application
{
    private Forms.NotifyIcon? _notifyIcon;
    private MainWindow? _mainWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _mainWindow = new MainWindow();
        _mainWindow.Hide();

        var processPath = Environment.ProcessPath;
        var appIcon = !string.IsNullOrWhiteSpace(processPath)
            ? Icon.ExtractAssociatedIcon(processPath)
            : null;

        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = appIcon ?? SystemIcons.Application,
            Text = "TUN \u6d41\u91cf\u76d1\u63a7",
            Visible = true,
            ContextMenuStrip = BuildMenu()
        };
        _notifyIcon.MouseClick += (_, args) =>
        {
            if (args.Button == Forms.MouseButtons.Left)
            {
                ShowMainWindow();
            }
        };
    }

    private Forms.ContextMenuStrip BuildMenu()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("\u6253\u5f00", null, (_, _) => ShowMainWindow());
        menu.Items.Add("\u5237\u65b0", null, (_, _) => _mainWindow?.RefreshNow());
        menu.Items.Add("\u8bbe\u7f6e", null, (_, _) => _mainWindow?.OpenSettings());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("\u9000\u51fa", null, (_, _) => Shutdown());
        return menu;
    }

    private void ShowMainWindow()
    {
        if (_mainWindow is null)
        {
            _mainWindow = new MainWindow();
        }

        _mainWindow.Show();
        _mainWindow.Activate();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_notifyIcon is not null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }

        base.OnExit(e);
    }
}
