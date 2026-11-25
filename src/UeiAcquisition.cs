using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

internal static class UeiAcquisition
{
    private static readonly Regex LineBreakTags = new(@"<br\s*/?>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex RichTextTags = new(@"</?[a-z][^>\r\n]*>|<#[0-9a-f]{3,8}>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly char[] LineSeparators = { '\r', '\n', '\u2028', '\u2029' };
    private static readonly char[] LabelSeparators = { ':', '：' };
    private static readonly char[] OpeningParentheses = { '(', '（' };
    private static readonly char[] ClosingParentheses = { ')', '）' };
    private static readonly char[] LineDecorations = { ' ', '\t', '|', '│', '┃', '▌' };
    private static readonly HashSet<string> Labels = new(StringComparer.OrdinalIgnoreCase)
    {
        "获取", "获取途径", "获取来源", "获取/制作", "获取／制作",
        "獲取", "獲取途徑", "獲取來源", "獲取/製作", "獲取／製作",
        "获得", "獲得", "来源", "來源",
        "Acquisition", "Acquired from", "Obtained from", "Source", "Sources",
    };

    // Read only explicitly labelled lines from the same description used by
    // hover tooltips. A mention of a source in ordinary prose is not evidence
    // of an acquisition route, and subsequent crafting/effect lines stay out.
    public static string Extract(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return string.Empty;
        }

        List<string> sources = new();
        HashSet<string> seen = new(StringComparer.Ordinal);
        bool readContinuation = false;
        bool includesCrafting = false;
        bool hasContinuation = false;
        int colorDepth = 0;
        int sourceDepth = 0;
        foreach (string rawLine in LineBreakTags.Replace(description, "\n").Split(LineSeparators))
        {
            int depthBeforeLine = colorDepth;
            foreach (Match tag in RichTextTags.Matches(rawLine))
            {
                if (tag.Value.StartsWith("</color", StringComparison.OrdinalIgnoreCase))
                    colorDepth = Math.Max(0, colorDepth - 1);
                else if (tag.Value.StartsWith("<color", StringComparison.OrdinalIgnoreCase) || tag.Value.StartsWith("<#", StringComparison.Ordinal))
                    colorDepth++;
            }

            string plainLine = RichTextTags.Replace(rawLine, string.Empty).Trim();
            string line = plainLine.TrimStart(LineDecorations).TrimStart();
            if (line.Length == 0)
            {
                if (string.IsNullOrWhiteSpace(rawLine) || (sourceDepth > 0 && colorDepth < sourceDepth))
                    readContinuation = false;
                continue;
            }

            int separator = line.IndexOfAny(LabelSeparators);
            string label = separator < 0 ? string.Empty : line.Substring(0, separator).Trim();
            if (Labels.Contains(label))
            {
                string value = line.Substring(separator + 1).Trim();
                AddSource(value);
                // An empty heading may put its value on the following line.
                // Once that value is read, only decorated lines inside the same
                // color block can continue it; plain prose stays out.
                readContinuation = value.Length == 0;
                includesCrafting = label.IndexOf("制作", StringComparison.Ordinal) >= 0 || label.IndexOf("製作", StringComparison.Ordinal) >= 0;
                hasContinuation = false;
                sourceDepth = colorDepth;
                continue;
            }

            if (!readContinuation)
                continue;

            int opening = line.IndexOfAny(OpeningParentheses);
            int closing = line.IndexOfAny(ClosingParentheses);
            bool colonInsideParentheses = opening >= 0 && opening < separator && (closing < 0 || closing > separator);
            bool otherHeading = separator >= 0 && !colonInsideParentheses;
            bool craftingHeading = label == "制作" || label == "製作"
                || label.StartsWith("制作(", StringComparison.Ordinal) || label.StartsWith("制作（", StringComparison.Ordinal)
                || label.StartsWith("製作(", StringComparison.Ordinal) || label.StartsWith("製作（", StringComparison.Ordinal);
            bool decorated = Array.IndexOf(LineDecorations, plainLine[0]) >= 0;
            if ((sourceDepth > 0 && depthBeforeLine < sourceDepth)
                || (hasContinuation && !decorated)
                || (otherHeading && !(includesCrafting && craftingHeading)))
            {
                readContinuation = false;
                continue;
            }

            AddSource(line);
            hasContinuation = true;
            readContinuation = sourceDepth > 0 && colorDepth >= sourceDepth;
        }

        return string.Join("\n", sources);

        void AddSource(string value)
        {
            if (!string.IsNullOrWhiteSpace(value) && seen.Add(value))
                sources.Add(value);
        }
    }
}
