# Windows TUN Traffic Tray

Windows TUN Traffic Tray 是一个 Windows 托盘工具，用来统计 Clash Verge / Mihomo TUN 模式下各个进程的网络用量。

它不自己写网络驱动，也不直接抓包；第一版直接读取 Mihomo 控制接口 `/connections`，把连接里的进程名、进程路径、域名、上传、下载和链路信息汇总成表格。

## 当前版本

当前版本：`v0.1.2`

## 功能

- 托盘常驻，支持打开主窗口、刷新、设置、退出。
- 读取 Mihomo `/connections` 接口。
- 按进程汇总上传量、下载量、上传速度、下载速度。
- 可展开查看每个进程访问的域名。
- 显示链路，例如 `Proxy`、`DIRECT` 或具体节点。
- 每个进程下按代理方式/链路单独统计用量。
- 顶部按 All / Proxy / DIRECT 分别显示当前会话用量。
- 支持 `All`、`Proxy`、`DIRECT` 过滤。
- 点击表格标题可以排序。
- 按日期保存每日 All / Proxy / DIRECT 用量记录。
- 安装器支持选择安装目录和开机自启。
- 支持重置当前会话统计。
- 本地保存 controller 地址和 secret，不提交到 GitHub。
- 提供可安装程序，安装后有桌面快捷方式、开始菜单快捷方式和卸载入口。

## 运行要求

- Windows 10/11
- Clash Verge / Mihomo 已开启 external controller
- 使用轻量安装包前，需要安装 .NET 10 Desktop Runtime
- 如果要自己编译，需要 .NET 10 SDK

默认控制接口：

```text
http://127.0.0.1:9097
```

本地设置文件位置：

```text
%APPDATA%\WindowsTunTrafficTray\settings.json
```

这个文件可能包含 Mihomo secret，不要提交到 GitHub。

## 下载安装

从 GitHub Releases 下载：

```text
WindowsTunTrafficTraySetup.exe
```

双击安装即可。安装位置：

```text
%LOCALAPPDATA%\Programs\WindowsTunTrafficTray
```

安装器会创建：

- 桌面快捷方式
- 开始菜单快捷方式
- Windows 设置里的卸载入口

安装时可以选择安装目录，也可以勾选开机自启。

## 本地运行

```powershell
.\run.ps1
```

## 本地构建

```powershell
.\build.ps1
```

## 生成安装包

```powershell
powershell -ExecutionPolicy Bypass -File .\package-installer.ps1
```

生成结果：

```text
artifacts\WindowsTunTrafficTraySetup.exe
```

当前安装包使用折中方案：安装器自带运行时，保证可以双击安装；安装后的主程序是框架依赖版本，电脑需要安装 .NET 10 Desktop Runtime。这样比完整自包含安装包更小。

## 发布 Release

推荐版本号格式：

```text
v0.1.0
v0.2.0
v1.0.0
```

发布步骤：

```powershell
git tag v0.1.0
git push origin main
git push origin v0.1.0
```

然后在 GitHub Releases 上传：

```text
artifacts\WindowsTunTrafficTraySetup.exe
```

## Git 分支建议

- `main`：稳定版本
- `dev`：日常开发
- `feature/*`：单个功能开发

建议小步提交，例如：

```text
Create tray app shell
Read Mihomo connections
Aggregate usage by process
Add installer packaging
Release v0.1.0
```

## 卸载

在 Windows 设置的“应用”里找到 `Windows TUN Traffic Tray` 并卸载。

也可以运行安装目录里的卸载器：

```powershell
WindowsTunTrafficTraySetup.exe --uninstall
```
