using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using Vortice.Direct2D1;
using Vortice.Mathematics;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin.Shape;
using YukkuriMovieMaker.Project.Items;
using WpfColor = System.Windows.Media.Color;

namespace TimerPlusPlugin;

internal class TimerPlusShapeSource : IShapeSource
{
    private readonly IGraphicsDevicesAndContext devices;
    private readonly TimerPlusShapeParameter parameter;
    private readonly TextItem item = new();
    private readonly object textSource;
    private readonly MethodInfo updateMethod;
    private readonly PropertyInfo outputsProperty;

    private bool isFirst = true;
    private bool errorLogged;
    private ID2D1SolidColorBrush? errorBrush;
    private ID2D1CommandList? errorCommandList;

    private int cachedFrame = -1;
    private double cachedCumulativeRateSeconds;

    // 前回のDecorations比較用(TextDecorationはrecordなので値比較できる)
    private ImmutableList<TextDecoration> lastDecorations = ImmutableList<TextDecoration>.Empty;

    public ID2D1Image Output { get; private set; } = null!;

    public TimerPlusShapeSource(IGraphicsDevicesAndContext devices, TimerPlusShapeParameter parameter)
    {
        this.devices = devices;
        this.parameter = parameter;

        var textSourceType = typeof(TextItem).Assembly.GetType("YukkuriMovieMaker.Player.Video.Items.TextSource")
            ?? throw new InvalidOperationException("TextSource type not found.");

        textSource = Activator.CreateInstance(textSourceType, devices, item)
            ?? throw new InvalidOperationException("Failed to create TextSource instance.");

        updateMethod = textSourceType.GetMethod("Update")
            ?? throw new InvalidOperationException("Update method not found.");

        outputsProperty = textSourceType.GetProperty("Outputs")
            ?? throw new InvalidOperationException("Outputs property not found.");
    }

    public void Update(TimelineItemSourceDescription desc)
    {
        try
        {
            UpdateCore(desc);
        }
        catch (Exception ex)
        {
            RenderFallbackError(ex);
        }
    }

    private void UpdateCore(TimelineItemSourceDescription desc)
    {
        int frame = desc.ItemPosition.Frame;
        int length = desc.ItemDuration.Frame;
        int fps = desc.FPS;
        TimeSpan duration = desc.ItemDuration.Time;

        TimeSpan counterTime = ComputeCounterTime(frame, length, fps, duration);

        var lines = TimerPlusFormatter.FormatLines(counterTime, parameter.Format, parameter.CreateCustomSettings());

        // 行を"\r\n"で連結しつつ、個別設定(CustomStyleEnabled)がONのセグメントについては
        // その文字範囲(Start/Length)を記録し、あとでTextDecorationとして
        // 文字グループ(既定スタイル)の設定を上書きする。
        var textBuilder = new StringBuilder();
        var decorations = new List<TextDecoration>();

        for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
        {
            var line = lines[lineIndex];
            foreach (var segment in line.Segments)
            {
                int start = textBuilder.Length;
                textBuilder.Append(segment.Text);
                int len = segment.Text.Length;
                if (len == 0) continue;

                if (segment.Unit != TimerPlusUnitKind.Default)
                {
                    var decoration = BuildDecoration(segment.Unit, start, len, frame, length, fps);
                    if (decoration != null)
                        decorations.Add(decoration);
                }
            }

            if (lineIndex < lines.Count - 1)
                textBuilder.Append("\r\n");
        }

        string text = textBuilder.ToString();
        var decorationList = decorations.ToImmutableList();

        bool changed = isFirst
            || item.Text != text
            || item.Font != parameter.Font
            || !item.FontSize.DeepEquals(parameter.FontSize)
            || !item.LetterSpacing2.DeepEquals(parameter.LetterSpacing2)
            || item.BasePoint != parameter.BasePoint
            || item.FontColor != parameter.FontColor
            || item.Style != parameter.Style
            || item.StyleColor != parameter.StyleColor
            || item.Bold != parameter.Bold
            || item.Italic != parameter.Italic
            || item.Underline != parameter.Underline
            || item.Strikethrough != parameter.StrikeThrough
            || item.IsDevidedPerCharacter != parameter.SplitByCharacter
            || !decorationList.SequenceEqual(lastDecorations);

        if (changed)
        {
            item.Text = text;
            item.Font = parameter.Font;
            item.FontSize.CopyFrom(parameter.FontSize);
            item.LetterSpacing2.CopyFrom(parameter.LetterSpacing2);
            item.BasePoint = parameter.BasePoint;
            item.FontColor = parameter.FontColor;
            item.Style = parameter.Style;
            item.StyleColor = parameter.StyleColor;
            item.Bold = parameter.Bold;
            item.Italic = parameter.Italic;
            item.Underline = parameter.Underline;
            item.Strikethrough = parameter.StrikeThrough;
            item.IsDevidedPerCharacter = parameter.SplitByCharacter;
            item.Decorations = decorationList;

            lastDecorations = decorationList;

            updateMethod.Invoke(textSource, new object[] { desc });

            var outputs = outputsProperty.GetValue(textSource) as IList;
            if (outputs != null && outputs.Count > 0)
            {
                var firstOutput = outputs[0];
                if (firstOutput != null)
                {
                    var outputProp = firstOutput.GetType().GetProperty("Output");
                    if (outputProp != null)
                    {
                        Output = (ID2D1Image)outputProp.GetValue(firstOutput)!;
                    }
                }
            }
        }

        isFirst = false;
    }

