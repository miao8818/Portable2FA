# 安全说明

## 密钥处理边界

Portable 2FA 在本地进程中解析二维码并计算 TOTP：

- 未保存的密钥只存在当前进程内存中，退出程序后随进程释放。
- 只有用户点击“保存账户”并确认信息后，密钥才会写入本地账户库。
- `%LOCALAPPDATA%\Portable2FA\vault.dat` 整体使用 Windows DPAPI CurrentUser 加密，
  解密依赖保存它的 Windows 用户上下文。
- 启用 Windows 同步后，账户会写入 Windows Credential Locker，由 Windows/Microsoft
  账户的凭据漫游能力负责设备间同步。
- 应用没有自建服务端、遥测、日志上传、广告或分析 SDK。

DPAPI 用于静态文件保护，不用于跨设备同步。Windows Credential Locker 的漫游行为由
系统版本、Microsoft 账户状态和设备/组织策略决定；Windows Hello 也不是同步机制。

## 截图、二维码和剪贴板

- 二维码图片和屏幕选区只在本机进程内解码，不会由应用自动保存或上传。
- 验证码仅在用户主动点击复制时写入剪贴板。
- 工具不会自动清理系统剪贴板或剪贴板历史；高敏感环境应按自身策略清理。
- 为避免泄露，请勿在 Issue、截图或漏洞报告中包含真实 2FA 密钥、二维码或验证码。

## 本地攻击边界

账户库加密不能抵御已经以同一 Windows 用户身份执行并能调用 DPAPI 的恶意程序，也不能
替代磁盘加密、系统补丁、锁屏和恶意软件防护。启用 Credential Locker 同步意味着密钥
会进入用户选择的 Windows 凭据漫游边界。

## 第三方组件

二维码识别使用 ZXing.Net 0.16.11（Apache-2.0）。固定 DLL 的来源、许可证和 SHA-256
见 [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)。

## 报告漏洞

请通过 GitHub 仓库的 Security Advisory 私密报告安全问题。报告中请说明受影响版本、
复现步骤、预期行为和实际行为，并使用合成测试密钥代替真实账户数据。
