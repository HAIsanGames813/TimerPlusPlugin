using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;
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

/// <summary>
/// タイマー+の実描画クラス。
///
/// 【設計変更の経緯】当初は1つの TextItem.Text に全行を改行文字("\r\n"→のちに"\n")で
/// 連結し、各単位の TextDecoration の Start/Length をその連結後の文字列内の位置として
/// 計算する方式にしていた。しかし実機検証で「表示行が増えるほど、下の行ほど内容が
/// 断片化・重複する(後ろからn文字ずつ増える)」という不具合が確認された。
/// 改行の実際の内部表現(1文字か2文字か、あるいは全く別の仕組みか)を外部から
/// 正確に推測するのが難しく、修正を重ねても再発したため、根本的に方式を変えた。
///
/// 現在は「1行につき1組の TextItem/TextSource を用意し、改行文字を一切使わずに
/// 各行を完全に独立してレンダリングし、その結果(画像)をこちら側で縦に並べて合成する」
/// という方式にしている。これにより、各行内の TextDecoration の Start/Length は
/// その行単体の文字列だけを基準にすればよくなり、行をまたぐインデックス計算が
/// 一切不要になった(このバグのクラス自体が原理的に発生しなくなる)。
/// </summary>
internal class TimerPlusShapeSource : IShapeSource
{
    private readonly IGraphicsDevicesAndContext devices;
    private readonly TimerPlusShapeParameter parameter;

    private readonly List<LineRenderer> lineRenderers = new();

    private bool isFirst = true;
    private bool errorLogged;
    private ID2D1SolidColorBrush? errorBrush;
    private ID2D1CommandList? errorCommandList;
    private ID2D1CommandList? compositeCommandList;

    private int cachedFrame = -1;
    private double cachedCumulativeRateSeconds;

    private string lastCacheKey = string.Empty;

    public ID2D1Image Output { get; private set; } = null!;

    public TimerPlusShapeSource(IGraphicsDevicesAndContext devices, TimerPlusShapeParameter parameter)
    {
        this.devices = devices;
        this.parameter = parameter;
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

        // 行ごとに、その行単体のテキストとDecorationsを作る。
        // (改行文字を一切使わないので、行をまたぐインデックスのズレが発生しようがない)
        var lineData = new List<(string text, ImmutableList<TextDecoration> decorations)>();
        foreach (var line in lines)
        {
            var sb = new StringBuilder();
            var decorations = new List<TextDecoration>();
            foreach (var segment in line.Segments)
            {
                int start = sb.Length;
                sb.Append(segment.Text);
                int len = segment.Text.Length;
                if (len == 0) continue;

                if (segment.Unit != TimerPlusUnitKind.Default)
                {
                    var d = BuildDecoration(segment.Unit, start, len, frame, length, fps);
                    if (d != null) decorations.Add(d);
                }
            }
            lineData.Add((sb.Length == 0 ? " " : sb.ToString(), decorations.ToImmutableList()));
        }

        // 変化検知: 厳密な差分検知ではなく、行ごとのテキスト・Decorations・
        // 文字グループの主要プロパティを連結した簡易キーで「何か変わったら丸ごと作り直す」
        // という単純な方式にしている(細かいプロパティ単位の比較は複雑になりバグの温床になりやすいため)。
        var keyBuilder = new StringBuilder();
        keyBuilder.Append(parameter.Font).Append('|');
        keyBuilder.Append(parameter.FontSize.GetValue(frame, length, fps)).Append('|');
        keyBuilder.Append(parameter.LetterSpacing2.GetValue(frame, length, fps)).Append('|');
        keyBuilder.Append(parameter.BasePoint).Append('|');
        keyBuilder.Append(parameter.FontColor).Append('|');
        keyBuilder.Append(parameter.Style).Append('|');
        keyBuilder.Append(parameter.StyleColor).Append('|');
        keyBuilder.Append(parameter.Bold).Append('|');
        keyBuilder.Append(parameter.Italic).Append('|');
        keyBuilder.Append(parameter.Underline).Append('|');
        keyBuilder.Append(parameter.StrikeThrough).Append('|');
        keyBuilder.Append(parameter.SplitByCharacter);
        foreach (var (text, decorations) in lineData)
        {
            keyBuilder.Append("##").Append(text);
            foreach (var d in decorations)
                keyBuilder.Append('~').Append(d);
        }
        string cacheKey = keyBuilder.ToString();

        if (isFirst || cacheKey != lastCacheKey)
        {
            RenderComposite(lineData, desc, frame, length, fps);
            lastCacheKey = cacheKey;
        }

        isFirst = false;
    }

