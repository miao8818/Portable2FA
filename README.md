<div align="center">
  <img src="Resources/app-preview.png" alt="Portable 2FA icon" width="112" height="112">
  <h1>Portable 2FA</h1>
  <p>轻量、便携、面向 Windows 的 TOTP 验证码生成器。</p>
  <p>
    <a href="https://github.com/miao8818/Portable2FA/releases/latest">下载最新版</a>
    · <a href="使用说明.md">使用说明</a>
    · <a href="CHANGELOG.md">更新日志</a>
    · <a href="LICENSE">MIT License</a>
  </p>
  <p>
    <a href="https://linux.do/"><img src="https://img.shields.io/badge/LINUX-DO-FFB003?style=flat-square" alt="LINUX DO"></a>
    <img src="https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4?style=flat-square" alt="Windows 10/11">
    <img src="https://img.shields.io/github/v/release/miao8818/Portable2FA?style=flat-square&label=version&color=16a085" alt="Latest version">
    <img src="https://img.shields.io/badge/license-MIT-2f3640?style=flat-square" alt="MIT License">
  </p>
</div>

![Portable 2FA 主界面](Resources/screenshots/01-main.png)

## 项目介绍

Portable 2FA 是一个单文件 Windows 桌面工具。它可以读取 Base32 密钥、
`otpauth://` 链接或二维码，实时生成 TOTP 验证码并显示倒计时。验证码可点击复制，
关闭或最小化窗口后程序继续驻留系统托盘。

账户由用户主动保存到本地加密账户库，也可以选择通过 Windows Credential Locker
随 Microsoft 账户跨 Windows 设备漫游。程序不包含自建服务器、账户系统、遥测或广告。

## 下载与运行

