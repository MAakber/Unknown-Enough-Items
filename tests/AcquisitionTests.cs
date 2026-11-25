internal static class AcquisitionTests
{
    public static void Run(Action<bool, string> check)
    {
        var cases = new (string Name, string? Description, string Expected)[]
        {
            ("screenshot format",
                "一个0.75L的罐子。\n<color=#91a7ff>┃容重比：0.22u/100mL</color>\n<color=#ffee58>┃获取：维生舱箱子/食物箱/空投舱/交易</color>",
                "维生舱箱子/食物箱/空投舱/交易"),
            ("separate label and value colors",
                "描述\n<color=#888888>获取：</color><color=#aaaaaa>生命舱箱子/食物箱/交易</color>\n<color=orange>评价：不错</color>",
                "生命舱箱子/食物箱/交易"),
            ("crafting and effects in the same color block",
                "<color=#ffee58>┃获取：医疗箱/交易\n┃制作(5)：2撕裂的绷带</color>\n┃皮肤 +2.5",
                "医疗箱/交易"),
            ("combined acquisition and crafting label",
                "┃获取/制作：纤维组合(需加热)", "纤维组合(需加热)"),
            ("source on the next line",
                "<color=#ffee58>┃获取：\n┃空投舱/维生舱箱子/食物箱/交易/棘背兽长老掉落</color>\n<color=orange>特点：共5次使用</color>",
                "空投舱/维生舱箱子/食物箱/交易/棘背兽长老掉落"),
            ("combined heading with a following crafting route",
                "<color=#ffee58>┃获取/制作：\n┃制作(8)：3木板+2废料管+1钉子+1(可捶打)=1夹板(80%)</color>\n<color=orange>佩戴后夹板的重量不计入负重</color>",
                "制作(8)：3木板+2废料管+1钉子+1(可捶打)=1夹板(80%)"),
            ("multiple decorated source lines inside one block",
                "<color=yellow>┃获取：\n┃医疗箱/交易\n┃空投舱</color>\n┃制作(1)：纤维", "医疗箱/交易\n空投舱"),
            ("next-line label with separate color tags",
                "<color=gray>获取：</color>\n<color=yellow>医疗箱/交易</color>\n评价：不错", "医疗箱/交易"),
            ("next-line condition with a colon",
                "获取：\n交易(售价: $2)\n重量：1u", "交易(售价: $2)"),
            ("a following unrelated heading is not a source", "获取：\n重量：1u", ""),
            ("an empty heading at the end of a block",
                "<color=yellow>┃获取：\n</color>\n一些普通说明", ""),
            ("fullwidth slash", "获取／制作：拆解", "拆解"),
            ("traditional Chinese", "<color=yellow>獲取途徑：醫療箱/交易</color>", "醫療箱/交易"),
            ("English annotation", "A useful tool.\nObtained from: supply crates / traders", "supply crates / traders"),
            ("English case and decorations", " \t│ sOuRcEs : crates / traders \r\nWeight: 1u", "crates / traders"),
            ("acquired from", "Acquired from: dismantling", "dismantling"),
            ("acquisition label", "Acquisition: scavenging", "scavenging"),
            ("rich text line breaks", "Description<br>获取：医疗箱<BR/>来源：交易", "医疗箱\n交易"),
            ("multiple annotations and duplicate lines", "获取：医疗箱\n来源：交易\n获取：医疗箱", "医疗箱\n交易"),
            ("colon inside the value", "获取：交易(售价: $2)", "交易(售价: $2)"),
            ("numeric comparisons", "获取：制作(耐久>=90%) / 地面(概率<5%)", "制作(耐久>=90%) / 地面(概率<5%)"),
            ("nested and shorthand rich text", "<#ffee58><b>┃获取</b>：<size=12>食物箱</size></color>", "食物箱"),
            ("sprite decoration", "<sprite index=0>┃获取：食物箱", "食物箱"),
            ("ordinary English source mentions", "A light source! Uses a heat source in recipes.", ""),
            ("ordinary Chinese mentions", "来源不明。正常游戏无法获取。\n说明：获取难度很高", ""),
            ("label must start the line", "说明：获取：医疗箱", ""),
            ("missing colon", "获取物品需要交易", ""),
            ("empty annotation stays empty", "获取：\n┃制作(1)：纤维", ""),
            ("missing locale key", "unknown_itemdsc", ""),
            ("null", null, ""),
            ("whitespace", " \r\n\t", ""),
        };
        foreach (var test in cases)
        {
            check(UeiAcquisition.Extract(test.Description) == test.Expected, "Acquisition extraction: " + test.Name);
        }

        string longSource = string.Join("/", Enumerable.Repeat("物资箱(保留完整条件说明)", 100));
        check(UeiAcquisition.Extract("┃获取：" + longSource) == longSource, "Long acquisition text was truncated");

        string language = UeiPlugin.LanguageMode.Value;
        try
        {
            UeiPlugin.LanguageMode.Value = "en";
            check(UeiI18n.T("detail.acquisition") == "Acquisition", "English acquisition heading");
            check(UeiI18n.T("detail.noAcquisition") == "No acquisition info", "English missing acquisition text");
            check(UeiI18n.T("detail.craftingRecipes") == "Crafting recipes", "English crafting heading");
            check(UeiI18n.T("detail.empty") == "No crafting recipes", "English source empty state");
            check(UeiI18n.T("detail.emptyUses") == "No usage recipes", "English usage empty state");
            UeiPlugin.LanguageMode.Value = "zh";
            check(UeiI18n.T("detail.acquisition") == "获取途径", "Chinese acquisition heading");
            check(UeiI18n.T("detail.noAcquisition") == "暂无获取信息", "Chinese missing acquisition text");
            check(UeiI18n.T("detail.craftingRecipes") == "制作配方", "Chinese crafting heading");
            check(UeiI18n.T("detail.empty") == "无制作配方", "Chinese source empty state");
            check(UeiI18n.T("detail.emptyUses") == "无用途配方", "Chinese usage empty state");
        }
        finally
        {
            UeiPlugin.LanguageMode.Value = language;
        }
        Console.WriteLine("PASS: annotated acquisition text, rich text boundaries, missing data, and bilingual empty states");
    }
}
