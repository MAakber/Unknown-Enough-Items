using System;
using System.Collections.Generic;
using UnityEngine;

internal static class UeiI18n
{
    private static readonly Dictionary<string, string> En = new(StringComparer.OrdinalIgnoreCase)
    {
        ["category.all"] = "All",
        ["category.favorites"] = "Favorites",
        ["category.liquid"] = "Liquid",
        ["category.entity"] = "Entities",
        ["entity.traderVariant"] = "{0} {1}",
        ["entity.noDescription"] = "No description available.",
        ["placement.started"] = "Placing {0}",
        ["placement.hint"] = "{0}\nLeft-click to spawn; right-click / Esc to cancel",
        ["placement.blocked"] = "{0}\nMove the pointer into the game world",
        ["placement.noAppearance"] = "{0}\nNo visible preview; left-click to spawn, right-click / Esc to cancel",
        ["placement.cancelled"] = "Spawn cancelled",
        ["placement.interrupted"] = "Spawn cancelled: game state or permission changed",
        ["category.other"] = "Other",
        ["itemCategory.medical"] = "Medical",
        ["itemCategory.drug"] = "Drugs",
        ["itemCategory.container"] = "Containers",
        ["itemCategory.food"] = "Food",
        ["itemCategory.water"] = "Water",
        ["itemCategory.tool"] = "Tools",
        ["itemCategory.trash"] = "Trash",
        ["itemCategory.utility"] = "Utility",
        ["itemCategory.custom"] = "Custom",
        ["itemCategory.unobtainable"] = "Unobtainable",
        ["itemCategory.other"] = "Other",
        ["recipeCategory.materials"] = "Materials",
        ["recipeCategory.tools"] = "Tools",
        ["recipeCategory.medicine"] = "Medicine",
        ["recipeCategory.utilities"] = "Utilities",
        ["recipeCategory.food"] = "Food",
        ["quality.foliage"] = "Foliage",
        ["quality.nails"] = "Nails",
        ["quality.water"] = "Water",
        ["quality.hammering"] = "Hammering",
        ["quality.cutting"] = "Cutting",
        ["quality.rippable"] = "Rippable",
        ["quality.disinfectant"] = "Antiseptic",
        ["quality.blood"] = "Blood",
        ["quality.opiate"] = "Opiate",
        ["quality.dressing"] = "Dressing",
        ["quality.produce"] = "Produce",
        ["quality.meat"] = "Meat",
        ["quality.firestarter"] = "Firestarter",
        ["quality.heatsource"] = "Heat source",
        ["quality.flammable"] = "Flammable",
        ["quality.flour"] = "Flour",
        ["quality.fat"] = "Fat",
        ["quality.condiment"] = "Condiments",
        ["search.placeholder"] = "Search...",
        ["loading.items"] = "Waiting for item data...",
        ["detail.source"] = "Source",
        ["detail.uses"] = "Uses",
        ["detail.mode.source"] = "SRC",
        ["detail.mode.uses"] = "USE",
        ["detail.acquisition"] = "Acquisition",
        ["detail.noAcquisition"] = "No acquisition info",
        ["acquisition.supplement"] = "Description supplement:",
        ["acquisition.loading"] = "Looking for item sources; more may appear.",
        ["acquisition.incomplete"] = "Some sources could not be read; this list may be incomplete.",
        ["acquisition.trade"] = "Trade: traders may stock this item",
        ["acquisition.weaponTrade"] = "Trade: some traders may stock this weapon or ammunition",
        ["acquisition.container"] = "Liquid in {0} when found (default {1} mL; actual amount depends on spawn condition)",
        ["acquisition.context"] = "{0} (inside {1})",
        ["acquisition.break"] = "Break",
        ["acquisition.defeat"] = "Defeat",
        ["acquisition.condition"] = "condition {0}–{1}",
        ["acquisition.conditionSingle"] = "condition {0}",
        ["acquisition.conditionUnknown"] = "condition unknown",
        ["acquisition.always"] = "{0} {1}: guaranteed drop ×{2} ({3})",
        ["acquisition.fixed"] = "{0} {1}: {2} chance per independent drop (drop chances: {3}; {4})",
        ["acquisition.pool"] = "{0} {1}: {3} chance to get this item at least once (random drops: {2}; {4})",
        ["acquisition.poolUnknown"] = "{0} {1}: may drop this item at random ({2} draws expected; chance unknown)",
        ["acquisition.card.trader"] = "Trader",
        ["acquisition.card.independent"] = "Independent drop",
        ["acquisition.card.guaranteed"] = "Guaranteed drop",
        ["acquisition.card.randomPool"] = "Random drops",
        ["acquisition.card.initialContents"] = "Liquid inside",
        ["acquisition.card.trade"] = "Trade",
        ["acquisition.card.method"] = "{0} · {1}",
        ["acquisition.card.randomStock"] = "Traders may stock this item",
        ["acquisition.card.weaponStock"] = "Some traders may stock this weapon or ammunition",
        ["acquisition.card.amountVaries"] = "Actual amount varies with spawn condition",
        ["acquisition.card.guaranteedCount"] = "Guaranteed ×{0}",
        ["acquisition.card.fixedRolls"] = "Drop chances: {0} · {1} each",
        ["acquisition.card.poolRolls"] = "Random drops: {0} · Item chance: {1}",
        ["acquisition.card.poolUnknown"] = "Expected random drops: {0} · Chance unknown",
        ["acquisition.card.defaultAmount"] = "Default {0} mL",
        ["acquisition.card.mergedSources"] = "Found in these places (same drops):",
        ["acquisition.card.standalone"] = "{0} (on its own)",
        ["acquisition.card.containerIcon"] = "Jar",
        ["acquisition.card.entityIcon"] = "Entity",
        ["acquisition.card.buildingIcon"] = "Source",
        ["acquisition.card.tradeIcon"] = "Trade",
        ["detail.supplement"] = "Notes",
        ["detail.craftingRecipes"] = "Crafting recipes",
        ["detail.empty"] = "No crafting recipes",
        ["detail.emptyUses"] = "No usage recipes",
        ["detail.noIngredients"] = "No ingredients",
        ["status.usedIn"] = "Used in",
        ["status.source"] = "Source",
        ["status.liquidSource"] = "Liquid source",
        ["status.liquidUse"] = "Liquid use",
        ["status.detailOnly"] = "Detail only",
        ["status.recipes"] = "Recipes",
        ["status.recipeLinkFailed"] = "Recipe link failed",
        ["status.jumpRecipe"] = "Jumped",
        ["status.jumpNoRecipe"] = "No recipe to jump",
        ["status.jumpFailed"] = "Jump failed",
        ["status.pinRecipe"] = "Pinned",
        ["status.unpinRecipe"] = "Unpinned",
        ["status.pinFailed"] = "Pin failed",
        ["status.cheatSpawned"] = "Gave {0} {1}",
        ["status.cheatSpawnedNearby"] = "Spawned nearby; make room and pick it up",
        ["status.cheatRequestSent"] = "Give requested",
        ["status.cheatHostRequired"] = "Mod item: ask the host to spawn it",
        ["status.cheatIllegal"] = "Not a legal item",
        ["status.cheatFailed"] = "Give failed",
        ["status.cheatEntitySpawned"] = "Spawned {0} at the selected position",
        ["status.cheatSpawnRequestSent"] = "Spawn requested from server",
        ["status.cheatEntityFailed"] = "Spawn failed",
        ["status.cheatLiquidGiven"] = "Received a full temporary bottle (expires in 5 minutes)",
        ["status.reviveDone"] = "Revived",
        ["status.reviveRequestSent"] = "Revive requested",
        ["status.reviveFailed"] = "Revive failed",
        ["button.settings.short"] = "S",
        ["button.settings"] = "Settings",
        ["button.settings.desc"] = "Open UEI settings",
        ["button.revive"] = "Revive",
        ["button.revive.desc"] = "Fully heal and revive the player character",
        ["button.closeDetail"] = "Close",
        ["button.closeDetail.desc"] = "Close UEI detail panel",
        ["button.jumpRecipe"] = "Jump",
        ["button.jumpRecipe.desc"] = "Open the original crafting panel at this recipe",
        ["button.pinRecipe"] = "Pin recipe",
        ["button.pinRecipe.desc"] = "Pin or unpin this recipe with the original crafting panel",
        ["settings.title"] = "UEI Settings",
        ["settings.language"] = "Language",
        ["settings.language.auto"] = "Auto",
        ["settings.language.zh"] = "Chinese",
        ["settings.language.en"] = "English",
        ["settings.position"] = "Position",
        ["settings.position.left"] = "Left",
        ["settings.position.right"] = "Right",
        ["settings.scale"] = "Scale",
        ["settings.detailRows"] = "Cards per page",
        ["settings.detailRows.value"] = "{0}",
        ["settings.detailRows.desc"] = "Click to cycle 1–4 rows (default 2) for acquisition, crafting and uses. Denser pages scale card contents to fit.",
        ["settings.apply"] = "Apply",
        ["settings.cheat"] = "Cheat",
        ["settings.on"] = "On",
        ["settings.off"] = "Off",
        ["settings.noCommandAccess"] = "No access",
        ["settings.about"] = "About",
        ["settings.aboutText"] = "UEI - Unknown Enough Items\nVersion: {0}\nGUID: {1}\nAuthor: {2}",
        ["recipe.visible"] = "Visible",
        ["recipe.locked"] = "Locked",
        ["recipe.made"] = "Made",
        ["recipe.new"] = "New",
        ["recipe.int"] = "INT",
        ["recipe.fallback"] = "Recipe",
        ["ingredient.liquid"] = "Liquid",
        ["ingredient.any"] = "Any",
        ["ingredient.anyShort"] = "Any",
        ["ingredient.liquidShort"] = "Liq",
        ["ingredient.anyItemTitle"] = "Any item: {0}",
        ["ingredient.anyLiquidTitle"] = "Any liquid: {0}",
        ["ingredient.requirement"] = "Requirement",
        ["ingredient.quality"] = "Quality",
        ["ingredient.minimumCondition"] = "Minimum condition",
        ["ingredient.matchingItems"] = "Matching items",
        ["ingredient.matchingLiquids"] = "Matching liquids",
        ["ingredient.noMatches"] = "No matching entries",
        ["ingredient.moreMatches"] = "...and {0} more",
        ["ingredient.needsMl"] = "{0} mL needed",
        ["tooltip.id"] = "ID",
        ["tooltip.value"] = "Value",
        ["tooltip.injectable"] = "Injectable",
        ["tooltip.healthUse"] = "Health use",
        ["tooltip.weight"] = "Weight",
        ["selection.noResults"] = "No results",
        ["selection.entries"] = "{0} entries",
    };

