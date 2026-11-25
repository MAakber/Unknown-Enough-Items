using System;
using System.Collections.Generic;
using System.Text;

internal static class UeiAcquisitionSupplement
{
    // Filter the display copy only. Descriptions, annotations and typed routes
    // remain intact, including information the native scan has not found yet.
    public static string Filter(UeiAcquisitionIndex index, UeiEntryKind kind, string id, string annotation)
    {
        if (string.IsNullOrWhiteSpace(annotation)) return string.Empty;
        HashSet<string> covered = new(StringComparer.OrdinalIgnoreCase);
        foreach (UeiAcquisitionRoute route in index.Get(kind, id))
        {
            AddRoute(covered, route);
            if (route.Kind == UeiAcquisitionRouteKind.FilledContainer && route.Container != null)
            {
                foreach (UeiAcquisitionRoute source in index.Get(UeiEntryKind.Item, route.Container.Id))
                    AddRoute(covered, source);
            }
        }
        if (covered.Count == 0 || !TrySplit(annotation, out List<Part> parts)) return annotation;

        bool[] matched = new bool[parts.Count];
        for (int i = 0; i < parts.Count; i++) matched[i] = covered.Contains(Normalize(parts[i].Text));
        bool[] remove = (bool[])matched.Clone();
        PreserveConditionContext(parts, matched, remove);

        bool changed = false;
        StringBuilder text = new();
        int previous = -1;
        for (int i = 0; i < parts.Count; i++)
        {
            if (remove[i]) { changed = true; continue; }
            if (previous >= 0)
            {
                string separator = parts[i].SeparatorBefore;
                for (int j = previous + 1; j <= i; j++)
                {
                    if (parts[j].Line == parts[j - 1].Line) continue;
                    separator = parts[j].SeparatorBefore;
                    break;
                }
                text.Append(separator);
            }
            text.Append(parts[i].Text);
            previous = i;
        }
        return changed ? text.ToString() : annotation;
    }

    private static void AddRoute(HashSet<string> covered, UeiAcquisitionRoute route)
    {
        UeiAcquisitionBuilding? building = route.Building;
        if (building != null)
        {
            Add(covered, building.Id);
            Add(covered, building.Name);
            Add(covered, UeiI18n.BuildingText(building.Id, building.Name));
            // The shipped zh-CN bandage description calls medcrate 医疗箱;
            // its building locale calls the same ID 医疗物资箱.
            if (string.Equals(building.Id, "medcrate", StringComparison.OrdinalIgnoreCase))
            {
                Add(covered, "医疗箱");
                Add(covered, "医疗物资箱");
                Add(covered, "Medical crate");
            }
            // A parent location alone is not evidence of another source.
        }
        if (route.Kind == UeiAcquisitionRouteKind.FilledContainer && route.Container != null)
        {
            Add(covered, route.Container.Id);
            Add(covered, route.Container.Name);
            Add(covered, UeiI18n.ItemText(route.Container.Id, route.Container.Name));
        }
        if (route.Kind == UeiAcquisitionRouteKind.Trade || route.Kind == UeiAcquisitionRouteKind.WeaponTrade)
        {
            Add(covered, "交易");
            Add(covered, "商人");
            Add(covered, "Trade");
            Add(covered, "Trading");
            Add(covered, "Trader");
            Add(covered, "Traders");
            Add(covered, UeiI18n.T("acquisition.card.trader"));
        }
        if (route.Kind == UeiAcquisitionRouteKind.WeaponTrade)
        {
            Add(covered, "武器商人");
            Add(covered, "Weapon trader");
            Add(covered, "Weapon traders");
        }
    }

    private static void Add(HashSet<string> covered, string value)
    {
        string normalized = Normalize(value);
        if (normalized.Length > 0) covered.Add(normalized);
    }

