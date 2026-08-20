using System;
using System.Windows.Media;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Project.Items;

namespace TimerPlusPlugin;

public class TimerPlusShapeParameterData
{
    public TimerPlusFormat Format { get; set; }
    public TimerPlusCountDirection Direction { get; set; }
    public bool ReverseBehavior { get; set; }

    public TimeSpan InitialTime { get; set; }
    public int InitialValueBaseFrames { get; set; }

    public Animation InitialValueOffset { get; } = new Animation(0.0, -2147483648.0, 2147483647.0);
    public Animation PlaybackRate { get; } = new Animation(100.0, -100000.0, 100000.0);

    public bool DayEnabled { get; set; }
    public int DayDigits { get; set; }
    public string DayPrefix { get; set; } = "";
    public string DaySuffix { get; set; } = "";
    public int DayLine { get; set; }
    public bool DayCustomStyleEnabled { get; set; }
    public string DayFont { get; set; } = "";
    public Animation DayFontSize { get; } = new Animation(34.0, 1.0, 100000.0);
    public Color DayFontColor { get; set; }
    public bool DayBold { get; set; }
    public bool DayItalic { get; set; }
    public bool DayUnderline { get; set; }
    public bool DayStrikeThrough { get; set; }
    public bool DaySplitByCharacter { get; set; }
    public YukkuriMovieMaker.Project.Items.Style DayStyle { get; set; }
    public Color DayStyleColor { get; set; }

    public bool HourEnabled { get; set; }
    public int HourDigits { get; set; }
    public string HourPrefix { get; set; } = "";
    public string HourSuffix { get; set; } = "";
    public int HourLine { get; set; }
    public bool HourCustomStyleEnabled { get; set; }
    public string HourFont { get; set; } = "";
    public Animation HourFontSize { get; } = new Animation(34.0, 1.0, 100000.0);
    public Color HourFontColor { get; set; }
    public bool HourBold { get; set; }
    public bool HourItalic { get; set; }
    public bool HourUnderline { get; set; }
    public bool HourStrikeThrough { get; set; }
    public bool HourSplitByCharacter { get; set; }
    public YukkuriMovieMaker.Project.Items.Style HourStyle { get; set; }
    public Color HourStyleColor { get; set; }

    public bool MinuteEnabled { get; set; }
    public int MinuteDigits { get; set; }
    public string MinutePrefix { get; set; } = "";
    public string MinuteSuffix { get; set; } = "";
    public int MinuteLine { get; set; }
    public bool MinuteCustomStyleEnabled { get; set; }
    public string MinuteFont { get; set; } = "";
    public Animation MinuteFontSize { get; } = new Animation(34.0, 1.0, 100000.0);
    public Color MinuteFontColor { get; set; }
    public bool MinuteBold { get; set; }
    public bool MinuteItalic { get; set; }
    public bool MinuteUnderline { get; set; }
    public bool MinuteStrikeThrough { get; set; }
    public bool MinuteSplitByCharacter { get; set; }
    public YukkuriMovieMaker.Project.Items.Style MinuteStyle { get; set; }
    public Color MinuteStyleColor { get; set; }

    public bool SecondEnabled { get; set; }
    public int SecondDigits { get; set; }
    public string SecondPrefix { get; set; } = "";
    public string SecondSuffix { get; set; } = "";
    public int SecondLine { get; set; }
    public bool SecondCustomStyleEnabled { get; set; }
    public string SecondFont { get; set; } = "";
    public Animation SecondFontSize { get; } = new Animation(34.0, 1.0, 100000.0);
    public Color SecondFontColor { get; set; }
    public bool SecondBold { get; set; }
    public bool SecondItalic { get; set; }
    public bool SecondUnderline { get; set; }
    public bool SecondStrikeThrough { get; set; }
    public bool SecondSplitByCharacter { get; set; }
    public YukkuriMovieMaker.Project.Items.Style SecondStyle { get; set; }
    public Color SecondStyleColor { get; set; }

    public bool FractionEnabled { get; set; }
    public int FractionDigits { get; set; }
    public string FractionPrefix { get; set; } = "";
    public string FractionSuffix { get; set; } = "";
    public int FractionLine { get; set; }
    public bool FractionCustomStyleEnabled { get; set; }
    public string FractionFont { get; set; } = "";
    public Animation FractionFontSize { get; } = new Animation(34.0, 1.0, 100000.0);
    public Color FractionFontColor { get; set; }
    public bool FractionBold { get; set; }
    public bool FractionItalic { get; set; }
    public bool FractionUnderline { get; set; }
    public bool FractionStrikeThrough { get; set; }
    public bool FractionSplitByCharacter { get; set; }
    public YukkuriMovieMaker.Project.Items.Style FractionStyle { get; set; }
    public Color FractionStyleColor { get; set; }

    public string Font { get; set; } = "";
    public Animation FontSize { get; } = new Animation(34.0, 1.0, 100000.0);
    public Animation LetterSpacing2 { get; } = new Animation(0.0, -100000.0, 100000.0);
    public BasePoint BasePoint { get; set; }
    public Color FontColor { get; set; }
    public YukkuriMovieMaker.Project.Items.Style Style { get; set; }
    public Color StyleColor { get; set; }
    public bool Bold { get; set; }
    public bool Italic { get; set; }
    public bool Underline { get; set; }
    public bool StrikeThrough { get; set; }
    public bool SplitByCharacter { get; set; }

    public TimerPlusShapeParameterData()
    {
    }

