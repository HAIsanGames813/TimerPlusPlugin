using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;

namespace TimerPlusPlugin;

/// <summary>Dayの「設定」表示グループ用の薄いプロキシ。実データはすべて親のTimerPlusShapeParameterのプロパティ(Set()経由)を直接読み書きする。</summary>
public sealed class DayDetailView : INotifyPropertyChanged
{
    private readonly TimerPlusShapeParameter parent;
    private readonly DayFontView fontView;

    public event PropertyChangedEventHandler? PropertyChanged;

    internal DayDetailView(TimerPlusShapeParameter parent)
    {
        this.parent = parent;
        fontView = new DayFontView(parent);
    }

    internal void RaiseFontEditorChanged() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FontEditor)));

    [Display(GroupName = "表示", Name = "前文字", Description = "数値の前に表示する文字列")]
    [TextEditor]
    public string Prefix { get => parent.DayPrefix; set => parent.DayPrefix = value; }

    [Display(GroupName = "表示", Name = "後文字", Description = "数値の後ろに表示する文字列")]
    [TextEditor]
    public string Suffix { get => parent.DaySuffix; set => parent.DaySuffix = value; }

    [Display(GroupName = "表示", Name = "桁数", Description = "表示する桁数")]
    [TextBoxSlider("F0", "", 1, 10)]
    [DefaultValue(2)]
    [Range(1, 10)]
    public int Digits { get => parent.DayDigits; set => parent.DayDigits = value; }

    [Display(GroupName = "表示", Name = "表示行", Description = "表示する行")]
    [TextBoxSlider("F0", "", 1, 5)]
    [DefaultValue(1)]
    [Range(1, 5)]
    public int Line { get => parent.DayLine; set => parent.DayLine = value; }

    [Display(GroupName = "表示", Name = "桁数固定", Description = "表示する桁数を固定")]
    [ToggleSlider]
    public bool FixedDigits { get => parent.DayFixedDigits; set => parent.DayFixedDigits = value; }

    [Display(GroupName = "表示", Name = "個別設定", Description = "この単位を個別")]
    [ToggleSlider]
    public bool CustomStyleEnabled { get => parent.DayCustomStyleEnabled; set => parent.DayCustomStyleEnabled = value; }

    [Display(GroupName = "表示", Name = "テキスト", Description = "", AutoGenerateField = true)]
    public DayFontView? FontEditor => CustomStyleEnabled ? fontView : null;
}

/// <summary>Dayの「テキスト」個別設定表示グループ用の薄いプロキシ。実データはすべて親のプロパティを直接読み書きする。</summary>
public sealed class DayFontView
{
    private readonly TimerPlusShapeParameter parent;

    internal DayFontView(TimerPlusShapeParameter parent)
    {
        this.parent = parent;
    }

    [Display(GroupName = "テキスト", Name = "X", AutoGenerateField = true)]
    [AnimationSlider("F1", "px", -500, 500)]
    public Animation OffsetX => parent.DayOffsetX;

    [Display(GroupName = "テキスト", Name = "Y", AutoGenerateField = true)]
    [AnimationSlider("F1", "px", -500, 500)]
    public Animation OffsetY => parent.DayOffsetY;

    [Display(GroupName = "テキスト", Name = "回転角", AutoGenerateField = true)]
    [AnimationSlider("F1", "°", -360, 360)]
    public Animation RotationAngle => parent.DayRotationAngle;

    [Display(GroupName = "テキスト", Name = "フォント")]
    [FontComboBox]
    public string Font { get => parent.DayFont; set => parent.DayFont = value; }

    [Display(GroupName = "テキスト", Name = "サイズ(px)", AutoGenerateField = true)]
    [AnimationSlider("F1", "px", 0, 100)]
    public Animation FontSize => parent.DayFontSize;

    [Display(GroupName = "テキスト", Name = "文字間隔", AutoGenerateField = true)]
    [AnimationSlider("F1", "px", -100, 100)]
    public Animation LetterSpacing => parent.DayLetterSpacing;

    [Display(GroupName = "テキスト", Name = "文字色")]
    [ColorPicker]
    public Color FontColor { get => parent.DayFontColor; set => parent.DayFontColor = value; }

    [Display(GroupName = "テキスト", Name = "太字")]
    [ToggleSlider]
    public bool Bold { get => parent.DayBold; set => parent.DayBold = value; }

    [Display(GroupName = "テキスト", Name = "イタリック")]
    [ToggleSlider]
    public bool Italic { get => parent.DayItalic; set => parent.DayItalic = value; }

