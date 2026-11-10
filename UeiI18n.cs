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
        ["detail.empty"] = "No recipes",
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
        ["status.cheatSpawned"] = "Gave 1",
        ["status.cheatIllegal"] = "Not a legal item",
        ["status.cheatFailed"] = "Give failed",
        ["button.settings.short"] = "S",
        ["button.settings"] = "Settings",
        ["button.settings.desc"] = "Open UEI settings",
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
        ["detail.empty"] = "无配方",
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
        ["status.cheatSpawned"] = "已发放 1 个",
        ["status.cheatIllegal"] = "不是合法物品",
        ["status.cheatFailed"] = "发放失败",
        ["button.settings.short"] = "设",
        ["button.settings"] = "设置",
        ["button.settings.desc"] = "打开 UEI 设置",
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
        if (table.TryGetValue(key, out string value))
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
        if (IsResolvedLocaleValue(value, localeKey))
        {
            return value;
        }

        // 纯查询，不传 fallback，避免 LocaleRegistry.Register 用英文覆盖中文
        string cu = CuCoreLibHelper.GetLocaleText("item", localeKey);
        if (IsResolvedLocaleValue(cu, localeKey))
        {
            return cu;
        }

        return fallback;
    }

    public static string OtherText(string localeKey, string fallback)
    {
        if (string.IsNullOrWhiteSpace(localeKey))
        {
            return fallback;
        }

        string value = SafeString(() => Locale.GetOther(localeKey), string.Empty);
        if (IsResolvedLocaleValue(value, localeKey))
        {
            return value;
        }

        string cu = CuCoreLibHelper.GetLocaleText("other", localeKey);
        if (IsResolvedLocaleValue(cu, localeKey))
        {
            return cu;
        }

        string liquid = CuCoreLibHelper.GetLocaleText("liquid", localeKey);
        if (IsResolvedLocaleValue(liquid, localeKey))
        {
            return liquid;
        }

        return fallback;
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
        string raw = SafeString(() => category.ToString(), string.Empty);
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
        if (table.TryGetValue(key, out value))
        {
            return true;
        }

        return En.TryGetValue(key, out value);
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