    public TimerPlusShapeParameterData(TimerPlusShapeParameter parameter)
    {
        Font = parameter.Font;
        InitialValueOffset.CopyFrom(parameter.InitialValueOffset);
        PlaybackRate.CopyFrom(parameter.PlaybackRate);
        FontSize.CopyFrom(parameter.FontSize);
        LetterSpacing2.CopyFrom(parameter.LetterSpacing2);
        FontColor = parameter.FontColor;
        Style = parameter.Style;
        StyleColor = parameter.StyleColor;
        Bold = parameter.Bold;
        Italic = parameter.Italic;
        Underline = parameter.Underline;
        StrikeThrough = parameter.StrikeThrough;
        SplitByCharacter = parameter.SplitByCharacter;
        BasePoint = parameter.BasePoint;

        Format = parameter.Format;
        Direction = parameter.Direction;
        ReverseBehavior = parameter.ReverseBehavior;

        InitialTime = parameter.InitialTime;
        InitialValueBaseFrames = parameter.InitialValueBaseFrames;

        DayEnabled = parameter.DayEnabled;
        DayDigits = parameter.DayDigits;
        DayPrefix = parameter.DayPrefix;
        DaySuffix = parameter.DaySuffix;
        DayLine = parameter.DayLine;
        DayCustomStyleEnabled = parameter.DayCustomStyleEnabled;
        DayFont = parameter.DayFont;
        DayFontSize.CopyFrom(parameter.DayFontSize);
        DayFontColor = parameter.DayFontColor;
        DayBold = parameter.DayBold;
        DayItalic = parameter.DayItalic;
        DayUnderline = parameter.DayUnderline;
        DayStrikeThrough = parameter.DayStrikeThrough;
        DaySplitByCharacter = parameter.DaySplitByCharacter;
        DayStyle = parameter.DayStyle;
        DayStyleColor = parameter.DayStyleColor;

        HourEnabled = parameter.HourEnabled;
        HourDigits = parameter.HourDigits;
        HourPrefix = parameter.HourPrefix;
        HourSuffix = parameter.HourSuffix;
        HourLine = parameter.HourLine;
        HourCustomStyleEnabled = parameter.HourCustomStyleEnabled;
        HourFont = parameter.HourFont;
        HourFontSize.CopyFrom(parameter.HourFontSize);
        HourFontColor = parameter.HourFontColor;
        HourBold = parameter.HourBold;
        HourItalic = parameter.HourItalic;
        HourUnderline = parameter.HourUnderline;
        HourStrikeThrough = parameter.HourStrikeThrough;
        HourSplitByCharacter = parameter.HourSplitByCharacter;
        HourStyle = parameter.HourStyle;
        HourStyleColor = parameter.HourStyleColor;

        MinuteEnabled = parameter.MinuteEnabled;
        MinuteDigits = parameter.MinuteDigits;
        MinutePrefix = parameter.MinutePrefix;
        MinuteSuffix = parameter.MinuteSuffix;
        MinuteLine = parameter.MinuteLine;
        MinuteCustomStyleEnabled = parameter.MinuteCustomStyleEnabled;
        MinuteFont = parameter.MinuteFont;
        MinuteFontSize.CopyFrom(parameter.MinuteFontSize);
        MinuteFontColor = parameter.MinuteFontColor;
        MinuteBold = parameter.MinuteBold;
        MinuteItalic = parameter.MinuteItalic;
        MinuteUnderline = parameter.MinuteUnderline;
        MinuteStrikeThrough = parameter.MinuteStrikeThrough;
        MinuteSplitByCharacter = parameter.MinuteSplitByCharacter;
        MinuteStyle = parameter.MinuteStyle;
        MinuteStyleColor = parameter.MinuteStyleColor;

        SecondEnabled = parameter.SecondEnabled;
        SecondDigits = parameter.SecondDigits;
        SecondPrefix = parameter.SecondPrefix;
        SecondSuffix = parameter.SecondSuffix;
        SecondLine = parameter.SecondLine;
        SecondCustomStyleEnabled = parameter.SecondCustomStyleEnabled;
        SecondFont = parameter.SecondFont;
        SecondFontSize.CopyFrom(parameter.SecondFontSize);
        SecondFontColor = parameter.SecondFontColor;
        SecondBold = parameter.SecondBold;
        SecondItalic = parameter.SecondItalic;
        SecondUnderline = parameter.SecondUnderline;
        SecondStrikeThrough = parameter.SecondStrikeThrough;
        SecondSplitByCharacter = parameter.SecondSplitByCharacter;
        SecondStyle = parameter.SecondStyle;
        SecondStyleColor = parameter.SecondStyleColor;

        FractionEnabled = parameter.FractionEnabled;
        FractionDigits = parameter.FractionDigits;
        FractionPrefix = parameter.FractionPrefix;
        FractionSuffix = parameter.FractionSuffix;
        FractionLine = parameter.FractionLine;
        FractionCustomStyleEnabled = parameter.FractionCustomStyleEnabled;
        FractionFont = parameter.FractionFont;
        FractionFontSize.CopyFrom(parameter.FractionFontSize);
        FractionFontColor = parameter.FractionFontColor;
        FractionBold = parameter.FractionBold;
        FractionItalic = parameter.FractionItalic;
        FractionUnderline = parameter.FractionUnderline;
        FractionStrikeThrough = parameter.FractionStrikeThrough;
        FractionSplitByCharacter = parameter.FractionSplitByCharacter;
        FractionStyle = parameter.FractionStyle;
        FractionStyleColor = parameter.FractionStyleColor;
    }
}