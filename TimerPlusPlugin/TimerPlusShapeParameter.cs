using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Exo;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin.Shape;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;

namespace TimerPlusPlugin;

public class TimerPlusShapeParameter : ShapeParameterBase
{
    public TimerPlusShapeParameter(SharedDataStore? sharedData) : base(sharedData)
    {
        dayUnitView = new DayUnitView(this);
        hourUnitView = new HourUnitView(this);
        minuteUnitView = new MinuteUnitView(this);
        secondUnitView = new SecondUnitView(this);
        fractionUnitView = new FractionUnitView(this);
    }

    [Obsolete]
    public TimerPlusShapeParameter() : this(null)
    {
    }

    [Display(GroupName = "", Name = "書式", Description = "表示形式を選択します。「カスタム」を選ぶと下の単位ごとの項目が有効になります")]
    [EnumComboBox]
    public TimerPlusFormat Format
    {
        get => field;
        set => Set(ref field, value, etcChangedPropertyNames: [nameof(Day), nameof(Hour), nameof(Minute), nameof(Second), nameof(Fraction)]);
    } = TimerPlusFormat.MMSSFF;

    [Display(GroupName = "", Name = "モード", Description = "カウントアップ/カウントダウン")]
    [EnumComboBox]
    public TimerPlusCountDirection Direction { get => field; set => Set(ref field, value); } = TimerPlusCountDirection.CountUp;

    [Display(GroupName = "", Name = "初期値", Description = "カウントの基準となる時間(00:00:00.00の形で入力してください)")]
    [TimeSpanTextEditor]
    [TimeSpanDefaultValue]
    [TimeSpanRange]
    public TimeSpan InitialTime { get => field; set => Set(ref field, value); } = TimeSpan.Zero;

    [Display(GroupName = "", Name = "初期値オフセット", Description = "初期値に加算される、イージング対応のオフセット(秒)", AutoGenerateField = true)]
    [AnimationSlider("F2", "秒", -60, 60)]
    public Animation InitialValueOffset { get; } = new Animation(0.0, -2147483648.0, 2147483647.0);

    [Display(GroupName = "", Name = "速度", Description = "タイマーの再生速度を変更できます", AutoGenerateField = true)]
    [AnimationSlider("F1", "%", -100, 100)]
    public Animation PlaybackRate { get; } = new Animation(100.0, -100000.0, 100000.0);

    [Display(GroupName = "", Name = "初期値反転", Description = "初期値を終了値として扱うようにします")]
    [ToggleSlider]
    public bool IsInitialValueReversed { get => field; set => Set(ref field, value); } = false;

    public bool DayEnabled
    {
        get => field;
        set { if (Set(ref field, value)) dayUnitView.RaiseDetailChanged(); }
    } = false;

    [Display(GroupName = "", Name = "日", Description = "書式が「カスタム」のときのみ表示されます", AutoGenerateField = true)]
    public DayUnitView? Day => Format == TimerPlusFormat.Custom ? dayUnitView : null;

    // 以下は実データ。UI表示は dayUnitView 配下のプロキシ側の同名プロパティで行うため、
    // ここには表示用属性を付けない(付けると書式より上に常時表示の二重項目ができてしまう)。
    public string DayPrefix { get => field; set => Set(ref field, value); } = "";
    public string DaySuffix { get => field; set => Set(ref field, value); } = ":";
    public int DayDigits { get => field; set => Set(ref field, value); } = 2;
    public int DayLine { get => field; set => Set(ref field, value); } = 1;

    public bool DayFixedDigits { get => field; set => Set(ref field, value); } = false;

    public bool DayCustomStyleEnabled
    {
        get => field;
        set { if (Set(ref field, value)) dayUnitView.DetailViewInternal.RaiseFontEditorChanged(); }
    } = false;

    public string DayFont { get => field; set => Set(ref field, value); } = "メイリオ";
    public Animation DayFontSize { get; } = new Animation(100.0, 0.0, 100000.0);
    public Color DayFontColor { get => field; set => Set(ref field, value); } = Colors.White;
    public bool DayBold { get => field; set => Set(ref field, value); } = false;
    public bool DayItalic { get => field; set => Set(ref field, value); } = false;
    public bool DayUnderline { get => field; set => Set(ref field, value); } = false;
    public bool DayStrikeThrough { get => field; set => Set(ref field, value); } = false;
    public YukkuriMovieMaker.Project.Items.Style DayStyle { get => field; set => Set(ref field, value); } = YukkuriMovieMaker.Project.Items.Style.Normal;
    public Color DayStyleColor { get => field; set => Set(ref field, value); } = Colors.Black;
    public Animation DayOffsetX { get; } = new Animation(0.0, -100000.0, 100000.0);
    public Animation DayOffsetY { get; } = new Animation(0.0, -100000.0, 100000.0);
    public Animation DayRotationAngle { get; } = new Animation(0.0, -100000.0, 100000.0);
    public Animation DayLetterSpacing { get; } = new Animation(0.0, -100000.0, 100000.0);