    private static readonly Dictionary<string, string> Zh = new(StringComparer.OrdinalIgnoreCase)
    {
        ["category.all"] = "全部",
        ["category.favorites"] = "收藏",
        ["category.liquid"] = "液体",
        ["category.entity"] = "实体",
        ["entity.traderVariant"] = "{0} {1}",
        ["entity.noDescription"] = "暂无描述",
        ["placement.started"] = "正在定位：{0}",
        ["placement.hint"] = "{0}\n左键生成；右键 / Esc 取消",
        ["placement.blocked"] = "{0}\n请将鼠标移到游戏场景中",
        ["placement.noAppearance"] = "{0}\n暂无可预览外观；左键生成，右键 / Esc 取消",
        ["placement.cancelled"] = "已取消生成",
        ["placement.interrupted"] = "已取消生成：游戏状态或权限已改变",
        ["category.other"] = "其他",
        ["itemCategory.medical"] = "医疗用品",
        ["itemCategory.drug"] = "药物",
        ["itemCategory.container"] = "容器",
        ["itemCategory.food"] = "食物",
        ["itemCategory.water"] = "饮料",
        ["itemCategory.tool"] = "工具",
        ["itemCategory.trash"] = "垃圾",
        ["itemCategory.utility"] = "实用物品",
        ["itemCategory.custom"] = "自定义",
        ["itemCategory.unobtainable"] = "不可获得",
        ["itemCategory.other"] = "其他",
        ["recipeCategory.materials"] = "材料",
        ["recipeCategory.tools"] = "工具",
        ["recipeCategory.medicine"] = "药物",
        ["recipeCategory.utilities"] = "杂项",
        ["recipeCategory.food"] = "食物",
        ["quality.foliage"] = "纤维",
        ["quality.nails"] = "铁钉",
        ["quality.water"] = "液体",
        ["quality.hammering"] = "可捶打",
        ["quality.cutting"] = "可切割",
        ["quality.rippable"] = "可撕碎",
        ["quality.disinfectant"] = "消毒",
        ["quality.blood"] = "血液",
        ["quality.opiate"] = "阿片",
        ["quality.dressing"] = "敷料",
        ["quality.produce"] = "农产品",
        ["quality.meat"] = "肉类",
        ["quality.firestarter"] = "火种",
        ["quality.heatsource"] = "热源",
        ["quality.flammable"] = "易燃的",
        ["quality.flour"] = "面粉",
        ["quality.fat"] = "脂肪",
        ["quality.condiment"] = "调味料",
        ["search.placeholder"] = "搜索...",
        ["loading.items"] = "等待物品数据...",
        ["detail.source"] = "来源",
        ["detail.uses"] = "用途",
        ["detail.mode.source"] = "源",
        ["detail.mode.uses"] = "用",
        ["detail.acquisition"] = "获取途径",
        ["detail.noAcquisition"] = "暂无获取信息",
        ["acquisition.supplement"] = "描述补充：",
        ["acquisition.loading"] = "正在查找获取途径，可能还会发现更多。",
        ["acquisition.incomplete"] = "部分获取信息读取失败，可能还有其他途径。",
        ["acquisition.trade"] = "交易：商人有时出售",
        ["acquisition.weaponTrade"] = "交易：部分商人有时出售这类武器或弹药",
        ["acquisition.container"] = "获得 {0} 时自带的液体（默认 {1} mL，实际液量取决于生成时的状态）",
        ["acquisition.context"] = "{0}（{1} 内）",
        ["acquisition.break"] = "破坏",
        ["acquisition.defeat"] = "击败",
        ["acquisition.condition"] = "耐久 {0}–{1}",
        ["acquisition.conditionSingle"] = "耐久 {0}",
        ["acquisition.conditionUnknown"] = "耐久未知",
        ["acquisition.always"] = "{0}{1}：必定掉落 ×{2}（{3}）",
        ["acquisition.fixed"] = "{0}{1}：有 {3} 次独立掉落机会，每次概率为 {2}（{4}）",
        ["acquisition.pool"] = "{0}{1}：有 {3} 的概率获得该物品（随机掉落 {2} 次，{4}）",
        ["acquisition.poolUnknown"] = "{0}{1}：可能随机掉落（预计抽取 {2} 次，概率未知）",
        ["acquisition.card.trader"] = "商人",
        ["acquisition.card.independent"] = "独立掉落",
        ["acquisition.card.guaranteed"] = "必定掉落",
        ["acquisition.card.randomPool"] = "随机掉落",
        ["acquisition.card.initialContents"] = "自带液体",
        ["acquisition.card.trade"] = "交易",
        ["acquisition.card.method"] = "{0} · {1}",
        ["acquisition.card.randomStock"] = "商人有时出售",
        ["acquisition.card.weaponStock"] = "部分商人有时出售这类武器或弹药",
        ["acquisition.card.amountVaries"] = "实际液量取决于生成时的状态",
        ["acquisition.card.guaranteedCount"] = "必定掉落 ×{0}",
        ["acquisition.card.fixedRolls"] = "掉落机会 {0} 次 · 每次概率 {1}",
        ["acquisition.card.poolRolls"] = "随机掉落 {0} 次 · 获得概率 {1}",
        ["acquisition.card.poolUnknown"] = "预计随机掉落 {0} 次 · 概率未知",
        ["acquisition.card.defaultAmount"] = "默认 {0} mL",
        ["acquisition.card.mergedSources"] = "出现位置（掉落相同）：",
        ["acquisition.card.standalone"] = "{0}（单独出现）",
        ["acquisition.card.containerIcon"] = "容器",
        ["acquisition.card.entityIcon"] = "实体",
        ["acquisition.card.buildingIcon"] = "来源",
        ["acquisition.card.tradeIcon"] = "交易",
        ["detail.supplement"] = "描述补充",
        ["detail.craftingRecipes"] = "制作配方",
        ["detail.empty"] = "无制作配方",
        ["detail.emptyUses"] = "无用途配方",
        ["detail.noIngredients"] = "无材料",
        ["status.usedIn"] = "用途",
        ["status.source"] = "来源",
        ["status.liquidSource"] = "液体来源",
        ["status.liquidUse"] = "液体用途",
        ["status.detailOnly"] = "仅详情",
        ["status.recipes"] = "配方",
        ["status.recipeLinkFailed"] = "配方联动失败",
        ["status.jumpRecipe"] = "已跳转",
        ["status.jumpNoRecipe"] = "没有可跳转配方",
        ["status.jumpFailed"] = "跳转失败",
        ["status.pinRecipe"] = "已收藏配方",
        ["status.unpinRecipe"] = "已取消收藏",
        ["status.pinFailed"] = "收藏失败",
        ["status.cheatSpawned"] = "已发放 {0} 个 {1}",
        ["status.cheatSpawnedNearby"] = "已生成在脚边，请腾出空位后拾取",
        ["status.cheatRequestSent"] = "已请求服务器发放",
        ["status.cheatHostRequired"] = "模组物品需由房主生成",
        ["status.cheatIllegal"] = "不是合法物品",
        ["status.cheatFailed"] = "发放失败",
        ["status.cheatEntitySpawned"] = "已在所选位置生成 {0}",
        ["status.cheatSpawnRequestSent"] = "已请求服务器生成",
        ["status.cheatEntityFailed"] = "生成失败",
        ["status.cheatLiquidGiven"] = "已领取装满液体的临时瓶（5 分钟后消失）",
        ["status.reviveDone"] = "已复活",
        ["status.reviveRequestSent"] = "已请求服务器复活",
        ["status.reviveFailed"] = "复活失败",
        ["button.settings.short"] = "设",
        ["button.settings"] = "设置",
        ["button.settings.desc"] = "打开 UEI 设置",
        ["button.revive"] = "一键复活",
        ["button.revive.desc"] = "回满并复活玩家角色",
        ["button.closeDetail"] = "关闭",
        ["button.closeDetail.desc"] = "关闭 UEI 详情面板",
        ["button.jumpRecipe"] = "跳转",
        ["button.jumpRecipe.desc"] = "在原版制作面板中打开这条配方",
        ["button.pinRecipe"] = "收藏配方",
        ["button.pinRecipe.desc"] = "调用原版制作面板收藏或取消收藏这条配方",
        ["settings.title"] = "UEI 设置",
        ["settings.language"] = "语言",
        ["settings.language.auto"] = "自动",
        ["settings.language.zh"] = "中文",
        ["settings.language.en"] = "English",
        ["settings.position"] = "位置",
        ["settings.position.left"] = "左侧",
        ["settings.position.right"] = "右侧",
        ["settings.scale"] = "比例",
        ["settings.detailRows"] = "每页卡片行数",
        ["settings.detailRows.value"] = "{0} 行",
        ["settings.detailRows.desc"] = "点击循环选择 1–4 行，默认 2 行；适用于获取途径、制作配方和用途。行数较多时，卡片内容会等比缩小以适应面板。",
        ["settings.apply"] = "应用",
        ["settings.cheat"] = "作弊",
        ["settings.on"] = "开启",
        ["settings.off"] = "关闭",
        ["settings.noCommandAccess"] = "无权限",
        ["settings.about"] = "关于",
        ["settings.aboutText"] = "UEI - Unknown Enough Items\n版本：{0}\nGUID：{1}\n作者：{2}",
        ["recipe.visible"] = "可见",
        ["recipe.locked"] = "锁定",
        ["recipe.made"] = "已制作",
        ["recipe.new"] = "未制作",
        ["recipe.int"] = "智力",
        ["recipe.fallback"] = "配方",
        ["ingredient.liquid"] = "液体",
        ["ingredient.any"] = "任意",
        ["ingredient.anyShort"] = "任意",
        ["ingredient.liquidShort"] = "液",
        ["ingredient.anyItemTitle"] = "任意物品：{0}",
        ["ingredient.anyLiquidTitle"] = "任意液体：{0}",
        ["ingredient.requirement"] = "需求",
        ["ingredient.quality"] = "质量",
        ["ingredient.minimumCondition"] = "最低耐久",
        ["ingredient.matchingItems"] = "可用物品",
        ["ingredient.matchingLiquids"] = "可用液体",
        ["ingredient.noMatches"] = "没有匹配项",
        ["ingredient.moreMatches"] = "...以及另外 {0} 项",
        ["ingredient.needsMl"] = "需要 {0} mL",
        ["tooltip.id"] = "ID",
        ["tooltip.value"] = "价值",
        ["tooltip.injectable"] = "可注射",
        ["tooltip.healthUse"] = "医疗用途",
        ["tooltip.weight"] = "重量",
        ["selection.noResults"] = "无结果",
        ["selection.entries"] = "{0} 项",
    };