    [Display(GroupName = "テキスト", Name = "下線")]
    [ToggleSlider]
    public bool Underline { get => parent.DayUnderline; set => parent.DayUnderline = value; }

    [Display(GroupName = "テキスト", Name = "打ち消し線")]
    [ToggleSlider]
    public bool StrikeThrough { get => parent.DayStrikeThrough; set => parent.DayStrikeThrough = value; }


    [Display(GroupName = "テキスト", Name = "装飾")]
    [EnumComboBox]
    public YukkuriMovieMaker.Project.Items.Style Style { get => parent.DayStyle; set => parent.DayStyle = value; }

    [Display(GroupName = "テキスト", Name = "装飾色")]
    [ColorPicker]
    public Color StyleColor { get => parent.DayStyleColor; set => parent.DayStyleColor = value; }

}

/// <summary>Hourの「設定」表示グループ用の薄いプロキシ。実データはすべて親のTimerPlusShapeParameterのプロパティ(Set()経由)を直接読み書きする。</summary>
public sealed class HourDetailView : INotifyPropertyChanged
{
    private readonly TimerPlusShapeParameter parent;
    private readonly HourFontView fontView;

    public event PropertyChangedEventHandler? PropertyChanged;

    internal HourDetailView(TimerPlusShapeParameter parent)
    {
        this.parent = parent;
        fontView = new HourFontView(parent);
    }

    internal void RaiseFontEditorChanged() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FontEditor)));

    [Display(GroupName = "表示", Name = "前文字", Description = "数値の前に表示する文字列")]
    [TextEditor]
    public string Prefix { get => parent.HourPrefix; set => parent.HourPrefix = value; }

    [Display(GroupName = "表示", Name = "後文字", Description = "数値の後ろに表示する文字列")]
    [TextEditor]
    public string Suffix { get => parent.HourSuffix; set => parent.HourSuffix = value; }

    [Display(GroupName = "表示", Name = "桁数", Description = "表示する桁数")]
    [TextBoxSlider("F0", "", 1, 10)]
    [DefaultValue(2)]
    [Range(1, 10)]
    public int Digits { get => parent.HourDigits; set => parent.HourDigits = value; }

    [Display(GroupName = "表示", Name = "表示行", Description = "表示する行")]
    [TextBoxSlider("F0", "", 1, 5)]
    [DefaultValue(1)]
    [Range(1, 5)]
    public int Line { get => parent.HourLine; set => parent.HourLine = value; }

    [Display(GroupName = "表示", Name = "桁数固定", Description = "表示する桁数を固定")]
    [ToggleSlider]
    public bool FixedDigits { get => parent.HourFixedDigits; set => parent.HourFixedDigits = value; }

    [Display(GroupName = "表示", Name = "個別設定", Description = "この単位を個別設定")]
    [ToggleSlider]
    public bool CustomStyleEnabled { get => parent.HourCustomStyleEnabled; set => parent.HourCustomStyleEnabled = value; }

    [Display(GroupName = "表示", Name = "文字", Description = "", AutoGenerateField = true)]
    public HourFontView? FontEditor => CustomStyleEnabled ? fontView : null;
}

/// <summary>Hourの「文字」個別設定表示グループ用の薄いプロキシ。実データはすべて親のプロパティを直接読み書きする。</summary>
public sealed class HourFontView
{
    private readonly TimerPlusShapeParameter parent;

    internal HourFontView(TimerPlusShapeParameter parent)
    {
        this.parent = parent;
    }

    [Display(GroupName = "テキスト", Name = "X", AutoGenerateField = true)]
    [AnimationSlider("F1", "px", -500, 500)]
    public Animation OffsetX => parent.HourOffsetX;

    [Display(GroupName = "テキスト", Name = "Y", AutoGenerateField = true)]
    [AnimationSlider("F1", "px", -500, 500)]
    public Animation OffsetY => parent.HourOffsetY;

    [Display(GroupName = "テキスト", Name = "回転角", AutoGenerateField = true)]
    [AnimationSlider("F1", "°", -360, 360)]
    public Animation RotationAngle => parent.HourRotationAngle;

    [Display(GroupName = "テキスト", Name = "フォント")]
    [FontComboBox]
    public string Font { get => parent.HourFont; set => parent.HourFont = value; }

    [Display(GroupName = "テキスト", Name = "サイズ(px)", AutoGenerateField = true)]
    [AnimationSlider("F1", "px", 0, 100)]
    public Animation FontSize => parent.HourFontSize;

