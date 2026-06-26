# Windows TUN Traffic Tray

A Windows tray app that summarizes Clash Verge / Mihomo TUN traffic by process.

The first version reads Mihomo's controller API instead of capturing packets directly. This keeps the app simple and avoids writing a Windows network driver.

## Features

- Tray icon with Open, Refresh, Settings, and Exit.
- Reads Mihomo `/connections`.
- Groups traffic by process and domain.
- Shows upload, download, current speed, and route chain.
- Filters All, Proxy, and DIRECT traffic.
- Stores controller URL and secret in the current user's AppData folder.

## Requirements

- Windows
- Clash Verge / Mihomo with external controller enabled
- .NET 10 SDK to build locally

Default controller:

```text
http://127.0.0.1:9097
```

Settings are stored locally:

```text
%APPDATA%\WindowsTunTrafficTray\settings.json
```

Do not commit this settings file. It may contain your Mihomo secret.

## Build

```powershell
.\build.ps1
```

## Create Installer

```powershell
powershell -ExecutionPolicy Bypass -File .\package-installer.ps1
```

The installer will be created here:

```text
artifacts\WindowsTunTrafficTraySetup.exe
```

The installer is per-user. It installs to:

```text
%LOCALAPPDATA%\Programs\WindowsTunTrafficTray
```

It also creates Start Menu and Desktop shortcuts, and adds an uninstall entry to Windows Settings.

## Run

```powershell
.\run.ps1
```

## Version Management

Recommended branch flow:

- `main`: stable builds
- `dev`: daily development
- `feature/*`: individual changes

Recommended first commits:

```text
Create WPF tray app shell
Read Mihomo connections
Aggregate usage by process
Add settings window
```

## Publish to GitHub

This folder is already a local Git repository. To publish it:

1. Create an empty GitHub repository named `windows-tun-traffic-tray`.
2. Configure your local Git identity:

```powershell
git config user.name "Your GitHub Name"
git config user.email "your-email@example.com"
```

3. Commit and push:

```powershell
git add .
git commit -m "Create Windows TUN traffic tray app"
git remote add origin https://github.com/YOUR_NAME/windows-tun-traffic-tray.git
git push -u origin main
git switch -c dev
git push -u origin dev
```

