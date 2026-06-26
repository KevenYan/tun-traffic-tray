using System.Drawing;
using System.Windows.Forms;

namespace WindowsTunTrafficTray.Installer;

public sealed class InstallerOptionsForm : Form
{
    private readonly TextBox _installDirBox = new();
    private readonly CheckBox _autoStartBox = new();

    public InstallerOptionsForm(string defaultInstallDir)
    {
        Text = "Windows TUN Traffic Tray \u5b89\u88c5\u7a0b\u5e8f";
        Width = 720;
        Height = 430;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(238, 241, 245);
        Font = new Font("Segoe UI", 10);
        var processPath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(processPath))
        {
            Icon = Icon.ExtractAssociatedIcon(processPath);
        }

        var shell = new Panel
        {
            Left = 14,
            Top = 14,
            Width = 674,
            Height = 362,
            BackColor = Color.White
        };

        var brand = new Panel
        {
            Left = 0,
            Top = 0,
            Width = 210,
            Height = 362,
            BackColor = Color.FromArgb(245, 248, 252)
        };

        var badge = new Label
        {
            Left = 28,
            Top = 34,
            Width = 54,
            Height = 54,
            Text = "T",
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 22, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(22, 131, 248)
        };
        var brandTitle = new Label
        {
            Left = 28,
            Top = 108,
            Width = 160,
            Height = 64,
            Text = "TUN Traffic",
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Color.FromArgb(17, 24, 39)
        };
        var brandSubtitle = new Label
        {
            Left = 28,
            Top = 172,
            Width = 150,
            Height = 54,
            Text = "Mihomo TUN \u8fdb\u7a0b\u6d41\u91cf\u76d1\u63a7\u3002",
            ForeColor = Color.FromArgb(100, 116, 139)
        };

        brand.Controls.AddRange([badge, brandTitle, brandSubtitle]);

        var title = new Label
        {
            Text = "\u5b89\u88c5\u9009\u9879",
            Left = 240,
            Top = 30,
            Width = 360,
            Height = 34,
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Color.FromArgb(17, 24, 39)
        };

        var description = new Label
        {
            Text = "\u9009\u62e9\u5b89\u88c5\u76ee\u5f55\uff0c\u5e76\u8bbe\u7f6e\u662f\u5426\u968f Windows \u542f\u52a8\u3002",
            Left = 240,
            Top = 68,
            Width = 390,
            Height = 42,
            ForeColor = Color.FromArgb(100, 116, 139)
        };

        var pathCard = CreateCard(240, 120, 400, 92);
        var pathLabel = new Label
        {
            Text = "\u5b89\u88c5\u4f4d\u7f6e",
            Left = 16,
            Top = 12,
            Width = 180,
            Height = 24,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(31, 41, 55)
        };
        _installDirBox.Left = 16;
        _installDirBox.Top = 42;
        _installDirBox.Width = 282;
        _installDirBox.Height = 26;
        _installDirBox.Text = defaultInstallDir;
        var browseButton = new Button
        {
            Text = "\u6d4f\u89c8",
            Left = 308,
            Top = 40,
            Width = 76,
            Height = 30,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(243, 244, 246)
        };
        browseButton.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
        browseButton.Click += (_, _) => Browse();
        pathCard.Controls.AddRange([pathLabel, _installDirBox, browseButton]);

        var optionCard = CreateCard(240, 226, 400, 72);
        _autoStartBox.Left = 16;
        _autoStartBox.Top = 22;
        _autoStartBox.Width = 350;
        _autoStartBox.Text = "\u5f00\u673a\u81ea\u52a8\u542f\u52a8";
        _autoStartBox.ForeColor = Color.FromArgb(31, 41, 55);
        optionCard.Controls.Add(_autoStartBox);

        var cancelButton = new Button
        {
            Text = "\u53d6\u6d88",
            Left = 464,
            Top = 316,
            Width = 82,
            Height = 34,
            DialogResult = DialogResult.Cancel,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(243, 244, 246)
        };
        cancelButton.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);

        var installButton = new Button
        {
            Text = "\u5b89\u88c5",
            Left = 558,
            Top = 316,
            Width = 82,
            Height = 34,
            DialogResult = DialogResult.OK,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(22, 131, 248),
            ForeColor = Color.White
        };
        installButton.FlatAppearance.BorderColor = Color.FromArgb(22, 131, 248);

        AcceptButton = installButton;
        CancelButton = cancelButton;

        shell.Controls.AddRange([brand, title, description, pathCard, optionCard, cancelButton, installButton]);
        Controls.Add(shell);
    }

    public string InstallDir => _installDirBox.Text.Trim();
    public bool AutoStart => _autoStartBox.Checked;

    private static Panel CreateCard(int left, int top, int width, int height)
    {
        return new Panel
        {
            Left = left,
            Top = top,
            Width = width,
            Height = height,
            BackColor = Color.FromArgb(248, 250, 252)
        };
    }

    private void Browse()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "\u9009\u62e9\u5b89\u88c5\u4f4d\u7f6e",
            SelectedPath = _installDirBox.Text
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _installDirBox.Text = Path.Combine(dialog.SelectedPath, "WindowsTunTrafficTray");
        }
    }
}
