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
/// タイマー+の実描画クラス。行ごとに独立した TextItem/TextSource でレンダリングし、縦に合成する。
/// </summary>
internal class TimerPlusShapeSource : IShapeSource
{
    private readonly IGraphicsDevicesAndContext devices;
    private readonly TimerPlusShapeParameter parameter;

    private readonly List<LineRenderer> lineRenderers = new();

    private bool errorLogged;
    private ID2D1SolidColorBrush? errorBrush;
    private ID2D1CommandList? errorCommandList;
    private ID2D1CommandList? compositeCommandList;

    private int cachedFrame = -1;
    private double cachedCumulativeRateSeconds;

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
        var lines = TimerPlusFormatter.FormatLines(counterTime, parameter.Format, parameter.CreateCustomSettings(), fps);

        // 行ごとに、「揃え計算用(オフセットなし)」と「実描画用(オフセットあり)」の2種類のDecorationsを構築
        var lineData = new List<(string text, ImmutableList<TextDecoration> measureDecorations, ImmutableList<TextDecoration> renderDecorations)>();
        foreach (var line in lines)
        {
            var sb = new StringBuilder();
            var measureDecorations = new List<TextDecoration>();
            var renderDecorations = new List<TextDecoration>();
            foreach (var segment in line.Segments)
            {
                int start = sb.Length;
                sb.Append(segment.Text);
                int len = segment.Text.Length;
                if (len == 0) continue;

                if (segment.Unit != TimerPlusUnitKind.Default)
                {
                    var measureD = BuildDecoration(segment.Unit, start, len, frame, length, fps, applyOffsets: false);
                    if (measureD != null) measureDecorations.Add(measureD);
                    var renderD = BuildDecoration(segment.Unit, start, len, frame, length, fps, applyOffsets: true);
                    if (renderD != null) renderDecorations.Add(renderD);
                }
            }
            string text = sb.Length == 0 ? " " : sb.ToString();
            lineData.Add((text, measureDecorations.ToImmutableList(), renderDecorations.ToImmutableList()));
        }