    /// <summary>
    /// 個別設定(CustomStyleEnabled)がONの単位について、その文字範囲(Start/Length)に対する
    /// TextDecorationを作る。文字グループ(既定)の設定は無視され、ここで指定した値で
    /// 上書きされる。
    ///
    /// 【制限事項】TextDecorationにはFontSize自体を指定するプロパティが無く、
    /// 代わりに基準サイズ(グループの文字サイズ)に対する倍率(Scale)を指定する形になっている。
    /// そのため、このメソッドでは 個別サイズ ÷ グループサイズ の比率をScaleとして渡している。
    /// また「文字ごとに分割」はTextItem全体に対する設定であり、TextDecoration側には
    /// 対応するプロパティが無いため、単位ごとの個別指定はできない
    /// (グループの「文字ごとに分割」設定がアイテム全体に適用される)。
    /// </summary>
    private TextDecoration? BuildDecoration(TimerPlusUnitKind unit, int start, int length, int frame, int lengthFrames, int fps)
    {
        string font;
        Animation fontSizeAnim;
        WpfColor fontColor;
        bool bold, italic, underline, strike;
        YukkuriMovieMaker.Project.Items.Style style;
        WpfColor styleColor;

        switch (unit)
        {
            case TimerPlusUnitKind.Day:
                font = parameter.DayFont; fontSizeAnim = parameter.DayFontSize; fontColor = parameter.DayFontColor;
                bold = parameter.DayBold; italic = parameter.DayItalic; underline = parameter.DayUnderline; strike = parameter.DayStrikeThrough;
                style = parameter.DayStyle; styleColor = parameter.DayStyleColor;
                break;
            case TimerPlusUnitKind.Hour:
                font = parameter.HourFont; fontSizeAnim = parameter.HourFontSize; fontColor = parameter.HourFontColor;
                bold = parameter.HourBold; italic = parameter.HourItalic; underline = parameter.HourUnderline; strike = parameter.HourStrikeThrough;
                style = parameter.HourStyle; styleColor = parameter.HourStyleColor;
                break;
            case TimerPlusUnitKind.Minute:
                font = parameter.MinuteFont; fontSizeAnim = parameter.MinuteFontSize; fontColor = parameter.MinuteFontColor;
                bold = parameter.MinuteBold; italic = parameter.MinuteItalic; underline = parameter.MinuteUnderline; strike = parameter.MinuteStrikeThrough;
                style = parameter.MinuteStyle; styleColor = parameter.MinuteStyleColor;
                break;
            case TimerPlusUnitKind.Second:
                font = parameter.SecondFont; fontSizeAnim = parameter.SecondFontSize; fontColor = parameter.SecondFontColor;
                bold = parameter.SecondBold; italic = parameter.SecondItalic; underline = parameter.SecondUnderline; strike = parameter.SecondStrikeThrough;
                style = parameter.SecondStyle; styleColor = parameter.SecondStyleColor;
                break;
            case TimerPlusUnitKind.Fraction:
                font = parameter.FractionFont; fontSizeAnim = parameter.FractionFontSize; fontColor = parameter.FractionFontColor;
                bold = parameter.FractionBold; italic = parameter.FractionItalic; underline = parameter.FractionUnderline; strike = parameter.FractionStrikeThrough;
                style = parameter.FractionStyle; styleColor = parameter.FractionStyleColor;
                break;
            default:
                return null;
        }

        double baseFontSize = parameter.FontSize.GetValue(frame, lengthFrames, fps);
        double unitFontSize = fontSizeAnim.GetValue(frame, lengthFrames, fps);
        double scale = baseFontSize > 0.0001 ? unitFontSize / baseFontSize : 1.0;

        return new TextDecoration(
            Start: start,
            Length: length,
            IsBold: bold,
            IsItalic: italic,
            Scale: scale,
            Font: font,
            Foreground: fontColor,
            IsLineBreak: false,
            OffsetX: 0,
            OffsetY: 0,
            IsAbsoluteX: false,
            IsAbsoluteY: false,
            DecorationColor: styleColor,
            RotationZ: 0,
            IsStrikethrough: strike,
            IsUnderline: underline,
            RotationGroupId: 0,
            PositionGroupId: 0,
            CharacterSpacing: null,
            StyleType: (int)style);
    }