    public static string T(string key)
    {
        Dictionary<string, string> table = IsChinese() ? Zh : En;
        if (table.TryGetValue(key, out string? value))
        {
            return value;
        }

        return En.TryGetValue(key, out value) ? value : key;
    }

    public static string Format(string key, params object[] args)
    {
        return string.Format(T(key), args);
    }

    public static string ItemText(string localeKey, string fallback)
    {
        if (string.IsNullOrWhiteSpace(localeKey))
        {
            return fallback;
        }

        string value = SafeString(() => Locale.GetItem(localeKey), string.Empty);
        if (IsResolvedLocaleValue(value, localeKey)) return value;
        string custom = CuCoreLibHelper.GetItemText(localeKey);
        return IsResolvedLocaleValue(custom, localeKey) ? custom : fallback;
    }

    public static string OtherText(string localeKey, string fallback)
    {
        if (string.IsNullOrWhiteSpace(localeKey))
        {
            return fallback;
        }

        string value = SafeString(() => Locale.GetOther(localeKey), string.Empty);
        return IsResolvedLocaleValue(value, localeKey) ? value : fallback;
    }

    public static string LiquidText(string localeKey, string fallback)
    {
        // CUCore maps its liquid locale category into the game's other dictionary.
        return OtherText(localeKey, fallback);
    }