        // キャッシュによる差分判定はせず、毎回必ず再構築する(パラメーター変更が確実にプレビューへ反映されるようにするため)。
        if (lineData.Count == 1)
        {
            // 行が1つだけ(カスタム書式以外、またはカスタムでも1行にまとめている場合)は、
            // 自前で揃え計算をせず、TextItem自身のBasePoint処理にそのまま委ねる。
            // これにより既存の通常テキストアイテムと完全に同じ配置になる。
            RenderSingleLineNative(lineData[0], desc);
        }
        else
        {
            RenderComposite(lineData, desc, frame, length, fps);
        }
    }

    private void RenderSingleLineNative((string text, ImmutableList<TextDecoration> measureDecorations, ImmutableList<TextDecoration> renderDecorations) line, TimelineItemSourceDescription desc)
    {
        compositeCommandList?.Dispose();
        compositeCommandList = null;

        while (lineRenderers.Count < 1)
            lineRenderers.Add(new LineRenderer(devices));
        while (lineRenderers.Count > 1)
        {
            lineRenderers[^1].Dispose();
            lineRenderers.RemoveAt(lineRenderers.Count - 1);
        }

        var renderer = lineRenderers[0];
        renderer.Item.Text = line.text;
        renderer.Item.Font = parameter.Font;
        renderer.Item.FontSize.CopyFrom(parameter.FontSize);
        renderer.Item.LetterSpacing2.CopyFrom(parameter.LetterSpacing2);
        renderer.Item.BasePoint = parameter.BasePoint; // 揃え・縦書きはすべてTextItem自身に任せる
        renderer.Item.FontColor = parameter.FontColor;
        renderer.Item.Style = parameter.Style;
        renderer.Item.StyleColor = parameter.StyleColor;
        renderer.Item.Bold = parameter.Bold;
        renderer.Item.Italic = parameter.Italic;
        renderer.Item.Underline = parameter.Underline;
        renderer.Item.Strikethrough = parameter.StrikeThrough;
        renderer.Item.Decorations = line.renderDecorations;

        Output = renderer.Update(desc);
    }

    private void RenderComposite(List<(string text, ImmutableList<TextDecoration> measureDecorations, ImmutableList<TextDecoration> renderDecorations)> lineData, TimelineItemSourceDescription desc, int frame, int length, int fps)
    {
        // 行数分の LineRenderer を確保(過不足を作成/破棄)
        while (lineRenderers.Count < lineData.Count)
            lineRenderers.Add(new LineRenderer(devices));
        while (lineRenderers.Count > lineData.Count)
        {
            lineRenderers[^1].Dispose();
            lineRenderers.RemoveAt(lineRenderers.Count - 1);
        }

        var dc = devices.DeviceContext;

        void ApplyCommonItemProperties(LineRenderer renderer, string text)
        {
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
        }

        // 1パス目: オフセットなしのDecorationsで測定し、文字揃え(揃え位置)を先に決める。
        var measuredSizes = new List<(float width, float height)>();
        for (int i = 0; i < lineData.Count; i++)
        {
            var renderer = lineRenderers[i];
            var (text, measureDecorations, _) = lineData[i];

            ApplyCommonItemProperties(renderer, text);
            renderer.Item.Decorations = measureDecorations;

            var measureImage = renderer.Update(desc);
            var bounds = dc.GetImageLocalBounds(measureImage);
            float w = Math.Max(0, bounds.Right - bounds.Left);
            float h = Math.Max(0, bounds.Bottom - bounds.Top);
            measuredSizes.Add((w, h));
        }

        if (measuredSizes.Count == 0)
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
        foreach (var (w, h) in measuredSizes)
        {
            totalWidth = Math.Max(totalWidth, w);
            totalHeight += h;
        }

        var (horizontalAlign, verticalAlign) = GetAlignment(parameter.BasePoint);
        var (originX, originY) = GetOrigin(horizontalAlign, verticalAlign, totalWidth, totalHeight);

        // 2パス目: 個別設定のX/Y/回転角を含めたDecorationsで実際に描画する画像を作る。
        // 配置座標・行送りは1パス目で測定した(オフセットを含まない)サイズを使うため、
        // 個々の単位のズレが全体の揃え・行の積み上げに影響しない。
        var lineImages = new List<(ID2D1Image image, float width, float height)>();
        for (int i = 0; i < lineData.Count; i++)
        {
            var renderer = lineRenderers[i];
            var (text, _, renderDecorations) = lineData[i];

            ApplyCommonItemProperties(renderer, text);
            renderer.Item.Decorations = renderDecorations;

            var image = renderer.Update(desc);
            lineImages.Add((image, measuredSizes[i].width, measuredSizes[i].height));
        }

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

    /// <summary>BasePointから水平/垂直の揃えを取り出す。</summary>
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

    /// <summary>各単位の文字範囲に対するTextDecorationを作る(個別設定ONならその単位専用の値、OFFなら文字グループの値)。</summary>
    /// <summary>
    /// 各単位の文字範囲に対するTextDecorationを作る。
    /// applyOffsets=false: 文字揃え計算用の測定パス(位置/回転オフセットを含めない)。
    /// applyOffsets=true: 実描画パス(位置/回転オフセットを含める)。
    /// フォント/サイズ/色/装飾など見た目に関わる項目はどちらのパスでも同じ値を使う
    /// (測定パスの画像と実描画パスの画像でサイズが食い違わないようにするため)。
    /// </summary>
    private TextDecoration? BuildDecoration(TimerPlusUnitKind unit, int start, int length, int frame, int lengthFrames, int fps, bool applyOffsets)
    {
        bool useCustom;
        string customFont, groupFont;
        WpfColor customFontColor, customStyleColor;
        bool customBold, customItalic, customUnderline, customStrike;
        YukkuriMovieMaker.Project.Items.Style customStyle;
        Animation customFontSizeAnim, customOffsetX, customOffsetY, customRotationAngle, customLetterSpacing;

        switch (unit)
        {
            case TimerPlusUnitKind.Day:
                useCustom = parameter.DayCustomStyleEnabled;
                customFont = parameter.DayFont; customFontColor = parameter.DayFontColor;
                customBold = parameter.DayBold; customItalic = parameter.DayItalic;
                customUnderline = parameter.DayUnderline; customStrike = parameter.DayStrikeThrough;
                customStyle = parameter.DayStyle; customStyleColor = parameter.DayStyleColor;
                customFontSizeAnim = parameter.DayFontSize; customOffsetX = parameter.DayOffsetX;
                customOffsetY = parameter.DayOffsetY; customRotationAngle = parameter.DayRotationAngle;
                customLetterSpacing = parameter.DayLetterSpacing;
                break;
            case TimerPlusUnitKind.Hour:
                useCustom = parameter.HourCustomStyleEnabled;
                customFont = parameter.HourFont; customFontColor = parameter.HourFontColor;
                customBold = parameter.HourBold; customItalic = parameter.HourItalic;
                customUnderline = parameter.HourUnderline; customStrike = parameter.HourStrikeThrough;
                customStyle = parameter.HourStyle; customStyleColor = parameter.HourStyleColor;
                customFontSizeAnim = parameter.HourFontSize; customOffsetX = parameter.HourOffsetX;
                customOffsetY = parameter.HourOffsetY; customRotationAngle = parameter.HourRotationAngle;
                customLetterSpacing = parameter.HourLetterSpacing;
                break;
            case TimerPlusUnitKind.Minute:
                useCustom = parameter.MinuteCustomStyleEnabled;
                customFont = parameter.MinuteFont; customFontColor = parameter.MinuteFontColor;
                customBold = parameter.MinuteBold; customItalic = parameter.MinuteItalic;
                customUnderline = parameter.MinuteUnderline; customStrike = parameter.MinuteStrikeThrough;
                customStyle = parameter.MinuteStyle; customStyleColor = parameter.MinuteStyleColor;
                customFontSizeAnim = parameter.MinuteFontSize; customOffsetX = parameter.MinuteOffsetX;
                customOffsetY = parameter.MinuteOffsetY; customRotationAngle = parameter.MinuteRotationAngle;
                customLetterSpacing = parameter.MinuteLetterSpacing;
                break;
            case TimerPlusUnitKind.Second:
                useCustom = parameter.SecondCustomStyleEnabled;
                customFont = parameter.SecondFont; customFontColor = parameter.SecondFontColor;
                customBold = parameter.SecondBold; customItalic = parameter.SecondItalic;
                customUnderline = parameter.SecondUnderline; customStrike = parameter.SecondStrikeThrough;
                customStyle = parameter.SecondStyle; customStyleColor = parameter.SecondStyleColor;
                customFontSizeAnim = parameter.SecondFontSize; customOffsetX = parameter.SecondOffsetX;
                customOffsetY = parameter.SecondOffsetY; customRotationAngle = parameter.SecondRotationAngle;
                customLetterSpacing = parameter.SecondLetterSpacing;
                break;
            case TimerPlusUnitKind.Fraction:
                useCustom = parameter.FractionCustomStyleEnabled;
                customFont = parameter.FractionFont; customFontColor = parameter.FractionFontColor;
                customBold = parameter.FractionBold; customItalic = parameter.FractionItalic;
                customUnderline = parameter.FractionUnderline; customStrike = parameter.FractionStrikeThrough;
                customStyle = parameter.FractionStyle; customStyleColor = parameter.FractionStyleColor;
                customFontSizeAnim = parameter.FractionFontSize; customOffsetX = parameter.FractionOffsetX;
                customOffsetY = parameter.FractionOffsetY; customRotationAngle = parameter.FractionRotationAngle;
                customLetterSpacing = parameter.FractionLetterSpacing;
                break;
            default:
                return null;
        }

        groupFont = parameter.Font;
        string font = useCustom ? customFont : groupFont;
        WpfColor fontColor = useCustom ? customFontColor : parameter.FontColor;
        bool bold = useCustom ? customBold : parameter.Bold;
        bool italic = useCustom ? customItalic : parameter.Italic;
        bool underline = useCustom ? customUnderline : parameter.Underline;
        bool strike = useCustom ? customStrike : parameter.StrikeThrough;
        YukkuriMovieMaker.Project.Items.Style style = useCustom ? customStyle : parameter.Style;
        WpfColor styleColor = useCustom ? customStyleColor : parameter.StyleColor;

        // FontSizeはpx単位の絶対値なので、文字グループのpxとの比率をScaleとして渡す。
        double scale = 1.0;
        double? characterSpacing = null;
        if (useCustom)
        {
            double groupPx = parameter.FontSize.GetValue(frame, lengthFrames, fps);
            double unitPx = customFontSizeAnim.GetValue(frame, lengthFrames, fps);
            scale = groupPx > 0 ? unitPx / groupPx : 1.0;
            characterSpacing = customLetterSpacing.GetValue(frame, lengthFrames, fps);
        }

        // 位置(X/Y)・回転角は「文字揃え計算」の後に適用する見た目上の追加ズレとして扱うため、
        // 測定パス(applyOffsets=false)では常に0にする。
        double offsetX = 0, offsetY = 0, rotationAngle = 0;
        if (useCustom && applyOffsets)
        {
            offsetX = customOffsetX.GetValue(frame, lengthFrames, fps);
            offsetY = customOffsetY.GetValue(frame, lengthFrames, fps);
            rotationAngle = customRotationAngle.GetValue(frame, lengthFrames, fps);
        }

        return new TextDecoration(
            Start: start,
            Length: length,
            IsBold: bold,
            IsItalic: italic,
            Scale: scale,
            Font: font,
            Foreground: fontColor,
            IsLineBreak: false,
            OffsetX: offsetX,
            OffsetY: offsetY,
            IsAbsoluteX: false,
            IsAbsoluteY: false,
            DecorationColor: styleColor,
            RotationZ: rotationAngle,
            IsStrikethrough: strike,
            IsUnderline: underline,
            RotationGroupId: 0,
            PositionGroupId: 0,
            CharacterSpacing: characterSpacing,
            StyleType: (int)style);
    }

    private TimeSpan ComputeCounterTime(int frame, int length, int fps, TimeSpan duration)
    {
        double offset = parameter.InitialValueOffset.GetValue(frame, length, fps);
        double initialValue = parameter.InitialTime.TotalSeconds + offset;

        double cumulativeRateSeconds = GetCumulativeRateSeconds(frame, length, fps);

        bool isCountDown = parameter.Direction == TimerPlusCountDirection.CountDown;
        bool reversed = parameter.IsInitialValueReversed;

        // 通常: カウントダウンは初期値が終了値、カウントアップは初期値が開始値。反転時は入れ替わる。
        bool initialIsEnd = isCountDown != reversed;
        if (initialIsEnd)
            initialValue += isCountDown ? duration.TotalSeconds : -duration.TotalSeconds;

        // 増減の向き自体はDirectionのみで決まる(カウントダウンは常に減少、カウントアップは常に増加)。
        double seconds = initialValue + (isCountDown ? -cumulativeRateSeconds : cumulativeRateSeconds);
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