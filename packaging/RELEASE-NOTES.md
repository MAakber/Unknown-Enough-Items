<!-- version: 1.2.4 -->

本次更新适配游戏正式版与 Together 联机，补充第三方物品模组兼容，并修复作弊模式的权限判断与点击行为。

## 新增与适配

- 适配游戏正式版，更新 Together（CasualtiesMP）联机兼容逻辑。
- 新增 CUCoreLib、SexMod 物品适配，整合 RshLib 支持，完善模组物品、图标与配方的读取。
- 作弊操作栏新增一键复活按钮。
- 提供带封面和安装说明的 ZIP 安装包，可直接导入 Gale。

## 修复与优化

- 修复启用作弊模式或左键领取物品时，右侧配方详情被意外打开的问题；右键查看用途的功能保留。
- 修正 Together 客户端的作弊权限判断，解决已有服务器管理员权限仍显示“无权限”的问题。
- Together 客户端领取原版物品改为向服务器提交请求，并区分“请求已发送”与“发放完成”的提示。
- 缓存可选模组检测结果，减少重复扫描；优化物品目录和界面刷新，减少同一帧内的重复工作。
- 调整本地化读取，避免缺失翻译查询修改联机语言数据；补充界面对象销毁后的检查与重建，保留主菜单恢复机制。

## 已知限制

- Together 客户端暂不支持直接领取模组物品，请由房主生成；本版本未启用自动 `cuspawn`。
- 双端物品同步、权限边界和保存/重连仍待进一步实机验证。

## 下载与安装

- **Gale 用户：** 下载 [UEI-1.2.4.zip](https://github.com/MAakber/Unknown-Enough-Items/releases/download/v1.2.4/UEI-1.2.4.zip) 并导入档案。
- **手动安装：** 先安装 BepInEx 5，再将 `UEI.dll` 放入 `BepInEx/plugins/UEI/`。升级前关闭游戏，并确保插件目录只保留一份 UEI DLL。
- Together、CUCoreLib、RshLib 和 SexMod 均为可选适配，无需全部安装。

感谢 @dfgger433 在 [#2](https://github.com/MAakber/Unknown-Enough-Items/pull/2)、[#3](https://github.com/MAakber/Unknown-Enough-Items/pull/3)、[#4](https://github.com/MAakber/Unknown-Enough-Items/pull/4) 中提供的适配贡献。

**完整变更：** [V1.1.10 → v1.2.4](https://github.com/MAakber/Unknown-Enough-Items/compare/V1.1.10...v1.2.4)