    private readonly DayUnitView dayUnitView;

    public bool HourEnabled
    {
        get => field;
        set { if (Set(ref field, value)) hourUnitView.RaiseDetailChanged(); }
    } = false;

    [Display(GroupName = "", Name = "時", Description = "書式が「カスタム」のときのみ表示されます", AutoGenerateField = true)]
    public HourUnitView? Hour => Format == TimerPlusFormat.Custom ? hourUnitView : null;

    // 以下は実データ。UI表示は hourUnitView 配下のプロキシ側の同名プロパティで行うため、
    // ここには表示用属性を付けない(付けると書式より上に常時表示の二重項目ができてしまう)。
    public string HourPrefix { get => field; set => Set(ref field, value); } = "";
    public string HourSuffix { get => field; set => Set(ref field, value); } = ":";
    public int HourDigits { get => field; set => Set(ref field, value); } = 2;
    public int HourLine { get => field; set => Set(ref field, value); } = 1;

    public bool HourFixedDigits { get => field; set => Set(ref field, value); } = false;

    public bool HourCustomStyleEnabled
    {
        get => field;
        set { if (Set(ref field, value)) hourUnitView.DetailViewInternal.RaiseFontEditorChanged(); }
    } = false;

    public string HourFont { get => field; set => Set(ref field, value); } = "メイリオ";
    public Animation HourFontSize { get; } = new Animation(100.0, 0.0, 100000.0);
    public Color HourFontColor { get => field; set => Set(ref field, value); } = Colors.White;
    public bool HourBold { get => field; set => Set(ref field, value); } = false;
    public bool HourItalic { get => field; set => Set(ref field, value); } = false;
    public bool HourUnderline { get => field; set => Set(ref field, value); } = false;
    public bool HourStrikeThrough { get => field; set => Set(ref field, value); } = false;
    public YukkuriMovieMaker.Project.Items.Style HourStyle { get => field; set => Set(ref field, value); } = YukkuriMovieMaker.Project.Items.Style.Normal;
    public Color HourStyleColor { get => field; set => Set(ref field, value); } = Colors.Black;
    public Animation HourOffsetX { get; } = new Animation(0.0, -100000.0, 100000.0);
    public Animation HourOffsetY { get; } = new Animation(0.0, -100000.0, 100000.0);
    public Animation HourRotationAngle { get; } = new Animation(0.0, -100000.0, 100000.0);
    public Animation HourLetterSpacing { get; } = new Animation(0.0, -100000.0, 100000.0);

    private readonly HourUnitView hourUnitView;

    public bool MinuteEnabled
    {
        get => field;
        set { if (Set(ref field, value)) minuteUnitView.RaiseDetailChanged(); }
    } = true;

    [Display(GroupName = "", Name = "分", Description = "書式が「カスタム」のときのみ表示されます", AutoGenerateField = true)]
    public MinuteUnitView? Minute => Format == TimerPlusFormat.Custom ? minuteUnitView : null;

    // 以下は実データ。UI表示は minuteUnitView 配下のプロキシ側の同名プロパティで行うため、
    // ここには表示用属性を付けない(付けると書式より上に常時表示の二重項目ができてしまう)。
    public string MinutePrefix { get => field; set => Set(ref field, value); } = "";
    public string MinuteSuffix { get => field; set => Set(ref field, value); } = ":";
    public int MinuteDigits { get => field; set => Set(ref field, value); } = 2;
    public int MinuteLine { get => field; set => Set(ref field, value); } = 1;

    public bool MinuteFixedDigits { get => field; set => Set(ref field, value); } = false;

    public bool MinuteCustomStyleEnabled
    {
        get => field;
        set { if (Set(ref field, value)) minuteUnitView.DetailViewInternal.RaiseFontEditorChanged(); }
    } = false;

