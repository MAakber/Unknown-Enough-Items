# UEI — Unknown Enough Items @VERSION@

Casualties Unknown 的物品、液体与配方查询插件，支持中文和 English。
An item, liquid and recipe browser for Casualties Unknown.

## 安装 / Install

- 先安装 BepInEx 5；它是必需依赖。
- Gale 用户可直接导入这个 ZIP。
- 手动安装时，把 ZIP 中的 `BepInEx` 文件夹合并到游戏目录。
- 如果已有旧版 UEI DLL，请先关闭游戏，并只保留一份 UEI 插件，避免重复加载。

Install BepInEx 5 first. Import this ZIP into Gale, or merge its `BepInEx` folder
into the game directory. Close the game before replacing an older UEI DLL; keep
only one copy of the plugin.

## 兼容范围 / Compatibility

Together、CUCoreLib、RshLib 和 SexMod 都是可选适配，并非必需依赖。
Together 客户端的原版物品领取通过服务器命令处理；自定义模组物品仍需由房主生成。
自动 `cuspawn` 尚未启用；双端同步与保存/重连仍需实机验证。

Together, CUCoreLib, RshLib and SexMod are optional. Vanilla item requests on
Together clients go through the server. Custom mod items must currently be
spawned by the host. Automatic `cuspawn` is not enabled. Two-peer synchronization
and save/reconnect validation remain pending.

本包只包含 UEI，不包含游戏程序集、BepInEx、联机模组或临时测试插件。
This package contains UEI only, not game assemblies, BepInEx, multiplayer mods
or the disposable in-game test fixture.
