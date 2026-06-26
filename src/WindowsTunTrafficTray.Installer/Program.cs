using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

namespace WindowsTunTrafficTray.Installer;

internal static class Program
{
    private const string AppName = "Windows TUN Traffic Tray";
    private const string AppExe = "WindowsTunTrafficTray.exe";
    private const string AppVersion = "0.1.1";
    private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\WindowsTunTrafficTray";

    [STAThread]
    private static void Main(string[] args)
    {
        try
        {
            var silent = args.Any(arg => arg.Equals("--silent", StringComparison.OrdinalIgnoreCase));
            if (args.Any(arg => arg.Equals("--uninstall", StringComparison.OrdinalIgnoreCase)))
            {
                Uninstall(silent);
                return;
            }

            var launch = !args.Any(arg => arg.Equals("--no-launch", StringComparison.OrdinalIgnoreCase));
            Install(silent, launch);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void Install(bool silent, bool launch)
    {
        var installDir = GetInstallDir();
        Directory.CreateDirectory(installDir);

        StopApp();
        ExtractPayload(installDir);

        var setupPath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(setupPath))
        {
            File.Copy(setupPath, Path.Combine(installDir, "WindowsTunTrafficTraySetup.exe"), true);
        }

        var appPath = Path.Combine(installDir, AppExe);
        CreateShortcut(GetStartMenuShortcutPath(), appPath);
        CreateShortcut(GetDesktopShortcutPath(), appPath);
        RegisterUninstall(installDir);

        if (launch)
        {
            Process.Start(new ProcessStartInfo(appPath) { UseShellExecute = true });
        }

        if (!silent)
        {
            MessageBox.Show("Installed successfully.", AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private static void Uninstall(bool silent)
    {
        StopApp();

        DeleteFileIfExists(GetStartMenuShortcutPath());
        DeleteFileIfExists(GetDesktopShortcutPath());
        Registry.CurrentUser.DeleteSubKeyTree(RegistryKeyPath, false);

        var installDir = GetInstallDir();
        ScheduleSelfDelete(installDir);
        if (!silent)
        {
            MessageBox.Show("Uninstalled successfully.", AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private static void ExtractPayload(string installDir)
    {
        using var payload = Assembly.GetExecutingAssembly().GetManifestResourceStream("Payload.zip")
            ?? throw new InvalidOperationException("Installer payload is missing. Run package-installer.ps1 to build the installer.");
        using var archive = new ZipArchive(payload, ZipArchiveMode.Read);
        archive.ExtractToDirectory(installDir, true);
    }

    private static void StopApp()
    {
        foreach (var process in Process.GetProcessesByName("WindowsTunTrafficTray"))
        {
            try
            {
                process.Kill();
                process.WaitForExit(3000);
            }
            catch
            {
                // Best effort: install can continue if the process exits by itself.
            }
        }
    }

    private static void CreateShortcut(string shortcutPath, string targetPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(shortcutPath)!);

        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("Windows shortcut service is unavailable.");
        dynamic shell = Activator.CreateInstance(shellType)!;
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = targetPath;
        shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);
        shortcut.Description = AppName;
        shortcut.Save();
    }

    private static void RegisterUninstall(string installDir)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath);
        var setupPath = Path.Combine(installDir, "WindowsTunTrafficTraySetup.exe");
        var appPath = Path.Combine(installDir, AppExe);

        key.SetValue("DisplayName", AppName);
        key.SetValue("DisplayVersion", AppVersion);
        key.SetValue("Publisher", "Local");
        key.SetValue("InstallLocation", installDir);
        key.SetValue("DisplayIcon", appPath);
        key.SetValue("UninstallString", $"\"{setupPath}\" --uninstall");
        key.SetValue("NoModify", 1, RegistryValueKind.DWord);
        key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
    }

    private static void ScheduleSelfDelete(string installDir)
    {
        var command = $"/c for /l %i in (1,1,20) do @(timeout /t 1 /nobreak > nul & rmdir /s /q \"{installDir}\" 2> nul & if not exist \"{installDir}\" exit)";
        Process.Start(new ProcessStartInfo("cmd.exe", command)
        {
            CreateNoWindow = true,
            UseShellExecute = false
        });
    }

    private static string GetInstallDir()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "WindowsTunTrafficTray");
    }

    private static string GetStartMenuShortcutPath()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", $"{AppName}.lnk");
    }

    private static string GetDesktopShortcutPath()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), $"{AppName}.lnk");
    }

    private static void DeleteFileIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