    public string MinuteFont { get => field; set => Set(ref field, value); } = "メイリオ";
    public Animation MinuteFontSize { get; } = new Animation(100.0, 0.0, 100000.0);
    public Color MinuteFontColor { get => field; set => Set(ref field, value); } = Colors.White;
    public bool MinuteBold { get => field; set => Set(ref field, value); } = false;
    public bool MinuteItalic { get => field; set => Set(ref field, value); } = false;
    public bool MinuteUnderline { get => field; set => Set(ref field, value); } = false;
    public bool MinuteStrikeThrough { get => field; set => Set(ref field, value); } = false;
    public YukkuriMovieMaker.Project.Items.Style MinuteStyle { get => field; set => Set(ref field, value); } = YukkuriMovieMaker.Project.Items.Style.Normal;
    public Color MinuteStyleColor { get => field; set => Set(ref field, value); } = Colors.Black;
    public Animation MinuteOffsetX { get; } = new Animation(0.0, -100000.0, 100000.0);
    public Animation MinuteOffsetY { get; } = new Animation(0.0, -100000.0, 100000.0);
    public Animation MinuteRotationAngle { get; } = new Animation(0.0, -100000.0, 100000.0);
    public Animation MinuteLetterSpacing { get; } = new Animation(0.0, -100000.0, 100000.0);

    private readonly MinuteUnitView minuteUnitView;

    public bool SecondEnabled
    {
        get => field;
        set { if (Set(ref field, value)) secondUnitView.RaiseDetailChanged(); }
    } = true;

    [Display(GroupName = "", Name = "秒", Description = "書式が「カスタム」のときのみ表示されます", AutoGenerateField = true)]
    public SecondUnitView? Second => Format == TimerPlusFormat.Custom ? secondUnitView : null;

    // 以下は実データ。UI表示は secondUnitView 配下のプロキシ側の同名プロパティで行うため、
    // ここには表示用属性を付けない(付けると書式より上に常時表示の二重項目ができてしまう)。
    public string SecondPrefix { get => field; set => Set(ref field, value); } = "";
    public string SecondSuffix { get => field; set => Set(ref field, value); } = ".";
    public int SecondDigits { get => field; set => Set(ref field, value); } = 2;
    public int SecondLine { get => field; set => Set(ref field, value); } = 1;

    public bool SecondFixedDigits { get => field; set => Set(ref field, value); } = false;

    public bool SecondCustomStyleEnabled
    {
        get => field;
        set { if (Set(ref field, value)) secondUnitView.DetailViewInternal.RaiseFontEditorChanged(); }
    } = false;

    public string SecondFont { get => field; set => Set(ref field, value); } = "メイリオ";
    public Animation SecondFontSize { get; } = new Animation(100.0, 0.0, 100000.0);
    public Color SecondFontColor { get => field; set => Set(ref field, value); } = Colors.White;
    public bool SecondBold { get => field; set => Set(ref field, value); } = false;
    public bool SecondItalic { get => field; set => Set(ref field, value); } = false;
    public bool SecondUnderline { get => field; set => Set(ref field, value); } = false;
    public bool SecondStrikeThrough { get => field; set => Set(ref field, value); } = false;
    public YukkuriMovieMaker.Project.Items.Style SecondStyle { get => field; set => Set(ref field, value); } = YukkuriMovieMaker.Project.Items.Style.Normal;
    public Color SecondStyleColor { get => field; set => Set(ref field, value); } = Colors.Black;
    public Animation SecondOffsetX { get; } = new Animation(0.0, -100000.0, 100000.0);
    public Animation SecondOffsetY { get; } = new Animation(0.0, -100000.0, 100000.0);
    public Animation SecondRotationAngle { get; } = new Animation(0.0, -100000.0, 100000.0);
    public Animation SecondLetterSpacing { get; } = new Animation(0.0, -100000.0, 100000.0);

    private readonly SecondUnitView secondUnitView;

    public bool FractionEnabled
    {
        get => field;
        set { if (Set(ref field, value)) fractionUnitView.RaiseDetailChanged(); }
    } = true;

    [Display(GroupName = "", Name = "小数秒", Description = "書式が「カスタム」のときのみ表示されます", AutoGenerateField = true)]
    public FractionUnitView? Fraction => Format == TimerPlusFormat.Custom ? fractionUnitView : null;