    private static string Normalize(string value)
    {
        StringBuilder text = new();
        bool space = false;
        foreach (char c in value)
        {
            if (char.IsWhiteSpace(c)) { space = text.Length > 0; continue; }
            if (space) text.Append(' ');
            text.Append(c);
            space = false;
        }
        return text.ToString();
    }

    private readonly struct Part
    {
        public readonly string Text;
        public readonly string SeparatorBefore;
        public readonly int Line;
        public Part(string text, string separatorBefore, int line)
        { Text = text; SeparatorBefore = separatorBefore; Line = line; }
    }

    private static bool TrySplit(string annotation, out List<Part> parts)
    {
        parts = new();
        Stack<char> closes = new();
        int start = 0;
        int line = 0;
        string separatorBefore = string.Empty;
        for (int i = 0; i < annotation.Length; i++)
        {
            char c = annotation[i];
            if (closes.Count > 0)
            {
                char close = closes.Peek();
                if (c == close) { closes.Pop(); continue; }
                if (IsQuote(close))
                {
                    if (c == '\\' && i + 1 < annotation.Length) i++;
                    continue;
                }
            }
            char closing = c switch
            {
                '(' => ')', '（' => '）', '[' => ']', '【' => '】', '{' => '}',
                '「' => '」', '『' => '』', '《' => '》', '〈' => '〉',
                '“' => '”', '‘' => '’', '"' => '"',
                '\'' when i == 0 || !char.IsLetterOrDigit(annotation[i - 1]) => '\'',
                _ => '\0',
            };
            if (closing != '\0') { closes.Push(closing); continue; }
            if (c is ')' or '）' or ']' or '】' or '}' or '」' or '』' or '》' or '〉' or '”' or '’')
                return false;
            if (closes.Count > 0) continue;
            bool newline = c is '\r' or '\n' or '\u2028' or '\u2029';
            if (!newline && c is not '/' and not '／' and not '、') continue;
            string part = annotation.Substring(start, i - start).Trim();
            if (part.Length == 0) return false;
            parts.Add(new(part, separatorBefore, line));
            separatorBefore = c.ToString();
            if (c == '\r' && i + 1 < annotation.Length && annotation[i + 1] == '\n')
            { separatorBefore = "\r\n"; i++; }
            if (newline) line++;
            start = i + 1;
        }
        if (closes.Count > 0) return false;
        string last = annotation.Substring(start).Trim();
        if (last.Length == 0) return false;
        parts.Add(new(last, separatorBefore, line));
        return true;
    }

    private static bool IsQuote(char c) => c is '"' or '\'' or '”' or '’';

    private static void PreserveConditionContext(List<Part> parts, bool[] matched, bool[] remove)
    {
        int previousStart = 0;
        int coveredBlockStart = 0;
        bool previousCovered = false;
        bool groupedConditionAbove = false;
        for (int start = 0; start < parts.Count;)
        {
            int end = start + 1;
            while (end < parts.Count && parts[end].Line == parts[start].Line) end++;
            bool allCovered = true;
            bool groupedCondition = false;
            for (int i = start; i < end; i++)
            {
                allCovered &= matched[i];
                groupedCondition |= !matched[i] && parts[i].Text[0] is '(' or '（' or '[' or '【' or '{';
            }
            // A separate bracketed note can qualify sources on the same line
            // or the source block immediately below it.
            if (groupedCondition || (groupedConditionAbove && allCovered))
                for (int i = start; i < end; i++) remove[i] = false;
            // An unknown following line may qualify the source list above it.
            // Retain the preceding block so that condition keeps its context.
            if (!allCovered && start > 0)
            {
                int contextStart = previousCovered ? coveredBlockStart : previousStart;
                for (int i = contextStart; i < start; i++) remove[i] = false;
            }
            if (allCovered && !previousCovered) coveredBlockStart = start;
            previousStart = start;
            previousCovered = allCovered;
            groupedConditionAbove = groupedCondition || (groupedConditionAbove && allCovered);
            start = end;
        }
    }
}