    public static string BuildingText(string localeKey, string fallback)
    {
        if (string.IsNullOrWhiteSpace(localeKey)) return fallback;
        string value = SafeString(() => Locale.GetBuilding(localeKey), string.Empty);
        return IsResolvedLocaleValue(value, localeKey) ? value : fallback;
    }

    public static string CategoryLabel(string categoryKey)
    {
        if (string.Equals(categoryKey, UeiCatalog.CategoryAll, StringComparison.OrdinalIgnoreCase))
        {
            return T("category.all");
        }

        if (string.Equals(categoryKey, UeiCatalog.CategoryFavorites, StringComparison.OrdinalIgnoreCase))
        {
            return T("category.favorites");
        }

        if (string.Equals(categoryKey, UeiCatalog.CategoryLiquid, StringComparison.OrdinalIgnoreCase))
        {
            return T("category.liquid");
        }

        if (string.Equals(categoryKey, UeiCatalog.CategoryEntity, StringComparison.OrdinalIgnoreCase))
        {
            return T("category.entity");
        }
        if (string.IsNullOrWhiteSpace(categoryKey))
        {
            return T("category.other");
        }

        return ItemCategoryLabel(categoryKey);
    }

    public static string ItemCategoryLabel(string categoryKey)
    {
        if (string.IsNullOrWhiteSpace(categoryKey))
        {
            return T("category.other");
        }

        string normalized = categoryKey.Trim().ToLowerInvariant();
        if (TryTableText("itemCategory." + normalized, out string value))
        {
            return value;
        }

        string localeKey = "category" + normalized;
        string localized = OtherText(localeKey, string.Empty);
        return string.IsNullOrWhiteSpace(localized) ? HumanizeKey(categoryKey) : localized;
    }