    // 以下は実データ。UI表示は fractionUnitView 配下のプロキシ側の同名プロパティで行うため、
    // ここには表示用属性を付けない(付けると書式より上に常時表示の二重項目ができてしまう)。
    public TimerPlusFractionType FractionType { get => field; set => Set(ref field, value); } = TimerPlusFractionType.Decimal;
    public string FractionPrefix { get => field; set => Set(ref field, value); } = "";
    public string FractionSuffix { get => field; set => Set(ref field, value); } = "";
    public int FractionDigits { get => field; set => Set(ref field, value); } = 2;
    public int FractionLine { get => field; set => Set(ref field, value); } = 1;

    public bool FractionFixedDigits { get => field; set => Set(ref field, value); } = false;

    public bool FractionCustomStyleEnabled
    {
        get => field;
        set { if (Set(ref field, value)) fractionUnitView.DetailViewInternal.RaiseFontEditorChanged(); }
    } = false;

    public string FractionFont { get => field; set => Set(ref field, value); } = "メイリオ";
    public Animation FractionFontSize { get; } = new Animation(100.0, 0.0, 100000.0);
    public Color FractionFontColor { get => field; set => Set(ref field, value); } = Colors.White;
    public bool FractionBold { get => field; set => Set(ref field, value); } = false;
    public bool FractionItalic { get => field; set => Set(ref field, value); } = false;
    public bool FractionUnderline { get => field; set => Set(ref field, value); } = false;
    public bool FractionStrikeThrough { get => field; set => Set(ref field, value); } = false;
    public YukkuriMovieMaker.Project.Items.Style FractionStyle { get => field; set => Set(ref field, value); } = YukkuriMovieMaker.Project.Items.Style.Normal;
    public Color FractionStyleColor { get => field; set => Set(ref field, value); } = Colors.Black;
    public Animation FractionOffsetX { get; } = new Animation(0.0, -100000.0, 100000.0);
    public Animation FractionOffsetY { get; } = new Animation(0.0, -100000.0, 100000.0);
    public Animation FractionRotationAngle { get; } = new Animation(0.0, -100000.0, 100000.0);
    public Animation FractionLetterSpacing { get; } = new Animation(0.0, -100000.0, 100000.0);

    private readonly FractionUnitView fractionUnitView;

    [Display(GroupName = "テキスト", Name = "フォント")]
    [FontComboBox]
    public string Font { get => field; set => Set(ref field, value); } = "メイリオ";

    [Display(GroupName = "テキスト", Name = "サイズ", AutoGenerateField = true)]
    [AnimationSlider("F1", "px", 0, 100)]
    public Animation FontSize { get; } = new Animation(100.0, 0.0, 1000000.0);

    [Display(GroupName = "テキスト", Name = "文字間隔", AutoGenerateField = true)]
    [AnimationSlider("F1", "px", -100, 100)]
    public Animation LetterSpacing2 { get; } = new Animation(0.0, -100000.0, 100000.0);

    [Display(GroupName = "テキスト", Name = "文字揃え")]
    [EnumComboBox]
    public BasePoint BasePoint { get => field; set => Set(ref field, value); } = BasePoint.CenterCenter;

    [Display(GroupName = "テキスト", Name = "文字色")]
    [ColorPicker]
    public Color FontColor { get => field; set => Set(ref field, value); } = Colors.White;

    [Display(GroupName = "テキスト", Name = "装飾")]
    [EnumComboBox]
    public YukkuriMovieMaker.Project.Items.Style Style { get => field; set => Set(ref field, value); } = YukkuriMovieMaker.Project.Items.Style.Normal;

    [Display(GroupName = "テキスト", Name = "装飾色")]
    [ColorPicker]
    public Color StyleColor { get => field; set => Set(ref field, value); } = Colors.Black;

    [Display(GroupName = "テキスト", Name = "太字")]
    [ToggleSlider]
    public bool Bold { get => field; set => Set(ref field, value); } = false;

    [Display(GroupName = "テキスト", Name = "イタリック")]
    [ToggleSlider]
    public bool Italic { get => field; set => Set(ref field, value); } = false;

    [Display(GroupName = "テキスト", Name = "下線")]
    [ToggleSlider]
    public bool Underline { get => field; set => Set(ref field, value); } = false;

    [Display(GroupName = "テキスト", Name = "打ち消し線")]
    [ToggleSlider]
    public bool StrikeThrough { get => field; set => Set(ref field, value); } = false;


    public override IShapeSource CreateShapeSource(IGraphicsDevicesAndContext devices)
        => new TimerPlusShapeSource(devices, this);

