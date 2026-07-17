# AudioPress 音频批量压缩器

AudioPress 是一个 Windows 绿色版音频批量压缩工具，使用 C# + .NET 10 + WPF 构建，底层调用 FFmpeg/ffprobe 完成音频识别、压缩和格式转换。

## V0.1.0 功能

- 批量添加文件、添加文件夹、递归扫描和拖拽导入。
- 支持常见输入：MP3、WAV、FLAC、M4A、AAC、OGG、Opus、WMA、AIFF、AC3、MKA 等。
- 输出 MP3、M4A/AAC、Opus、OGG、FLAC、WAV。
- 内置语音极小、语音清晰、播客标准、音乐均衡、音乐高品质、MP3兼容、无损归档、WAV编辑预设。
- 可调码率、采样率、声道、VBR 质量、FLAC 压缩等级和 WAV 位深。
- 尽可能保留标题、艺术家、专辑和封面。
- 单文件进度、总体进度、失败不中断、单项取消和全部取消。
- 支持中文路径、空格、括号和特殊字符。
- 输出到指定目录，或源目录下的 `Compressed` 文件夹。
- 同名处理支持跳过、自动编号、覆盖。
- 设置保存和本地日志。

## 绿色版结构

发布包形如：

```text
AudioPress-v0.1.0-win-x64-portable.zip
└─ AudioPress
   ├─ AudioPress.exe
   ├─ tools
   │  └─ ffmpeg
   │     ├─ ffmpeg.exe
   │     └─ ffprobe.exe
   ├─ README.txt
   ├─ LICENSE
   └─ THIRD_PARTY_NOTICES.md
```

用户只需要解压并运行 `AudioPress.exe`，不需要安装 .NET Runtime、Python 或 FFmpeg。

## 开发

需要 .NET 10 SDK。

```powershell
dotnet restore AudioPress.sln
dotnet build AudioPress.sln
dotnet test tests/AudioPress.Core.Tests/AudioPress.Core.Tests.csproj
```

macOS 可以维护源码和运行 Core 测试；WPF 图形界面和 Windows 绿色版发布在 Windows 或 GitHub Actions 上完成。

## 普通用户使用方式

1. 打开 GitHub Releases。
2. 下载 `AudioPress-v0.1.0-win-x64-portable.zip`。
3. 解压整个 ZIP。
4. 双击 `AudioPress.exe`。
5. 添加文件或文件夹，选择预设和输出位置，然后点击“开始压缩”。

## 隐私说明

- 音频文件只在本机处理。
- 不需要账号登录。
- 不上传音频、文件名或元数据。
- 日志只保存在用户本机。

## Windows 打包

```powershell
./scripts/package-windows.ps1 -Version 0.1.0
```

脚本会下载 Windows 版 FFmpeg/ffprobe，发布 `win-x64 self-contained`，并生成 ZIP 和 SHA256 校验文件。

### 中国大陆网络环境

如果 GitHub 下载较慢，可以先从你信任的大陆镜像或内网缓存下载 Windows x64 的 FFmpeg LGPL 构建，并把文件放到：

```text
tools/ffmpeg/ffmpeg.exe
tools/ffmpeg/ffprobe.exe
```

之后再运行打包脚本，`scripts/prepare-ffmpeg.ps1` 会优先复用已有文件，不会重复下载。

也可以临时指定下载地址：

```powershell
$env:AUDIOPRESS_FFMPEG_URL = "https://你的镜像地址/ffmpeg-win64-lgpl.zip"
./scripts/package-windows.ps1 -Version 0.1.0
```

发布前请确认镜像文件来源可信、许可证可再分发，并且不是 `--enable-nonfree` 构建。

## GitHub Release

推送标签会触发 Release：

```powershell
git tag v0.1.0
git push origin main --tags
```

GitHub Actions 会构建、测试、运行真实 FFmpeg 压缩烟测、打包绿色版，并上传到 Release。

## 发布风格

本项目沿用 `MacHeartRateMonitor` 的发布习惯：使用 `vX.Y.Z` 标签，Release 标题使用中文产品名和版本号，资产文件名包含产品名、版本和平台。