从 [GitHub Releases](https://github.com/miao8818/Portable2FA/releases/latest) 下载
`Portable2FA-v版本号-win-portable.exe`，放到任意文件夹后双击运行，无需安装。

Windows 可能对未购买代码签名证书的新 EXE 显示信誉提示。Release 同时提供 SHA-256
校验文件，项目也可以使用仓库中的 `build.ps1` 从源码复现构建。

## 功能说明

### 1. 输入密钥并生成验证码

输入框支持 Base32 密钥和 `otpauth://totp/...` 链接。支持 SHA1、SHA256、SHA512，
6 位或 8 位验证码，以及 5 至 300 秒周期。环形倒计时会实时显示当前验证码剩余秒数，
点击验证码或“复制验证码”按钮即可复制。

输入框右侧按钮从左到右依次为：

1. 截图识别二维码。
2. 显示或隐藏密钥。
3. 选择本地二维码图片。
4. 从剪贴板粘贴文本或二维码图片。

![二维码识别后的验证码](Resources/screenshots/06-qr-result.png)

### 2. 二维码导入与截图识别

- **图片文件**：选择 PNG、JPG、BMP、GIF 或 TIFF 等二维码图片。
- **剪贴板**：复制二维码图片后点击粘贴按钮，程序自动识别；普通文本仍按密钥处理。
- **屏幕截图**：点击准星按钮或按截图快捷键，框选二维码区域后自动识别并返回主界面。

截图只在当前进程中用于识别，程序不会自动保存截图文件。识别完成后仍需由用户确认并
点击“保存账户”，不会因为扫描二维码自动写入账户库。

![框选屏幕二维码](Resources/screenshots/05-screen-capture.png)

### 3. 加密账户库与标签

点击“＋ 新增账户”会清空当前输入并开始录入新账户，它本身不会保存任何内容。密钥解析
成功后点击“保存账户”，填写必填的**标签**，并可填写服务名称和账户。

标签用于在账户库中快速区分密钥，例如“GitHub 工作账号”或完整邮箱地址。长邮箱、长标签
会先适度缩小字号，再在列表尾部显示省略号；鼠标悬停可查看完整标签和账户信息，编辑时
输入框中也会保留完整内容，不会截断原始数据。

![账户库与长邮箱标签](Resources/screenshots/02-account-library.png)

![保存账户时填写标签](Resources/screenshots/03-save-account.png)

只有用户主动保存的账户才会持久化。账户库位于
`%LOCALAPPDATA%\Portable2FA\vault.dat`，整个文件使用 Windows DPAPI CurrentUser
加密，只能由同一 Windows 用户上下文解密。列表支持选择、更新和删除账户。

### 4. Windows 跨设备同步

在“设置”中启用 Windows 账户同步后，应用通过 Windows Credential Locker 保存账户，
并使用 Windows/Microsoft 账户提供的凭据漫游能力在设备之间同步，不需要手动复制加密库
文件。当前最多同步 20 个有效账户，删除记录也会参与合并，避免旧设备恢复已删除账户。

同步依赖系统中的 Microsoft 账户漫游设置；域策略、组织策略、Credential Locker 不可用
或系统关闭凭据漫游时可能无法同步。Windows Hello 属于设备身份验证能力，不是本项目的
跨设备同步通道。

### 5. 全局快捷键

默认快捷键：

- `Ctrl+Alt+T`：从任意程序中唤起 Portable 2FA。
- `Ctrl+Alt+Q`：直接进入屏幕二维码框选。

两个快捷键都可以在设置页面中自定义。点击对应输入框后直接按下组合键，再保存设置；若
组合键被其他程序占用，主界面会显示注册失败提示。

### 6. 开机启动与系统托盘

启用“登录 Windows 后自动启动”后，程序写入当前用户的
`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`。开机启动使用 `--startup`
参数，启动后直接隐藏到托盘，不打断桌面操作。

关闭窗口、按 `Esc` 或最小化都会进入托盘。双击托盘图标或使用唤起快捷键恢复窗口；托盘
右键菜单可以显示窗口、复制当前验证码、截图识别二维码、打开设置或彻底退出。

![快捷键、同步和开机启动设置](Resources/screenshots/04-settings.png)

### 7. 高 DPI 与窗口显示

应用声明 Per-Monitor V2 DPI 感知，界面以 96 DPI 为设计基线并按显示器缩放。已针对
2560×1600、150% 缩放场景修复固定最大/最小尺寸造成的内容显示不全问题；窗口、设置
页和保存账户弹窗都会随 DPI 缩放。

## 隐私与安全

- TOTP 计算、二维码解析和截图识别都在本机完成。
- 未点击“保存账户”的密钥只保留在当前进程内存中。
- 本地账户库使用 Windows DPAPI CurrentUser 加密。
- 启用同步时，账户写入 Windows Credential Locker，由 Windows 负责凭据漫游。
- 应用没有自建服务器、遥测、广告、崩溃上传或分析 SDK。
- 验证码仅在用户主动复制时写入剪贴板，程序不会自动清理剪贴板历史。

更完整的安全边界见 [SECURITY.md](SECURITY.md)。

## 源码构建

要求：Windows 10/11、PowerShell 5.1 或更高版本，以及系统自带的 .NET Framework C#
编译器和 Windows Runtime 元数据。

```powershell
git clone https://github.com/miao8818/Portable2FA.git
cd Portable2FA
.\build.ps1 -OutputDirectory .
```

构建脚本会读取 `version.json`，生成多尺寸应用/托盘图标，把 ZXing.Net 嵌入单个 EXE，
编译程序并执行 27 项 RFC 6238、Base32、URI、账户序列化与二维码回归检查。

## 版本规则

`version.json` 是版本号和更新时间的唯一数据源。每次对外发布功能、修复或文档更新时运行：

```powershell
.\update-version.ps1 -Part patch   # 也可使用 minor 或 major
```

随后更新 `CHANGELOG.md`、重新构建、创建对应 Git 标签和 GitHub Release。版本号和更新时间
也会显示在软件界面右上角。详细流程见 [CONTRIBUTING.md](CONTRIBUTING.md)。

## 项目文件说明

| 文件或目录 | 作用 |
| --- | --- |
| `Program.cs` | 程序入口、程序集信息、单实例互斥、二次启动唤醒和嵌入依赖加载。 |
| `MainForm.cs` | 主窗口、账户列表、验证码刷新、QR 导入、全局快捷键、同步和托盘生命周期。 |
| `Controls.cs` | 圆角面板、按钮、输入图标和倒计时环等自绘控件。 |
| `Totp.cs` | Base32 解码、`otpauth://` 解析和 RFC 6238 TOTP 实现。 |
| `QrCodeDecoder.cs` | 使用 ZXing.Net 识别二维码并校验 2FA 内容。 |
| `ScreenCaptureForm.cs` | 跨屏幕区域截图、选区交互和截图结果返回。 |
| `SavedAccount.cs` | 已保存账户模型、标签/服务/账户字段和同步序列化。 |
| `VaultStore.cs` | 使用 DPAPI CurrentUser 加密和读取本地账户库。 |
| `WindowsCredentialSync.cs` | Windows Credential Locker 可用性检测、推送、拉取和删除合并。 |
| `AppSettings.cs` | 快捷键、同步和开机启动设置的本地持久化。 |
| `AccountDialog.cs` | 保存/编辑账户时的标签、服务名称和账户弹窗。 |
| `SettingsForm.cs` | 同步、快捷键和开机启动设置页面。 |
| `HotkeyTextBox.cs` | 捕获并规范化用户自定义组合键。 |
| `TestHarness.cs` | 27 项 TOTP、Base32、URI、账户和二维码回归检查。 |
| `IconMaker.cs` | 确定性生成应用 ICO、托盘 ICO 和预览 PNG。 |
| `app.manifest` | Windows 兼容性、权限和 Per-Monitor V2 DPI 配置。 |
| `build.ps1` | 读取版本、生成资源、嵌入依赖、编译 EXE 并运行测试。 |
| `update-version.ps1` | 递增语义化版本并写入当前时区更新时间。 |
| `version.json` | 当前版本号与 ISO 8601 更新时间的唯一数据源。 |
| `ThirdParty/ZXing.Net/` | 固定版本的 ZXing.Net DLL、许可证和来源说明。 |
| `THIRD_PARTY_NOTICES.md` | 第三方组件版本、用途、许可证和校验值。 |
| `Resources/` | 应用图标、托盘图标、图标预览和 README 功能截图。 |
| `使用说明.md` | 面向普通用户的完整中文操作说明。 |
| `CHANGELOG.md` | 按版本记录新增、变更和修复。 |
| `CONTRIBUTING.md` | 开发约定、验证要求和版本发布流程。 |
| `SECURITY.md` | 密钥存储、同步、剪贴板和漏洞报告边界。 |
| `LICENSE` | MIT 开源许可证。 |
| `.gitattributes` | Git 行尾和二进制资源规则。 |
| `.gitignore` | 排除构建、测试和本地发布产物。 |

## 第三方组件

二维码识别使用 [ZXing.Net 0.16.11](https://www.nuget.org/packages/ZXing.Net/0.16.11)，
许可证为 Apache-2.0。DLL 固定在仓库中并嵌入最终单文件 EXE，详见
[THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)。

## 完整开源声明

本项目采用 [MIT License](LICENSE) 完整开源，不存在未公开的业务模块、付费模块、远程
服务端或私有构建步骤。GitHub Release 中的 EXE 由本仓库源码和 `build.ps1` 生成。

本项目认可并友链 [LINUX DO](https://linux.do/) 社区，并遵循社区的
[开源推广要求](https://linux.do/t/topic/1776670)。

## 参与贡献

Issue 和 Pull Request 均可提交。修改 TOTP、账户合并或二维码逻辑时应同步增加测试，
UI 修改需验证 100%、125% 和 150% DPI。其他约定见 [CONTRIBUTING.md](CONTRIBUTING.md)。

## 许可证

[MIT License](LICENSE) © 2026 miao8818