    [Display(GroupName = "テキスト", Name = "文字間隔", AutoGenerateField = true)]
    [AnimationSlider("F1", "px", -100, 100)]
    public Animation LetterSpacing => parent.HourLetterSpacing;

    [Display(GroupName = "テキスト", Name = "文字色")]
    [ColorPicker]
    public Color FontColor { get => parent.HourFontColor; set => parent.HourFontColor = value; }

    [Display(GroupName = "テキスト", Name = "太字")]
    [ToggleSlider]
    public bool Bold { get => parent.HourBold; set => parent.HourBold = value; }

    [Display(GroupName = "テキスト", Name = "イタリック")]
    [ToggleSlider]
    public bool Italic { get => parent.HourItalic; set => parent.HourItalic = value; }

    [Display(GroupName = "テキスト", Name = "下線")]
    [ToggleSlider]
    public bool Underline { get => parent.HourUnderline; set => parent.HourUnderline = value; }

    [Display(GroupName = "テキスト", Name = "打ち消し線")]
    [ToggleSlider]
    public bool StrikeThrough { get => parent.HourStrikeThrough; set => parent.HourStrikeThrough = value; }

    [Display(GroupName = "テキスト", Name = "装飾")]
    [EnumComboBox]
    public YukkuriMovieMaker.Project.Items.Style Style { get => parent.HourStyle; set => parent.HourStyle = value; }

    [Display(GroupName = "テキスト", Name = "装飾色")]
    [ColorPicker]
    public Color StyleColor { get => parent.HourStyleColor; set => parent.HourStyleColor = value; }
}

/// <summary>Minuteの「設定」表示グループ用の薄いプロキシ。実データはすべて親のTimerPlusShapeParameterのプロパティ(Set()経由)を直接読み書きする。</summary>
public sealed class MinuteDetailView : INotifyPropertyChanged
{
    private readonly TimerPlusShapeParameter parent;
    private readonly MinuteFontView fontView;

    public event PropertyChangedEventHandler? PropertyChanged;

    internal MinuteDetailView(TimerPlusShapeParameter parent)
    {
        this.parent = parent;
        fontView = new MinuteFontView(parent);
    }

    internal void RaiseFontEditorChanged() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FontEditor)));

    [Display(GroupName = "表示", Name = "前文字", Description = "数値の前に表示する文字列")]
    [TextEditor]
    public string Prefix { get => parent.MinutePrefix; set => parent.MinutePrefix = value; }

    [Display(GroupName = "表示", Name = "後文字", Description = "数値の後ろに表示する文字列")]
    [TextEditor]
    public string Suffix { get => parent.MinuteSuffix; set => parent.MinuteSuffix = value; }

    [Display(GroupName = "表示", Name = "桁数", Description = "表示する桁数")]
    [TextBoxSlider("F0", "", 1, 10)]
    [DefaultValue(2)]
    [Range(1, 10)]
    public int Digits { get => parent.MinuteDigits; set => parent.MinuteDigits = value; }

    [Display(GroupName = "表示", Name = "表示行", Description = "表示する行")]
    [TextBoxSlider("F0", "", 1, 5)]
    [DefaultValue(1)]
    [Range(1, 5)]
    public int Line { get => parent.MinuteLine; set => parent.MinuteLine = value; }

    [Display(GroupName = "表示", Name = "桁数固定", Description = "表示する桁数を固定")]
    [ToggleSlider]
    public bool FixedDigits { get => parent.MinuteFixedDigits; set => parent.MinuteFixedDigits = value; }

    [Display(GroupName = "表示", Name = "個別設定", Description = "この単位を個別設定")]
    [ToggleSlider]
    public bool CustomStyleEnabled { get => parent.MinuteCustomStyleEnabled; set => parent.MinuteCustomStyleEnabled = value; }

    [Display(GroupName = "表示", Name = "文字", Description = "", AutoGenerateField = true)]
    public MinuteFontView? FontEditor => CustomStyleEnabled ? fontView : null;
}

/// <summary>Minuteの「文字」個別設定表示グループ用の薄いプロキシ。実データはすべて親のプロパティを直接読み書きする。</summary>
public sealed class MinuteFontView
{
    private readonly TimerPlusShapeParameter parent;

    internal MinuteFontView(TimerPlusShapeParameter parent)
    {
        this.parent = parent;
    }