    private void RenderComposite(List<(string text, ImmutableList<TextDecoration> decorations)> lineData, TimelineItemSourceDescription desc, int frame, int length, int fps)
    {
        // 行数分の LineRenderer を確保する(足りなければ作成、余っていれば破棄)
        while (lineRenderers.Count < lineData.Count)
            lineRenderers.Add(new LineRenderer(devices));
        while (lineRenderers.Count > lineData.Count)
        {
            lineRenderers[^1].Dispose();
            lineRenderers.RemoveAt(lineRenderers.Count - 1);
        }

        var dc = devices.DeviceContext;

        // 各行を更新し、行ごとの画像とサイズを取得する。
        // 行内は常に左上基準(LeftTop)で組み立て、全体としての配置(BasePoint)は
        // こちら側で解釈して合成時に位置調整する。
        var lineImages = new List<(ID2D1Image image, float width, float height)>();
        for (int i = 0; i < lineData.Count; i++)
        {
            var renderer = lineRenderers[i];
            var (text, decorations) = lineData[i];

            renderer.Item.Text = text;
            renderer.Item.Font = parameter.Font;
            renderer.Item.FontSize.CopyFrom(parameter.FontSize);
            renderer.Item.LetterSpacing2.CopyFrom(parameter.LetterSpacing2);
            renderer.Item.BasePoint = YukkuriMovieMaker.Project.Items.BasePoint.LeftTop;
            renderer.Item.FontColor = parameter.FontColor;
            renderer.Item.Style = parameter.Style;
            renderer.Item.StyleColor = parameter.StyleColor;
            renderer.Item.Bold = parameter.Bold;
            renderer.Item.Italic = parameter.Italic;
            renderer.Item.Underline = parameter.Underline;
            renderer.Item.Strikethrough = parameter.StrikeThrough;
            renderer.Item.IsDevidedPerCharacter = parameter.SplitByCharacter;
            renderer.Item.Decorations = decorations;

            var image = renderer.Update(desc);

            var bounds = dc.GetImageLocalBounds(image);
            float w = Math.Max(0, bounds.Right - bounds.Left);
            float h = Math.Max(0, bounds.Bottom - bounds.Top);
            lineImages.Add((image, w, h));
        }

        if (lineImages.Count == 0)
        {
            compositeCommandList?.Dispose();
            var empty = dc.CreateCommandList();
            dc.Target = empty;
            dc.BeginDraw();
            dc.Clear(null);
            dc.EndDraw();
            dc.Target = null;
            empty.Close();
            compositeCommandList = empty;
            Output = empty;
            return;
        }

        float totalWidth = 0;
        float totalHeight = 0;
        foreach (var (_, w, h) in lineImages)
        {
            totalWidth = Math.Max(totalWidth, w);
            totalHeight += h;
        }

        var (horizontalAlign, verticalAlign) = GetAlignment(parameter.BasePoint);
        var (originX, originY) = GetOrigin(horizontalAlign, verticalAlign, totalWidth, totalHeight);

        compositeCommandList?.Dispose();
        var newCommandList = dc.CreateCommandList();
        dc.Target = newCommandList;
        dc.BeginDraw();
        dc.Clear(null);

        float y = -originY;
        foreach (var (image, w, h) in lineImages)
        {
            float x = horizontalAlign switch
            {
                0 => -originX,                       // 左揃え
                2 => -originX + (totalWidth - w),     // 右揃え
                _ => -originX + (totalWidth - w) / 2, // 中央揃え
            };
            dc.DrawImage(image, new Vector2(x, y), null, InterpolationMode.Linear, CompositeMode.SourceOver);
            y += h;
        }

        dc.EndDraw();
        dc.Target = null;
        newCommandList.Close();

        compositeCommandList = newCommandList;
        Output = newCommandList;
    }