    public override IEnumerable<string> CreateShapeItemExoFilter(int keyFrameIndex, ExoOutputDescription exoOutputDescription)
        => Array.Empty<string>();

    public override IEnumerable<string> CreateMaskExoFilter(int keyFrameIndex, ExoOutputDescription exoOutputDescription, ShapeMaskExoOutputDescription shapeMaskParameters)
        => Array.Empty<string>();

    protected override IEnumerable<IAnimatable> GetAnimatables() =>
    [
        InitialValueOffset, PlaybackRate, FontSize, LetterSpacing2,
            DayFontSize, DayOffsetX, DayOffsetY, DayRotationAngle, DayLetterSpacing,
            HourFontSize, HourOffsetX, HourOffsetY, HourRotationAngle, HourLetterSpacing,
            MinuteFontSize, MinuteOffsetX, MinuteOffsetY, MinuteRotationAngle, MinuteLetterSpacing,
            SecondFontSize, SecondOffsetX, SecondOffsetY, SecondRotationAngle, SecondLetterSpacing,
            FractionFontSize, FractionOffsetX, FractionOffsetY, FractionRotationAngle, FractionLetterSpacing,
    ];

    internal TimerPlusCustomSettings CreateCustomSettings() => new(
        new TimerPlusUnitSettings(DayEnabled, DayDigits, DayPrefix, DaySuffix, DayLine, DayFixedDigits, DayCustomStyleEnabled),
        new TimerPlusUnitSettings(HourEnabled, HourDigits, HourPrefix, HourSuffix, HourLine, HourFixedDigits, HourCustomStyleEnabled),
        new TimerPlusUnitSettings(MinuteEnabled, MinuteDigits, MinutePrefix, MinuteSuffix, MinuteLine, MinuteFixedDigits, MinuteCustomStyleEnabled),
        new TimerPlusUnitSettings(SecondEnabled, SecondDigits, SecondPrefix, SecondSuffix, SecondLine, SecondFixedDigits, SecondCustomStyleEnabled),
        new TimerPlusUnitSettings(FractionEnabled, FractionDigits, FractionPrefix, FractionSuffix, FractionLine, FractionFixedDigits, FractionCustomStyleEnabled),
        FractionType);

    protected override void LoadSharedData(SharedDataStore sharedData)
    {
    }

    protected override void SaveSharedData(SharedDataStore sharedData)
    {
    }

    public object CreateEditingData() => new TimerPlusShapeParameterData(this);