    [Display(GroupName = "テキスト", Name = "X", AutoGenerateField = true)]
    [AnimationSlider("F1", "px", -500, 500)]
    public Animation OffsetX => parent.MinuteOffsetX;

    [Display(GroupName = "テキスト", Name = "Y", AutoGenerateField = true)]
    [AnimationSlider("F1", "px", -500, 500)]
    public Animation OffsetY => parent.MinuteOffsetY;


    [Display(GroupName = "テキスト", Name = "回転角", AutoGenerateField = true)]
    [AnimationSlider("F1", "°", -360, 360)]
    public Animation RotationAngle => parent.MinuteRotationAngle;

    [Display(GroupName = "テキスト", Name = "フォント")]
    [FontComboBox]
    public string Font { get => parent.MinuteFont; set => parent.MinuteFont = value; }

    [Display(GroupName = "テキスト", Name = "サイズ(px)", AutoGenerateField = true)]
    [AnimationSlider("F1", "px", 0, 100)]
    public Animation FontSize => parent.MinuteFontSize;

    [Display(GroupName = "テキスト", Name = "文字間隔", AutoGenerateField = true)]
    [AnimationSlider("F1", "px", -100, 100)]
    public Animation LetterSpacing => parent.MinuteLetterSpacing;

    [Display(GroupName = "テキスト", Name = "文字色")]
    [ColorPicker]
    public Color FontColor { get => parent.MinuteFontColor; set => parent.MinuteFontColor = value; }

    [Display(GroupName = "テキスト", Name = "太字")]
    [ToggleSlider]
    public bool Bold { get => parent.MinuteBold; set => parent.MinuteBold = value; }

    [Display(GroupName = "テキスト", Name = "イタリック")]
    [ToggleSlider]
    public bool Italic { get => parent.MinuteItalic; set => parent.MinuteItalic = value; }

    [Display(GroupName = "テキスト", Name = "下線")]
    [ToggleSlider]
    public bool Underline { get => parent.MinuteUnderline; set => parent.MinuteUnderline = value; }

    [Display(GroupName = "テキスト", Name = "打ち消し線")]
    [ToggleSlider]
    public bool StrikeThrough { get => parent.MinuteStrikeThrough; set => parent.MinuteStrikeThrough = value; }


    [Display(GroupName = "テキスト", Name = "装飾")]
    [EnumComboBox]
    public YukkuriMovieMaker.Project.Items.Style Style { get => parent.MinuteStyle; set => parent.MinuteStyle = value; }

    [Display(GroupName = "テキスト", Name = "装飾色")]
    [ColorPicker]
    public Color StyleColor { get => parent.MinuteStyleColor; set => parent.MinuteStyleColor = value; }
}

/// <summary>Secondの「設定」表示グループ用の薄いプロキシ。実データはすべて親のTimerPlusShapeParameterのプロパティ(Set()経由)を直接読み書きする。</summary>
public sealed class SecondDetailView : INotifyPropertyChanged
{
    private readonly TimerPlusShapeParameter parent;
    private readonly SecondFontView fontView;

    public event PropertyChangedEventHandler? PropertyChanged;

    internal SecondDetailView(TimerPlusShapeParameter parent)
    {
        this.parent = parent;
        fontView = new SecondFontView(parent);
    }

    internal void RaiseFontEditorChanged() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FontEditor)));

    [Display(GroupName = "表示", Name = "前文字", Description = "数値の前に表示する文字列")]
    [TextEditor]
    public string Prefix { get => parent.SecondPrefix; set => parent.SecondPrefix = value; }

    [Display(GroupName = "表示", Name = "後文字", Description = "数値の後ろに表示する文字列")]
    [TextEditor]
    public string Suffix { get => parent.SecondSuffix; set => parent.SecondSuffix = value; }

    [Display(GroupName = "表示", Name = "桁数", Description = "表示する桁数")]
    [TextBoxSlider("F0", "", 1, 10)]
    [DefaultValue(2)]
    [Range(1, 10)]
    public int Digits { get => parent.SecondDigits; set => parent.SecondDigits = value; }

    [Display(GroupName = "表示", Name = "表示行", Description = "表示する行")]
    [TextBoxSlider("F0", "", 1, 5)]
    [DefaultValue(1)]
    [Range(1, 5)]
    public int Line { get => parent.SecondLine; set => parent.SecondLine = value; }

    [Display(GroupName = "表示", Name = "桁数固定", Description = "表示する桁数を固定")]
    [ToggleSlider]
    public bool FixedDigits { get => parent.SecondFixedDigits; set => parent.SecondFixedDigits = value; }

    [Display(GroupName = "表示", Name = "個別設定", Description = "この単位を個別設定")]
    [ToggleSlider]
    public bool CustomStyleEnabled { get => parent.SecondCustomStyleEnabled; set => parent.SecondCustomStyleEnabled = value; }

    [Display(GroupName = "表示", Name = "文字", Description = "この単位専用のフォント/色/装飾", AutoGenerateField = true)]
    public SecondFontView? FontEditor => CustomStyleEnabled ? fontView : null;
}