    /// <summary>
    /// BasePoint(18方位。縦書き用のVはこちらでは横書き相当として近似)から
    /// 水平方向(0=左,1=中央,2=右)・垂直方向(0=上,1=中央,2=下)の揃えを取り出す。
    /// </summary>
    private static (int horizontal, int vertical) GetAlignment(YukkuriMovieMaker.Project.Items.BasePoint basePoint)
    {
        int h = basePoint switch
        {
            YukkuriMovieMaker.Project.Items.BasePoint.LeftTop or
            YukkuriMovieMaker.Project.Items.BasePoint.LeftCenter or
            YukkuriMovieMaker.Project.Items.BasePoint.LeftBottom or
            YukkuriMovieMaker.Project.Items.BasePoint.VTopLeft or
            YukkuriMovieMaker.Project.Items.BasePoint.VCenterLeft or
            YukkuriMovieMaker.Project.Items.BasePoint.VBottomLeft => 0,
            YukkuriMovieMaker.Project.Items.BasePoint.RightTop or
            YukkuriMovieMaker.Project.Items.BasePoint.RightCenter or
            YukkuriMovieMaker.Project.Items.BasePoint.RightBottom or
            YukkuriMovieMaker.Project.Items.BasePoint.VTopRight or
            YukkuriMovieMaker.Project.Items.BasePoint.VCenterRight or
            YukkuriMovieMaker.Project.Items.BasePoint.VBottomRight => 2,
            _ => 1,
        };
        int v = basePoint switch
        {
            YukkuriMovieMaker.Project.Items.BasePoint.LeftTop or
            YukkuriMovieMaker.Project.Items.BasePoint.CenterTop or
            YukkuriMovieMaker.Project.Items.BasePoint.RightTop or
            YukkuriMovieMaker.Project.Items.BasePoint.VTopLeft or
            YukkuriMovieMaker.Project.Items.BasePoint.VTopCenter or
            YukkuriMovieMaker.Project.Items.BasePoint.VTopRight => 0,
            YukkuriMovieMaker.Project.Items.BasePoint.LeftBottom or
            YukkuriMovieMaker.Project.Items.BasePoint.CenterBottom or
            YukkuriMovieMaker.Project.Items.BasePoint.RightBottom or
            YukkuriMovieMaker.Project.Items.BasePoint.VBottomLeft or
            YukkuriMovieMaker.Project.Items.BasePoint.VBottomCenter or
            YukkuriMovieMaker.Project.Items.BasePoint.VBottomRight => 2,
            _ => 1,
        };
        return (h, v);
    }

    private static (float x, float y) GetOrigin(int horizontal, int vertical, float width, float height)
    {
        float x = horizontal switch { 0 => 0, 2 => width, _ => width / 2f };
        float y = vertical switch { 0 => 0, 2 => height, _ => height / 2f };
        return (x, y);
    }

    /// <summary>
    /// 各単位(日/時/分/秒/小数秒)について、その行内の文字範囲(Start/Length、
    /// その行単体のテキストを基準とする)に対するTextDecorationを常に作る。
    /// 「個別設定(CustomStyleEnabled)」がONの単位はその単位専用の設定値を、
    /// OFFの単位は文字グループ(既定)の設定値を、それぞれTextDecorationに詰める。
    ///
    /// 【サイズについて】個別設定の「サイズ」は文字グループのサイズに対する割合(%)として扱う
    /// (既定値100 = グループと同じサイズ)。OFFの場合はグループのサイズをそのまま使うので
    /// Scale=1.0(等倍)になる。
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
        bool useCustom;

