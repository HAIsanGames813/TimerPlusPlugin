using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace TimerPlusPlugin;

/// <summary>どの単位の値によって作られたテキスト片かを表す。</summary>
public enum TimerPlusUnitKind
{
    Default,
    Day,
    Hour,
    Minute,
    Second,
    Fraction,
}

/// <summary>1つのテキスト片。日/時/分/秒/小数秒は常に個別セグメントとして扱われる。</summary>
public sealed record TimerPlusTextSegment(string Text, TimerPlusUnitKind Unit);

/// <summary>1行分のテキスト片の並び。</summary>
public sealed record TimerPlusTextLine(int LineNumber, IReadOnlyList<TimerPlusTextSegment> Segments);

/// <summary>1つの単位（日/時/分/秒/小数秒）のフォーマット設定。</summary>
public sealed record TimerPlusUnitSettings(
    bool Enabled,
    int Digits,
    string Prefix,
    string Suffix,
    int Line,
    bool CustomStyleEnabled);

public sealed record TimerPlusCustomSettings(
    TimerPlusUnitSettings Day,
    TimerPlusUnitSettings Hour,
    TimerPlusUnitSettings Minute,
    TimerPlusUnitSettings Second,
    TimerPlusUnitSettings Fraction);

public static class TimerPlusFormatter
{
    private const long SecPerDay = 86400;
    private const long SecPerHour = 3600;
    private const long SecPerMinute = 60;
    private const long SecPerSecond = 1;

    /// <summary>カウンター時間を行・テキスト片リストに変換する。「カスタム」以外は1行1セグメント。</summary>
    public static IReadOnlyList<TimerPlusTextLine> FormatLines(TimeSpan counterTime, TimerPlusFormat format, TimerPlusCustomSettings custom)
    {
        bool negative = counterTime.Ticks < 0;
        var t = counterTime.Duration();
        string sign = negative ? "-" : string.Empty;

        if (format == TimerPlusFormat.Custom)
        {
            var lines = FormatCustomLines(t, custom);
            return PrependSign(lines, sign);
        }

        string simple = format switch
        {
            TimerPlusFormat.S => ((long)t.TotalSeconds).ToString("D1", CultureInfo.InvariantCulture),
            TimerPlusFormat.SS => ((long)t.TotalSeconds).ToString("D2", CultureInfo.InvariantCulture),
            TimerPlusFormat.SSS => ((long)t.TotalSeconds).ToString("D3", CultureInfo.InvariantCulture),
            TimerPlusFormat.SSSS => ((long)t.TotalSeconds).ToString("D4", CultureInfo.InvariantCulture),
            TimerPlusFormat.SSFF => t.ToString(@"ss\.ff", CultureInfo.InvariantCulture),
            TimerPlusFormat.MMSS => t.ToString(@"mm\:ss", CultureInfo.InvariantCulture),
            TimerPlusFormat.MMSSFF => t.ToString(@"mm\:ss\.ff", CultureInfo.InvariantCulture),
            TimerPlusFormat.HHMMSS => t.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture),
            TimerPlusFormat.HHMMSSFF => t.ToString(@"hh\:mm\:ss\.ff", CultureInfo.InvariantCulture),
            _ => throw new NotImplementedException(),
        };