    public static string RecipeCategoryLabel(object category)
    {
        string raw = SafeString(() => category.ToString() ?? string.Empty, string.Empty);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return T("category.other");
        }

        string normalized = raw.Trim().ToLowerInvariant();
        if (TryTableText("recipeCategory." + normalized, out string value))
        {
            return value;
        }

        string localeKey = "craftingcategory" + normalized;
        string localized = OtherText(localeKey, string.Empty);
        return string.IsNullOrWhiteSpace(localized) ? HumanizeKey(raw) : localized;
    }

    public static string QualityLabel(string qualityId, string fallback)
    {
        if (string.IsNullOrWhiteSpace(qualityId))
        {
            return fallback;
        }

        string normalized = qualityId.Trim();
        if (normalized.StartsWith("cq", StringComparison.OrdinalIgnoreCase) && normalized.Length > 2)
        {
            normalized = normalized.Substring(2);
        }

        normalized = normalized.ToLowerInvariant();
        if (TryTableText("quality." + normalized, out string value))
        {
            return value;
        }

        string localized = OtherText("cq" + normalized, string.Empty);
        if (!string.IsNullOrWhiteSpace(localized))
        {
            return localized;
        }

        return string.IsNullOrWhiteSpace(fallback) ? qualityId : fallback;
    }

    public static bool IsChinese()
    {
        string mode = SafeString(() => UeiPlugin.LanguageMode.Value, "auto").Trim();
        if (IsExplicitChinese(mode))
        {
            return true;
        }

        if (IsExplicitEnglish(mode))
        {
            return false;
        }

        string locale = SafeString(() => Locale.currentLangName, string.Empty);
        if (string.IsNullOrWhiteSpace(locale))
        {
            locale = SafeString(() => PlayerPrefs.GetString("locale"), string.Empty);
        }

        if (IsExplicitChinese(locale))
        {
            return true;
        }

        if (IsExplicitEnglish(locale))
        {
            return false;
        }

        SystemLanguage systemLanguage = Application.systemLanguage;
        return systemLanguage == SystemLanguage.Chinese
            || systemLanguage == SystemLanguage.ChineseSimplified
            || systemLanguage == SystemLanguage.ChineseTraditional;
    }

    private static bool IsExplicitChinese(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized = value.Trim().ToLowerInvariant();
        return normalized == "zh"
            || normalized == "cn"
            || normalized == "chs"
            || normalized == "zh-cn"
            || normalized == "zh-hans"
            || normalized == "chinese"
            || normalized.Contains("简体")
            || normalized.Contains("中文");
    }

    private static bool IsExplicitEnglish(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized = value.Trim().ToLowerInvariant();
        return normalized == "en"
            || normalized == "eng"
            || normalized == "english"
            || normalized.StartsWith("en-");
    }

    private static bool TryTableText(string key, out string value)
    {
        Dictionary<string, string> table = IsChinese() ? Zh : En;
        if (table.TryGetValue(key, out string? found) || En.TryGetValue(key, out found))
        {
            value = found;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static bool IsResolvedLocaleValue(string value, string key)
    {
        return !string.IsNullOrWhiteSpace(value)
            && !string.Equals(value, key, StringComparison.OrdinalIgnoreCase);
    }

    private static string HumanizeKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return T("category.other");
        }

        string cleaned = key.Trim().Replace('_', ' ').Replace('-', ' ');
        return cleaned.Length == 0 ? T("category.other") : char.ToUpperInvariant(cleaned[0]) + cleaned.Substring(1);
    }

    private static string SafeString(Func<string> getter, string fallback)
    {
        try
        {
            string value = getter();
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }
        catch
        {
            return fallback;
        }
    }
}