        switch (unit)
        {
            case TimerPlusUnitKind.Day:
                useCustom = parameter.DayCustomStyleEnabled;
                font = useCustom ? parameter.DayFont : parameter.Font;
                fontSizeAnim = useCustom ? parameter.DayFontSize : null!;
                fontColor = useCustom ? parameter.DayFontColor : parameter.FontColor;
                bold = useCustom ? parameter.DayBold : parameter.Bold;
                italic = useCustom ? parameter.DayItalic : parameter.Italic;
                underline = useCustom ? parameter.DayUnderline : parameter.Underline;
                strike = useCustom ? parameter.DayStrikeThrough : parameter.StrikeThrough;
                style = useCustom ? parameter.DayStyle : parameter.Style;
                styleColor = useCustom ? parameter.DayStyleColor : parameter.StyleColor;
                break;
            case TimerPlusUnitKind.Hour:
                useCustom = parameter.HourCustomStyleEnabled;
                font = useCustom ? parameter.HourFont : parameter.Font;
                fontSizeAnim = useCustom ? parameter.HourFontSize : null!;
                fontColor = useCustom ? parameter.HourFontColor : parameter.FontColor;
                bold = useCustom ? parameter.HourBold : parameter.Bold;
                italic = useCustom ? parameter.HourItalic : parameter.Italic;
                underline = useCustom ? parameter.HourUnderline : parameter.Underline;
                strike = useCustom ? parameter.HourStrikeThrough : parameter.StrikeThrough;
                style = useCustom ? parameter.HourStyle : parameter.Style;
                styleColor = useCustom ? parameter.HourStyleColor : parameter.StyleColor;
                break;
            case TimerPlusUnitKind.Minute:
                useCustom = parameter.MinuteCustomStyleEnabled;
                font = useCustom ? parameter.MinuteFont : parameter.Font;
                fontSizeAnim = useCustom ? parameter.MinuteFontSize : null!;
                fontColor = useCustom ? parameter.MinuteFontColor : parameter.FontColor;
                bold = useCustom ? parameter.MinuteBold : parameter.Bold;
                italic = useCustom ? parameter.MinuteItalic : parameter.Italic;
                underline = useCustom ? parameter.MinuteUnderline : parameter.Underline;
                strike = useCustom ? parameter.MinuteStrikeThrough : parameter.StrikeThrough;
                style = useCustom ? parameter.MinuteStyle : parameter.Style;
                styleColor = useCustom ? parameter.MinuteStyleColor : parameter.StyleColor;
                break;
            case TimerPlusUnitKind.Second:
                useCustom = parameter.SecondCustomStyleEnabled;
                font = useCustom ? parameter.SecondFont : parameter.Font;
                fontSizeAnim = useCustom ? parameter.SecondFontSize : null!;
                fontColor = useCustom ? parameter.SecondFontColor : parameter.FontColor;
                bold = useCustom ? parameter.SecondBold : parameter.Bold;
                italic = useCustom ? parameter.SecondItalic : parameter.Italic;
                underline = useCustom ? parameter.SecondUnderline : parameter.Underline;
                strike = useCustom ? parameter.SecondStrikeThrough : parameter.StrikeThrough;
                style = useCustom ? parameter.SecondStyle : parameter.Style;
                styleColor = useCustom ? parameter.SecondStyleColor : parameter.StyleColor;
                break;
            case TimerPlusUnitKind.Fraction:
                useCustom = parameter.FractionCustomStyleEnabled;
                font = useCustom ? parameter.FractionFont : parameter.Font;
                fontSizeAnim = useCustom ? parameter.FractionFontSize : null!;
                fontColor = useCustom ? parameter.FractionFontColor : parameter.FontColor;
                bold = useCustom ? parameter.FractionBold : parameter.Bold;
                italic = useCustom ? parameter.FractionItalic : parameter.Italic;
                underline = useCustom ? parameter.FractionUnderline : parameter.Underline;
                strike = useCustom ? parameter.FractionStrikeThrough : parameter.StrikeThrough;
                style = useCustom ? parameter.FractionStyle : parameter.Style;
                styleColor = useCustom ? parameter.FractionStyleColor : parameter.StyleColor;
                break;
            default:
                return null;
        }

        double scale = useCustom ? fontSizeAnim.GetValue(frame, lengthFrames, fps) / 100.0 : 1.0;

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
        foreach (var r in lineRenderers) r.Dispose();
        compositeCommandList?.Dispose();
        errorCommandList?.Dispose();
        errorBrush?.Dispose();
    }

    /// <summary>
    /// 1行分の TextItem/TextSource(リフレクション経由。internal型のため)をまとめたヘルパー。
    /// </summary>
    private sealed class LineRenderer : IDisposable
    {
        public readonly TextItem Item = new();
        private readonly object textSource;
        private readonly MethodInfo updateMethod;
        private readonly PropertyInfo outputsProperty;

        public LineRenderer(IGraphicsDevicesAndContext devices)
        {
            var textSourceType = typeof(TextItem).Assembly.GetType("YukkuriMovieMaker.Player.Video.Items.TextSource")
                ?? throw new InvalidOperationException("TextSource type not found.");

            textSource = Activator.CreateInstance(textSourceType, devices, Item)
                ?? throw new InvalidOperationException("Failed to create TextSource instance.");

            updateMethod = textSourceType.GetMethod("Update")
                ?? throw new InvalidOperationException("Update method not found.");

            outputsProperty = textSourceType.GetProperty("Outputs")
                ?? throw new InvalidOperationException("Outputs property not found.");
        }

        public ID2D1Image Update(TimelineItemSourceDescription desc)
        {
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
                        return (ID2D1Image)outputProp.GetValue(firstOutput)!;
                    }
                }
            }

            throw new InvalidOperationException("TextSource.Outputs[0].Output を取得できませんでした。");
        }

        public void Dispose()
        {
            (textSource as IDisposable)?.Dispose();
        }
    }
}