        return new[]
        {
            new TimerPlusTextLine(1, new[] { new TimerPlusTextSegment(sign + simple, TimerPlusUnitKind.Default) }),
        };
    }

    private static IReadOnlyList<TimerPlusTextLine> PrependSign(IReadOnlyList<TimerPlusTextLine> lines, string sign)
    {
        if (sign.Length == 0 || lines.Count == 0 || lines[0].Segments.Count == 0)
            return lines;

        var firstLine = lines[0];
        var newSegments = firstLine.Segments.ToList();
        newSegments[0] = newSegments[0] with { Text = sign + newSegments[0].Text };

        var newLines = lines.ToList();
        newLines[0] = firstLine with { Segments = newSegments };
        return newLines;
    }

    /// <summary>カスタム書式。各単位は常に独立したセグメントとして生成し、行番号でグルーピングする。</summary>
    private static IReadOnlyList<TimerPlusTextLine> FormatCustomLines(TimeSpan t, TimerPlusCustomSettings s)
    {
        double totalSecondsAbs = t.TotalSeconds;

        long wholeSeconds = (long)Math.Floor(totalSecondsAbs);
        double fracPart = totalSecondsAbs - wholeSeconds;

        int fracDigits = Math.Max(0, s.Fraction.Digits);
        int fracValue = 0;
        if (s.Fraction.Enabled && fracDigits > 0)
        {
            double scale = Math.Pow(10, fracDigits);
            fracValue = (int)Math.Round(fracPart * scale, MidpointRounding.AwayFromZero);
            if (fracValue >= scale)
            {
                fracValue = 0;
                wholeSeconds += 1;
            }
        }

        var ancestorsForHour = new (long size, bool enabled)[] { (SecPerDay, s.Day.Enabled) };
        var ancestorsForMinute = new (long size, bool enabled)[] { (SecPerHour, s.Hour.Enabled), (SecPerDay, s.Day.Enabled) };
        var ancestorsForSecond = new (long size, bool enabled)[] { (SecPerMinute, s.Minute.Enabled), (SecPerHour, s.Hour.Enabled), (SecPerDay, s.Day.Enabled) };

        long dayValue = wholeSeconds / SecPerDay;
        long hourValue = ApplyCarry(wholeSeconds, SecPerHour, ancestorsForHour);
        long minuteValue = ApplyCarry(wholeSeconds, SecPerMinute, ancestorsForMinute);
        long secondValue = ApplyCarry(wholeSeconds, SecPerSecond, ancestorsForSecond);

        string FormatNumber(long val, int digits) => val.ToString("D" + Math.Max(1, digits), CultureInfo.InvariantCulture);

        var items = new List<(int line, TimerPlusUnitKind kind, string text)>();

        // 各単位は常に独立したセグメントとして生成する(スタイルの出所はShapeSource側で分岐)

        if (s.Day.Enabled)
            items.Add((Math.Max(1, s.Day.Line), TimerPlusUnitKind.Day,
                s.Day.Prefix + FormatNumber(dayValue, s.Day.Digits) + s.Day.Suffix));

        if (s.Hour.Enabled)
            items.Add((Math.Max(1, s.Hour.Line), TimerPlusUnitKind.Hour,
                s.Hour.Prefix + FormatNumber(hourValue, s.Hour.Digits) + s.Hour.Suffix));

        if (s.Minute.Enabled)
            items.Add((Math.Max(1, s.Minute.Line), TimerPlusUnitKind.Minute,
                s.Minute.Prefix + FormatNumber(minuteValue, s.Minute.Digits) + s.Minute.Suffix));

        if (s.Second.Enabled)
            items.Add((Math.Max(1, s.Second.Line), TimerPlusUnitKind.Second,
                s.Second.Prefix + FormatNumber(secondValue, s.Second.Digits) + s.Second.Suffix));

        if (s.Fraction.Enabled)
            items.Add((Math.Max(1, s.Fraction.Line), TimerPlusUnitKind.Fraction,
                s.Fraction.Prefix + FormatNumber(fracValue, s.Fraction.Digits) + s.Fraction.Suffix));

        // 行番号でグルーピング(同じ行内は処理順を維持)
        var byLine = items
            .Select((it, idx) => (it, idx))
            .OrderBy(x => x.it.line)
            .ThenBy(x => x.idx)
            .GroupBy(x => x.it.line);

        var result = new List<TimerPlusTextLine>();
        foreach (var group in byLine)
        {
            var segments = group.Select(x => new TimerPlusTextSegment(x.it.text, x.it.kind)).ToList();
            if (segments.Count > 0)
                result.Add(new TimerPlusTextLine(group.Key, segments));
        }

        return result;
    }

    private static long ApplyCarry(long totalSeconds, long unitSizeSeconds, (long size, bool enabled)[] ancestorsNearestFirst)
    {
        long totalInUnit = totalSeconds / unitSizeSeconds;
        foreach (var (size, enabled) in ancestorsNearestFirst)
        {
            if (enabled)
            {
                long modulus = size / unitSizeSeconds;
                return totalInUnit % modulus;
            }
        }
        return totalInUnit;
    }
}