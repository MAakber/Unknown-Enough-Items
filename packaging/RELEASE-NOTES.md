## 下载与安装 / Download and install

- 推荐下载 `UEI-版本号.zip`，直接导入 Gale，或解压合并到已安装 BepInEx 的游戏目录。
- `UEI.dll` 是供手动替换使用的同一份插件；请勿与旧版 UEI DLL 重复安装。
- `SHA256SUMS.txt` 用于校验文件；`build-info.json` 记录源码提交和构建依赖清单摘要。

Use the versioned ZIP for Gale or manual installation. The standalone DLL is
the same plugin. Checksums and source-commit provenance are included.

## 验证范围 / Validation scope

此包已通过自动编译、回归检查和发布包内容校验。自动化检查不代表实际联机或读档测试。
当前 Together 客户端的模组物品仍需房主生成，自动 `cuspawn` 尚未启用；
双端同步、权限实测和保存/重连验证仍待完成。

Automated compilation, regression checks and package validation do not establish
in-game multiplayer or save/load correctness. Host spawning is still required
for custom items on Together clients; automatic `cuspawn` is not enabled.

Only UEI is distributed. Game assemblies, optional mods and the disposable
test fixture are excluded.