/// <summary>Secondの「文字」個別設定表示グループ用の薄いプロキシ。実データはすべて親のプロパティを直接読み書きする。</summary>
public sealed class SecondFontView
{
    private readonly TimerPlusShapeParameter parent;

    internal SecondFontView(TimerPlusShapeParameter parent)
    {
        this.parent = parent;
    }

    [Display(GroupName = "テキスト", Name = "X", AutoGenerateField = true)]
    [AnimationSlider("F1", "px", -500, 500)]
    public Animation OffsetX => parent.SecondOffsetX;

    [Display(GroupName = "テキスト", Name = "Y", AutoGenerateField = true)]
    [AnimationSlider("F1", "px", -500, 500)]
    public Animation OffsetY => parent.SecondOffsetY;


    [Display(GroupName = "テキスト", Name = "回転角", AutoGenerateField = true)]
    [AnimationSlider("F1", "°", -360, 360)]
    public Animation RotationAngle => parent.SecondRotationAngle;

    [Display(GroupName = "テキスト", Name = "フォント")]
    [FontComboBox]
    public string Font { get => parent.SecondFont; set => parent.SecondFont = value; }

    [Display(GroupName = "テキスト", Name = "サイズ(px)", AutoGenerateField = true)]
    [AnimationSlider("F1", "px", 0, 100)]
    public Animation FontSize => parent.SecondFontSize;

    [Display(GroupName = "テキスト", Name = "文字間隔", AutoGenerateField = true)]
    [AnimationSlider("F1", "px", -100, 100)]
    public Animation LetterSpacing => parent.SecondLetterSpacing;

    [Display(GroupName = "テキスト", Name = "文字色")]
    [ColorPicker]
    public Color FontColor { get => parent.SecondFontColor; set => parent.SecondFontColor = value; }

    [Display(GroupName = "テキスト", Name = "太字")]
    [ToggleSlider]
    public bool Bold { get => parent.SecondBold; set => parent.SecondBold = value; }

    [Display(GroupName = "テキスト", Name = "イタリック")]
    [ToggleSlider]
    public bool Italic { get => parent.SecondItalic; set => parent.SecondItalic = value; }

    [Display(GroupName = "テキスト", Name = "下線")]
    [ToggleSlider]
    public bool Underline { get => parent.SecondUnderline; set => parent.SecondUnderline = value; }

    [Display(GroupName = "テキスト", Name = "打ち消し線")]
    [ToggleSlider]
    public bool StrikeThrough { get => parent.SecondStrikeThrough; set => parent.SecondStrikeThrough = value; }


    [Display(GroupName = "テキスト", Name = "装飾")]
    [EnumComboBox]
    public YukkuriMovieMaker.Project.Items.Style Style { get => parent.SecondStyle; set => parent.SecondStyle = value; }

    [Display(GroupName = "テキスト", Name = "装飾色")]
    [ColorPicker]
    public Color StyleColor { get => parent.SecondStyleColor; set => parent.SecondStyleColor = value; }
}

/// <summary>Fractionの「設定」表示グループ用の薄いプロキシ。実データはすべて親のTimerPlusShapeParameterのプロパティ(Set()経由)を直接読み書きする。</summary>
public sealed class FractionDetailView : INotifyPropertyChanged
{
    private readonly TimerPlusShapeParameter parent;
    private readonly FractionFontView fontView;

    public event PropertyChangedEventHandler? PropertyChanged;

    internal FractionDetailView(TimerPlusShapeParameter parent)
    {
        this.parent = parent;
        fontView = new FractionFontView(parent);
    }

