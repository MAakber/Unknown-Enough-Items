<!-- version: 1.2.5 -->

本版更新 UEI 的平台图标、安装说明与打包内容，功能沿用 1.2.4。

## 图标与文档

- 使用 1:1 洞穴荧光款 UEI 图标，提供 256×256 平台图标与 1024×1024 高清图。
- GitHub README 使用同款图标，完善 Gale 安装入口和手动安装说明。
- 自动安装包加入 CHANGELOG.md，并核对包内图标与更新说明，减少不同发布渠道的内容差异。
- 将未使用的旧封面移至本地备份；仓库继续保留源码、构建配置、测试与使用文档。

## 兼容范围

- 沿用 1.2.4 的游戏正式版与 Together、CUCoreLib、RshLib、SexMod 兼容逻辑。
- Together 客户端的模组物品仍需房主生成；自动 cuspawn 尚未启用。
- 双端同步、权限边界以及保存/重连仍需进一步实机验证。

## 安装

在 Gale 的 Casualties: Unknown 模组列表搜索 UEI 并更新，或从 GitHub Release 下载 ZIP 后导入。
手动安装时先安装 BepInEx 5，再将 UEI.dll 放入 BepInEx/plugins/UEI/。
升级前关闭游戏，并只保留一份 UEI DLL。

Version 1.2.5 updates the square cave-glow icon, installation documentation and
packaged changelog. Gameplay behavior and multiplayer limitations remain those
of 1.2.4. Custom mod items on Together clients still require host spawning;
automatic cuspawn is disabled, and two-peer/save/reconnect validation is pending.