    public void SetEditingData(object editingData)
    {
        if (editingData is not TimerPlusShapeParameterData data) return;

        Font = data.Font;
        InitialValueOffset.CopyFrom(data.InitialValueOffset);
        PlaybackRate.CopyFrom(data.PlaybackRate);
        FontSize.CopyFrom(data.FontSize);
        LetterSpacing2.CopyFrom(data.LetterSpacing2);
        FontColor = data.FontColor;
        Style = data.Style;
        StyleColor = data.StyleColor;
        Bold = data.Bold;
        Italic = data.Italic;
        Underline = data.Underline;
        StrikeThrough = data.StrikeThrough;
        BasePoint = data.BasePoint;

        Format = data.Format;
        Direction = data.Direction;
        IsInitialValueReversed = data.IsInitialValueReversed;

        InitialTime = data.InitialTime;

        DayEnabled = data.DayEnabled;
        DayPrefix = data.DayPrefix;
        DaySuffix = data.DaySuffix;
        DayDigits = data.DayDigits;
        DayLine = data.DayLine;
        DayCustomStyleEnabled = data.DayCustomStyleEnabled;
        DayFont = data.DayFont;
        DayFontSize.CopyFrom(data.DayFontSize);
        DayFontColor = data.DayFontColor;
        DayBold = data.DayBold;
        DayItalic = data.DayItalic;
        DayUnderline = data.DayUnderline;
        DayStrikeThrough = data.DayStrikeThrough;
        DayFixedDigits = data.DayFixedDigits;
        DayStyle = data.DayStyle;
        DayStyleColor = data.DayStyleColor;
        DayOffsetX.CopyFrom(data.DayOffsetX);
        DayOffsetY.CopyFrom(data.DayOffsetY);
        DayRotationAngle.CopyFrom(data.DayRotationAngle);
        DayLetterSpacing.CopyFrom(data.DayLetterSpacing);

        HourEnabled = data.HourEnabled;
        HourPrefix = data.HourPrefix;
        HourSuffix = data.HourSuffix;
        HourDigits = data.HourDigits;
        HourLine = data.HourLine;
        HourCustomStyleEnabled = data.HourCustomStyleEnabled;
        HourFont = data.HourFont;
        HourFontSize.CopyFrom(data.HourFontSize);
        HourFontColor = data.HourFontColor;
        HourBold = data.HourBold;
        HourItalic = data.HourItalic;
        HourUnderline = data.HourUnderline;
        HourStrikeThrough = data.HourStrikeThrough;
        HourFixedDigits = data.HourFixedDigits;
        HourStyle = data.HourStyle;
        HourStyleColor = data.HourStyleColor;
        HourOffsetX.CopyFrom(data.HourOffsetX);
        HourOffsetY.CopyFrom(data.HourOffsetY);
        HourRotationAngle.CopyFrom(data.HourRotationAngle);
        HourLetterSpacing.CopyFrom(data.HourLetterSpacing);

        MinuteEnabled = data.MinuteEnabled;
        MinutePrefix = data.MinutePrefix;
        MinuteSuffix = data.MinuteSuffix;
        MinuteDigits = data.MinuteDigits;
        MinuteLine = data.MinuteLine;
        MinuteCustomStyleEnabled = data.MinuteCustomStyleEnabled;
        MinuteFont = data.MinuteFont;
        MinuteFontSize.CopyFrom(data.MinuteFontSize);
        MinuteFontColor = data.MinuteFontColor;
        MinuteBold = data.MinuteBold;
        MinuteItalic = data.MinuteItalic;
        MinuteUnderline = data.MinuteUnderline;
        MinuteStrikeThrough = data.MinuteStrikeThrough;
        MinuteFixedDigits = data.MinuteFixedDigits;
        MinuteStyle = data.MinuteStyle;
        MinuteStyleColor = data.MinuteStyleColor;
        MinuteOffsetX.CopyFrom(data.MinuteOffsetX);
        MinuteOffsetY.CopyFrom(data.MinuteOffsetY);
        MinuteRotationAngle.CopyFrom(data.MinuteRotationAngle);
        MinuteLetterSpacing.CopyFrom(data.MinuteLetterSpacing);

        SecondEnabled = data.SecondEnabled;
        SecondPrefix = data.SecondPrefix;
        SecondSuffix = data.SecondSuffix;
        SecondDigits = data.SecondDigits;
        SecondLine = data.SecondLine;
        SecondCustomStyleEnabled = data.SecondCustomStyleEnabled;
        SecondFont = data.SecondFont;
        SecondFontSize.CopyFrom(data.SecondFontSize);
        SecondFontColor = data.SecondFontColor;
        SecondBold = data.SecondBold;
        SecondItalic = data.SecondItalic;
        SecondUnderline = data.SecondUnderline;
        SecondStrikeThrough = data.SecondStrikeThrough;
        SecondFixedDigits = data.SecondFixedDigits;
        SecondStyle = data.SecondStyle;
        SecondStyleColor = data.SecondStyleColor;
        SecondOffsetX.CopyFrom(data.SecondOffsetX);
        SecondOffsetY.CopyFrom(data.SecondOffsetY);
        SecondRotationAngle.CopyFrom(data.SecondRotationAngle);
        SecondLetterSpacing.CopyFrom(data.SecondLetterSpacing);

        FractionEnabled = data.FractionEnabled;
        FractionType = data.FractionType;
        FractionPrefix = data.FractionPrefix;
        FractionSuffix = data.FractionSuffix;
        FractionDigits = data.FractionDigits;
        FractionLine = data.FractionLine;
        FractionCustomStyleEnabled = data.FractionCustomStyleEnabled;
        FractionFont = data.FractionFont;
        FractionFontSize.CopyFrom(data.FractionFontSize);
        FractionFontColor = data.FractionFontColor;
        FractionBold = data.FractionBold;
        FractionItalic = data.FractionItalic;
        FractionUnderline = data.FractionUnderline;
        FractionStrikeThrough = data.FractionStrikeThrough;
        FractionFixedDigits = data.FractionFixedDigits;
        FractionStyle = data.FractionStyle;
        FractionStyleColor = data.FractionStyleColor;
        FractionOffsetX.CopyFrom(data.FractionOffsetX);
        FractionOffsetY.CopyFrom(data.FractionOffsetY);
        FractionRotationAngle.CopyFrom(data.FractionRotationAngle);
        FractionLetterSpacing.CopyFrom(data.FractionLetterSpacing);

    }
}