    internal void RaiseFontEditorChanged() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FontEditor)));

    [Display(GroupName = "表示", Name = "前文字", Description = "数値の前に表示する文字列")]
    [TextEditor]
    public string Prefix { get => parent.FractionPrefix; set => parent.FractionPrefix = value; }

    [Display(GroupName = "表示", Name = "後文字", Description = "数値の後ろに表示する文字列")]
    [TextEditor]
    public string Suffix { get => parent.FractionSuffix; set => parent.FractionSuffix = value; }

    [Display(GroupName = "表示", Name = "桁数", Description = "表示する桁数")]
    [TextBoxSlider("F0", "", 1, 10)]
    [DefaultValue(2)]
    [Range(1, 10)]
    public int Digits { get => parent.FractionDigits; set => parent.FractionDigits = value; }

    [Display(GroupName = "表示", Name = "表示行", Description = "表示する行")]
    [TextBoxSlider("F0", "", 1, 5)]
    [DefaultValue(1)]
    [Range(1, 5)]
    public int Line { get => parent.FractionLine; set => parent.FractionLine = value; }

    [Display(GroupName = "表示", Name = "桁数固定", Description = "表示する桁数を固定")]
    [ToggleSlider]
    public bool FixedDigits { get => parent.FractionFixedDigits; set => parent.FractionFixedDigits = value; }

    [Display(GroupName = "表示", Name = "個別設定", Description = "この単位を個別に設定")]
    [ToggleSlider]
    public bool CustomStyleEnabled { get => parent.FractionCustomStyleEnabled; set => parent.FractionCustomStyleEnabled = value; }

    [Display(GroupName = "表示", Name = "文字", Description = "", AutoGenerateField = true)]
    public FractionFontView? FontEditor => CustomStyleEnabled ? fontView : null;
}

/// <summary>Fractionの「文字」個別設定表示グループ用の薄いプロキシ。実データはすべて親のプロパティを直接読み書きする。</summary>
public sealed class FractionFontView
{
    private readonly TimerPlusShapeParameter parent;

    internal FractionFontView(TimerPlusShapeParameter parent)
    {
        this.parent = parent;
    }

    [Display(GroupName = "テキスト", Name = "X", AutoGenerateField = true)]
    [AnimationSlider("F1", "px", -500, 500)]
    public Animation OffsetX => parent.FractionOffsetX;

    [Display(GroupName = "テキスト", Name = "Y", AutoGenerateField = true)]
    [AnimationSlider("F1", "px", -500, 500)]
    public Animation OffsetY => parent.FractionOffsetY;

    [Display(GroupName = "テキスト", Name = "回転角", AutoGenerateField = true)]
    [AnimationSlider("F1", "°", -360, 360)]
    public Animation RotationAngle => parent.FractionRotationAngle;

    [Display(GroupName = "テキスト", Name = "フォント")]
    [FontComboBox]
    public string Font { get => parent.FractionFont; set => parent.FractionFont = value; }

    [Display(GroupName = "テキスト", Name = "サイズ(px)", AutoGenerateField = true)]
    [AnimationSlider("F1", "px", 0, 100)]
    public Animation FontSize => parent.FractionFontSize;

    [Display(GroupName = "テキスト", Name = "文字間隔", AutoGenerateField = true)]
    [AnimationSlider("F1", "px", -100, 100)]
    public Animation LetterSpacing => parent.FractionLetterSpacing;

    [Display(GroupName = "テキスト", Name = "文字色")]
    [ColorPicker]
    public Color FontColor { get => parent.FractionFontColor; set => parent.FractionFontColor = value; }

    [Display(GroupName = "テキスト", Name = "太字")]
    [ToggleSlider]
    public bool Bold { get => parent.FractionBold; set => parent.FractionBold = value; }

    [Display(GroupName = "テキスト", Name = "イタリック")]
    [ToggleSlider]
    public bool Italic { get => parent.FractionItalic; set => parent.FractionItalic = value; }

    [Display(GroupName = "テキスト", Name = "下線")]
    [ToggleSlider]
    public bool Underline { get => parent.FractionUnderline; set => parent.FractionUnderline = value; }

    [Display(GroupName = "テキスト", Name = "打ち消し線")]
    [ToggleSlider]
    public bool StrikeThrough { get => parent.FractionStrikeThrough; set => parent.FractionStrikeThrough = value; }


    [Display(GroupName = "テキスト", Name = "装飾")]
    [EnumComboBox]
    public YukkuriMovieMaker.Project.Items.Style Style { get => parent.FractionStyle; set => parent.FractionStyle = value; }

    [Display(GroupName = "テキスト", Name = "装飾色")]
    [ColorPicker]
    public Color StyleColor { get => parent.FractionStyleColor; set => parent.FractionStyleColor = value; }
}