    private TimeSpan ComputeCounterTime(int frame, int length, int fps, TimeSpan duration)
    {
        double offset = parameter.InitialValueOffset.GetValue(frame, length, fps);
        double initialValue = parameter.GetInitialValueBaseSeconds(fps) + offset;

        double cumulativeRateSeconds = GetCumulativeRateSeconds(frame, length, fps);

        bool isCountDown = parameter.Direction == TimerPlusCountDirection.CountDown;
        bool reverse = parameter.ReverseBehavior;

        bool initialIsEnd = isCountDown != reverse;

        if (initialIsEnd)
        {
            initialValue += duration.TotalSeconds;
            cumulativeRateSeconds *= -1.0;
        }

        double seconds = initialValue + cumulativeRateSeconds;
        return TimeSpan.FromTicks((long)Math.Round(seconds * TimeSpan.TicksPerSecond, MidpointRounding.AwayFromZero));
    }

    private double GetCumulativeRateSeconds(int frame, int length, int fps)
    {
        if (frame <= 0)
        {
            cachedFrame = 0;
            cachedCumulativeRateSeconds = 0.0;
            return 0.0;
        }

        if (frame == cachedFrame)
        {
            return cachedCumulativeRateSeconds;
        }

        if (frame == cachedFrame + 1 && cachedFrame >= 0)
        {
            double rateAtPrevFrame = parameter.PlaybackRate.GetValue(cachedFrame, length, fps) / 100.0;
            cachedCumulativeRateSeconds += rateAtPrevFrame / fps;
            cachedFrame = frame;
            return cachedCumulativeRateSeconds;
        }

        double sum = 0.0;
        for (int f = 0; f < frame; f++)
        {
            double rate = parameter.PlaybackRate.GetValue(f, length, fps) / 100.0;
            sum += rate / fps;
        }
        cachedCumulativeRateSeconds = sum;
        cachedFrame = frame;
        return sum;
    }

    private void RenderFallbackError(Exception ex)
    {
        if (!errorLogged)
        {
            errorLogged = true;
            try
            {
                var path = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "TimerPlusPlugin_RenderError.txt");
                System.IO.File.WriteAllText(path, DateTime.Now + "\r\n" + ex);
            }
            catch
            {
            }
        }

        try
        {
            var dc = devices.DeviceContext;

            errorBrush ??= dc.CreateSolidColorBrush(new Color4(1f, 0f, 0f, 1f));

            errorCommandList?.Dispose();
            var newCommandList = dc.CreateCommandList();
            dc.Target = newCommandList;
            dc.BeginDraw();
            dc.Clear(null);
            dc.FillRectangle(new Rect(0, 0, 200, 60), errorBrush);
            dc.EndDraw();
            dc.Target = null;
            newCommandList.Close();

            errorCommandList = newCommandList;
            Output = newCommandList;
        }
        catch
        {
        }
    }

    public void Dispose()
    {
        (textSource as IDisposable)?.Dispose();
        errorCommandList?.Dispose();
        errorBrush?.Dispose();
    }
}