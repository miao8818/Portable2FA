# 参与贡献

感谢对 Portable 2FA 的改进。

## 开发要求

- 保持工具完全离线，不加入网络、遥测或账户依赖。
- 不把 2FA 密钥写入磁盘、注册表或日志。
- TOTP 核心修改必须增加或更新 `TestHarness.cs` 中的检查。
- UI 修改需要在 100%、125% 和 150% DPI 下确认没有遮挡或文字截断。

## 提交前检查

```powershell
.\build.ps1 -OutputDirectory .
```

构建输出必须包含 `PASS`，且程序能正常启动、复制验证码、关闭进托盘并从托盘退出。

## 强制版本流程

每次对外发布的代码、功能、修复或文档更新都必须更新版本号和更新时间：

```powershell
.\update-version.ps1 -Part patch
```

- `patch`：修复或文档更新。
- `minor`：向后兼容的新功能。
- `major`：不兼容变更。

运行脚本后必须：

1. 检查 `version.json` 中的版本和 ISO 8601 时间。
2. 在 `CHANGELOG.md` 顶部添加对应版本记录。
3. 运行 `build.ps1` 并通过全部测试。
4. 提交代码，创建 `v版本号` Git 标签和 GitHub Release。

## Pull Request

请保持一次 PR 只处理一个明确主题，并在说明中列出行为变化和验证结果。
