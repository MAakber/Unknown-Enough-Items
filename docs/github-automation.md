# GitHub 自动编译与发布

## 触发规则

| 操作 | 自动执行 |
| --- | --- |
| 提交 PR | 版本一致性、打包安全检查、现有生产代码回归检查；不读取私有构建密钥 |
| 向本仓库分支推送提交 | 上述检查，然后使用真实游戏程序集编译并上传可下载的 Actions 构建产物 |
| 在 Actions 中手动运行 | 与分支构建相同，不创建 Release |
| 推送 `v1.2.5` 这样的版本标签 | 全部检查和编译通过后，创建并发布对应 GitHub Release |

版本号本身严格为 `x.x.x`。标签采用 `v` 前缀，必须与 `UEI.csproj` 和
`Plugin.cs` 拼接后的版本完全相同。标签指向的提交必须已进入 `main`。
普通提交不自动改版本，也不覆盖已有 Release。

## 维护者发布步骤

1. 更新 `UEI.csproj` 的 `Version`、`Plugin.cs` 的版本常量和 README 版本标识。
   同时重写 `packaging/RELEASE-NOTES.md`，记录本版实际新增、修复、已知限制与安装方式，
   并更新首行的版本标记。版本不匹配或说明为空会阻止检查通过。
2. 提交并合入 `main`，等待 `UEI CI and Release` 的检查和构建通过。
3. 在对应提交打标签并推送，例如：

```powershell
git tag -a v1.2.5 -m "UEI 1.2.5"
git push origin v1.2.5
```

自动化使用对应版本的更新日志作为发布正文，先创建草稿并上传全部文件，上传成功后才公开发布。
不会把通用安装模板或自动生成的 PR 列表作为更新日志，也不会覆盖同名 Release。
如果上传阶段失败，可能留下草稿；先检查失败日志和草稿，再决定修复草稿或发布新版本。
不要移动已经发布的标签。

Release 包含 `UEI-版本号.zip`、`UEI.dll`、`SHA256SUMS.txt` 和 `build-info.json`。
ZIP 含 Gale 可识别的 manifest、256×256 图标、README、CHANGELOG、许可证和一份 UEI DLL。
CHANGELOG 从当前版本的 `packaging/RELEASE-NOTES.md` 生成，避免维护两份更新内容。
打包检查会核对图标与源码文件一致，并核对 CHANGELOG 与当前版本的更新说明一致。
不分发游戏 DLL、BepInEx、Together 或临时测试插件。

## 图标和 Thunderstore 同步

`packaging/icon.png` 是打包使用的 256×256 PNG；`packaging/icon-D-1024.png`
是 README 展示与后续编辑使用的高清图。README 功能截图仍位于 `image/README/`。
草稿、旧封面备份、校验包和编译产物保存在被 Git 忽略的 `artifacts/` 中。

GitHub 源码图标在提交推送后更新，已发布 ZIP 不会自动随源码修改。
Thunderstore 的已发布版本不可修改图标；只有 README 与 CHANGELOG 可原地编辑。
更新图标需要提高版本号、用同一团队上传新包，并刷新 Gale 模组列表核对实际显示。
规则见 [Thunderstore 更新说明](https://wiki.thunderstore.io/mods/updating-a-package)。

自动 GitHub Release 的标签与源码版本必须一致，仍按上面的维护者步骤发布。

## 编译引用的来源与更新

云端使用 `.github/build-references/manifest.json` 锁定的十个真实编译引用。
它们来自本机安装的游戏和 BepInEx，经过 GnuPG AES-256 加密后存入仓库；
解密密钥只在仓库的 `UEI_BUILD_REFERENCES_KEY` Actions Secret 中。
编译前会检查每个文件的大小与 SHA-256。明文只解密到 runner 临时目录，
不上传为产物、不写入缓存；PR 检查不获取密钥。

首次配置或游戏 API 更新时，维护者在已登录 `gh` 的本机运行：

```powershell
python scripts/ci.py prepare-references --game-root "游戏目录" --gpg "gpg.exe 的路径" --provision-secret
```

该命令读取十个指定引用、生成随机密钥、验证加解密往返，并通过 `gh secret set`
保存密钥；不会在命令行或输出中显示密钥。密钥未存入本机文件。
然后及时提交生成的 `.gpg` 和 `manifest.json`，推送并检查云端构建。
更新密钥与加密文件应作为同一次维护操作，旧密钥不能解密新文件。

贡献者无需密钥即可运行：

```powershell
python scripts/ci.py version
python -m unittest discover -s tests/automation -v
dotnet run --project tests/UEI.RegressionTests.csproj -c Release -- "$PWD"
```

有匹配游戏引用的本机可额外执行完整构建与打包：

```powershell
python scripts/ci.py build --game-root "游戏目录" --output artifacts/build
python scripts/ci.py package --dll artifacts/build/UEI.dll --output artifacts/release
```

需要 .NET SDK 10、Python 3.10+；只有引用准备/解密步骤需要 GnuPG。
构建脚本写到独立输出，不自动安装到游戏或启动游戏。

## 自动化验证的边界

云端编译使用真实引用，不用测试桩代替游戏 API。回归检查和包内容校验
不等于 Unity 实机、双端联机或存档重连验证。现有客户端模组物品限制和
自动 `cuspawn` 尚未启用的状态保持不变。发布说明会明确保留这些限制。
