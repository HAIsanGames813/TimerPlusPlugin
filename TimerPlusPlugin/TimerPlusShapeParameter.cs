using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Controls.AvalonEdit.AutoCompletionStrategy;
using YukkuriMovieMaker.Controls.AvalonEdit.FoldingStrategy;
using YukkuriMovieMaker.Controls.AvalonEdit.ToolBarStrategy;
using YukkuriMovieMaker.Exo;
using YukkuriMovieMaker.ItemEditor;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin;
using YukkuriMovieMaker.Plugin.Effects;
using YukkuriMovieMaker.Plugin.Shape;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.Resources.Localization;

namespace TimerPlusPlugin;

public class TimerPlusShapeParameter : ShapeParameterBase
{
    public TimerPlusShapeParameter(SharedDataStore? sharedData) : base(sharedData)
    {
    }

    [Obsolete]
    public TimerPlusShapeParameter() : this(null)
    {
    }

    [Display(GroupName = "書式", Name = "書式", Description = "表示形式を選択します。「カスタム」を選ぶと下の単位ごとの項目が有効になります")]
    [EnumComboBox]
    public TimerPlusFormat Format { get => field; set => Set(ref field, value); } = TimerPlusFormat.MMSSFF;

    [Display(GroupName = "モード", Name = "モード", Description = "カウントアップ/カウントダウン")]
    [EnumComboBox]
    public TimerPlusCountDirection Direction { get => field; set => Set(ref field, value); } = TimerPlusCountDirection.CountUp;

    [Display(GroupName = "モード", Name = "動作反転", Description = "OFF: カウントアップは初期値が開始値、カウントダウンは初期値が終了値。ON: この対応関係を入れ替えます")]
    [ToggleSlider]
    public bool ReverseBehavior { get => field; set => Set(ref field, value); } = false;

    [Display(GroupName = "タイマー", Name = "初期時間", Description = "カウントの基準となる時間(00:00:00.00形式)")]
    [TimeSpanTextEditor]
    [TimeSpanDefaultValue]
    [TimeSpanRange]
    public TimeSpan InitialTime { get => field; set => Set(ref field, value); } = TimeSpan.Zero;

    [Display(GroupName = "タイマー", Name = "初期値:フレーム", Description = "カウントの基準となる時間(フレームの部分。タイムラインのFPS基準で秒に変換されます)")]
    [TextBoxSlider("F0", "", 0, 999999)]
    [DefaultValue(0)]
    [Range(0, 999999)]
    public int InitialValueBaseFrames { get => field; set => Set(ref field, value); } = 0;

    internal double GetInitialValueBaseSeconds(int fps)
    {
        double frameSeconds = fps > 0 ? (double)InitialValueBaseFrames / fps : 0.0;
        return InitialTime.TotalSeconds + frameSeconds;
    }

    [Display(GroupName = "タイマー", Name = "初期値オフセット", Description = "初期値に加算される、イージング対応のオフセット(秒)", AutoGenerateField = true)]
    [AnimationSlider("F2", "秒", -60, 60)]
    public Animation InitialValueOffset { get; } = new Animation(0.0, -2147483648.0, 2147483647.0);

    [Display(GroupName = "タイマー", Name = "速度", Description = "再生速度(%)。Plusではマイナス方向(逆再生)にも対応。アイテム先頭からの積算(累計)で計算されます", AutoGenerateField = true)]
    [AnimationSlider("F0", "%", -1000, 1000)]
    public Animation PlaybackRate { get; } = new Animation(100.0, -100000.0, 100000.0);

    [Display(GroupName = "日", Name = "日を使用する", Description = "書式が「カスタム」のとき、日の表示を有効にします")]
    [ToggleSlider]
    public bool DayEnabled { get => field; set => Set(ref field, value); } = false;

    [Display(GroupName = "日", Name = "日の桁数", Description = "0埋めする桁数")]
    [TextBoxSlider("F0", "", 1, 10)]
    [DefaultValue(2)]
    [Range(1, 10)]
    public int DayDigits { get => field; set => Set(ref field, value); } = 2;

    [Display(GroupName = "日", Name = "日:前文字", Description = "日の数値の前に表示する文字列")]
    [TextEditor]
    public string DayPrefix { get => field; set => Set(ref field, value); } = "";

    [Display(GroupName = "日", Name = "日:後文字", Description = "日の数値の後ろに表示する文字列")]
    [TextEditor]
    public string DaySuffix { get => field; set => Set(ref field, value); } = ":";

    [Display(GroupName = "日", Name = "表示行", Description = "表示する行番号(1〜)")]
    [TextBoxSlider("F0", "", 1, 10)]
    [DefaultValue(1)]
    [Range(1, 10)]
    public int DayLine { get => field; set => Set(ref field, value); } = 1;

    [Display(GroupName = "日", Name = "個別設定", Description = "ONのときはこの単位専用のスタイル設定を使用します。OFFのときは文字グループの設定をそのまま使用します")]
    [ToggleSlider]
    public bool DayCustomStyleEnabled { get => field; set => Set(ref field, value); } = false;

    [Display(GroupName = "日/文字", Name = "フォント", Description = "日の数値部分のフォント")]
    [FontComboBox]
    public string DayFont { get => field; set => Set(ref field, value); } = "メイリオ";

    [Display(GroupName = "日/文字", Name = "サイズ", Description = "文字グループのサイズに対する割合(%)。100で同じサイズ", AutoGenerateField = true)]
    [AnimationSlider("F0", "%", 1, 1000)]
    public Animation DayFontSize { get; } = new Animation(100.0, 1.0, 100000.0);

    [Display(GroupName = "日/文字", Name = "文字色", Description = "日の数値部分の文字色")]
    [ColorPicker]
    public Color DayFontColor { get => field; set => Set(ref field, value); } = Colors.White;

    [Display(GroupName = "日/文字", Name = "太字", Description = "日の数値部分を太字にします")]
    [ToggleSlider]
    public bool DayBold { get => field; set => Set(ref field, value); } = false;

    [Display(GroupName = "日/文字", Name = "イタリック", Description = "日の数値部分を斜体にします")]
    [ToggleSlider]
    public bool DayItalic { get => field; set => Set(ref field, value); } = false;

    [Display(GroupName = "日/文字", Name = "下線", Description = "日の数値部分に下線を付けます")]
    [ToggleSlider]
    public bool DayUnderline { get => field; set => Set(ref field, value); } = false;

    [Display(GroupName = "日/文字", Name = "打ち消し線", Description = "日の数値部分に打ち消し線を付けます")]
    [ToggleSlider]
    public bool DayStrikeThrough { get => field; set => Set(ref field, value); } = false;

    [Display(GroupName = "日/文字", Name = "文字ごとに分割", Description = "日の数値部分を文字ごとに個別のグリフとして配置します")]
    [ToggleSlider]
    public bool DaySplitByCharacter { get => field; set => Set(ref field, value); } = false;

    [Display(GroupName = "日/文字", Name = "装飾", Description = "日の数値部分の縁取り等の装飾")]
    [EnumComboBox]
    public YukkuriMovieMaker.Project.Items.Style DayStyle { get => field; set => Set(ref field, value); } = YukkuriMovieMaker.Project.Items.Style.Normal;

    [Display(GroupName = "日/文字", Name = "装飾色", Description = "日の数値部分の装飾色")]
    [ColorPicker]
    public Color DayStyleColor { get => field; set => Set(ref field, value); } = Colors.Black;

    [Display(GroupName = "時", Name = "時を使用する", Description = "書式が「カスタム」のとき、時の表示を有効にします")]
    [ToggleSlider]
    public bool HourEnabled { get => field; set => Set(ref field, value); } = false;

    [Display(GroupName = "時", Name = "時の桁数", Description = "0埋めする桁数")]
    [TextBoxSlider("F0", "", 1, 10)]
    [DefaultValue(2)]
    [Range(1, 10)]
    public int HourDigits { get => field; set => Set(ref field, value); } = 2;

    [Display(GroupName = "時", Name = "時:前文字", Description = "時の数値の前に表示する文字列")]
    [TextEditor]
    public string HourPrefix { get => field; set => Set(ref field, value); } = "";

    [Display(GroupName = "時", Name = "時:後文字", Description = "時の数値の後ろに表示する文字列")]
    [TextEditor]
    public string HourSuffix { get => field; set => Set(ref field, value); } = ":";

    [Display(GroupName = "時", Name = "表示行", Description = "表示する行番号(1〜)")]
    [TextBoxSlider("F0", "", 1, 10)]
    [DefaultValue(1)]
    [Range(1, 10)]
    public int HourLine { get => field; set => Set(ref field, value); } = 1;

    [Display(GroupName = "時", Name = "個別設定", Description = "ONのときはこの単位専用のスタイル設定を使用します。OFFのときは文字グループの設定をそのまま使用します")]
    [ToggleSlider]
    public bool HourCustomStyleEnabled { get => field; set => Set(ref field, value); } = false;

    [Display(GroupName = "時/文字", Name = "フォント", Description = "時の数値部分のフォント")]
    [FontComboBox]
    public string HourFont { get => field; set => Set(ref field, value); } = "メイリオ";

    [Display(GroupName = "時/文字", Name = "サイズ", Description = "文字グループのサイズに対する割合(%)。100で同じサイズ", AutoGenerateField = true)]
    [AnimationSlider("F0", "%", 1, 1000)]
    public Animation HourFontSize { get; } = new Animation(100.0, 1.0, 100000.0);

    [Display(GroupName = "時/文字", Name = "文字色", Description = "時の数値部分の文字色")]
    [ColorPicker]
    public Color HourFontColor { get => field; set => Set(ref field, value); } = Colors.White;

    [Display(GroupName = "時/文字", Name = "太字", Description = "時の数値部分を太字にします")]
    [ToggleSlider]
    public bool HourBold { get => field; set => Set(ref field, value); } = false;

    [Display(GroupName = "時/文字", Name = "イタリック", Description = "時の数値部分を斜体にします")]
    [ToggleSlider]
    public bool HourItalic { get => field; set => Set(ref field, value); } = false;

    [Display(GroupName = "時/文字", Name = "下線", Description = "時の数値部分に下線を付けます")]
    [ToggleSlider]
    public bool HourUnderline { get => field; set => Set(ref field, value); } = false;

    [Display(GroupName = "時/文字", Name = "打ち消し線", Description = "時の数値部分に打ち消し線を付けます")]
    [ToggleSlider]
    public bool HourStrikeThrough { get => field; set => Set(ref field, value); } = false;

    [Display(GroupName = "時/文字", Name = "文字ごとに分割", Description = "時の数値部分を文字ごとに個別のグリフとして配置します")]
    [ToggleSlider]
    public bool HourSplitByCharacter { get => field; set => Set(ref field, value); } = false;

    [Display(GroupName = "時/文字", Name = "装飾", Description = "時の数値部分の縁取り等の装飾")]
    [EnumComboBox]
    public YukkuriMovieMaker.Project.Items.Style HourStyle { get => field; set => Set(ref field, value); } = YukkuriMovieMaker.Project.Items.Style.Normal;

    [Display(GroupName = "時/文字", Name = "装飾色", Description = "時の数値部分の装飾色")]
    [ColorPicker]
    public Color HourStyleColor { get => field; set => Set(ref field, value); } = Colors.Black;

    [Display(GroupName = "分", Name = "分を使用する", Description = "書式が「カスタム」のとき、分の表示を有効にします")]
    [ToggleSlider]
    public bool MinuteEnabled { get => field; set => Set(ref field, value); } = true;

    [Display(GroupName = "分", Name = "分の桁数", Description = "0埋めする桁数")]
    [TextBoxSlider("F0", "", 1, 10)]
    [DefaultValue(2)]
    [Range(1, 10)]
    public int MinuteDigits { get => field; set => Set(ref field, value); } = 2;

    [Display(GroupName = "分", Name = "分:前文字", Description = "分の数値の前に表示する文字列")]
    [TextEditor]
    public string MinutePrefix { get => field; set => Set(ref field, value); } = "";

    [Display(GroupName = "分", Name = "分:後文字", Description = "分の数値の後ろに表示する文字列")]
    [TextEditor]
    public string MinuteSuffix { get => field; set => Set(ref field, value); } = ":";

    [Display(GroupName = "分", Name = "表示行", Description = "表示する行番号(1〜)")]
    [TextBoxSlider("F0", "", 1, 10)]
    [DefaultValue(1)]
    [Range(1, 10)]
    public int MinuteLine { get => field; set => Set(ref field, value); } = 1;

    [Display(GroupName = "分", Name = "個別設定", Description = "ONのときはこの単位専用のスタイル設定を使用します。OFFのときは文字グループの設定をそのまま使用します")]
    [ToggleSlider]
    public bool MinuteCustomStyleEnabled { get => field; set => Set(ref field, value); } = false;

    [Display(GroupName = "分/文字", Name = "フォント", Description = "分の数値部分のフォント")]
    [FontComboBox]
    public string MinuteFont { get => field; set => Set(ref field, value); } = "メイリオ";

    [Display(GroupName = "分/文字", Name = "サイズ", Description = "文字グループのサイズに対する割合(%)。100で同じサイズ", AutoGenerateField = true)]
    [AnimationSlider("F0", "%", 1, 1000)]
    public Animation MinuteFontSize { get; } = new Animation(100.0, 1.0, 100000.0);

    [Display(GroupName = "分/文字", Name = "文字色", Description = "分の数値部分の文字色")]
    [ColorPicker]
    public Color MinuteFontColor { get => field; set => Set(ref field, value); } = Colors.White;

    [Display(GroupName = "分/文字", Name = "太字", Description = "分の数値部分を太字にします")]
    [ToggleSlider]
    public bool MinuteBold { get => field; set => Set(ref field, value); } = false;

    [Display(GroupName = "分/文字", Name = "イタリック", Description = "分の数値部分を斜体にします")]
    [ToggleSlider]
    public bool MinuteItalic { get => field; set => Set(ref field, value); } = false;

    [Display(GroupName = "分/文字", Name = "下線", Description = "分の数値部分に下線を付けます")]
    [ToggleSlider]
    public bool MinuteUnderline { get => field; set => Set(ref field, value); } = false;

    [Display(GroupName = "分/文字", Name = "打ち消し線", Description = "分の数値部分に打ち消し線を付けます")]
    [ToggleSlider]
    public bool MinuteStrikeThrough { get => field; set => Set(ref field, value); } = false;

    [Display(GroupName = "分/文字", Name = "文字ごとに分割", Description = "分の数値部分を文字ごとに個別のグリフとして配置します")]
    [ToggleSlider]
    public bool MinuteSplitByCharacter { get => field; set => Set(ref field, value); } = false;

    [Display(GroupName = "分/文字", Name = "装飾", Description = "分の数値部分の縁取り等の装飾")]
    [EnumComboBox]
    public YukkuriMovieMaker.Project.Items.Style MinuteStyle { get => field; set => Set(ref field, value); } = YukkuriMovieMaker.Project.Items.Style.Normal;

    [Display(GroupName = "分/文字", Name = "装飾色", Description = "分の数値部分の装飾色")]
    [ColorPicker]
    public Color MinuteStyleColor { get => field; set => Set(ref field, value); } = Colors.Black;

    [Display(GroupName = "秒", Name = "秒を使用する", Description = "書式が「カスタム」のとき、秒の表示を有効にします")]
    [ToggleSlider]
    public bool SecondEnabled { get => field; set => Set(ref field, value); } = true;

    [Display(GroupName = "秒", Name = "秒の桁数", Description = "0埋めする桁数")]
    [TextBoxSlider("F0", "", 1, 10)]
    [DefaultValue(2)]
    [Range(1, 10)]
    public int SecondDigits { get => field; set => Set(ref field, value); } = 2;

    [Display(GroupName = "秒", Name = "秒:前文字", Description = "秒の数値の前に表示する文字列")]
    [TextEditor]
    public string SecondPrefix { get => field; set => Set(ref field, value); } = "";

    [Display(GroupName = "秒", Name = "秒:後文字", Description = "秒の数値の後ろに表示する文字列(小数秒を使う場合は小数点などをここに入れます)")]
    [TextEditor]
    public string SecondSuffix { get => field; set => Set(ref field, value); } = ".";

    [Display(GroupName = "秒", Name = "表示行", Description = "表示する行番号(1〜)")]
    [TextBoxSlider("F0", "", 1, 10)]
    [DefaultValue(1)]
    [Range(1, 10)]
    public int SecondLine { get => field; set => Set(ref field, value); } = 1;

    [Display(GroupName = "秒", Name = "個別設定", Description = "ONのときはこの単位専用のスタイル設定を使用します。OFFのときは文字グループの設定をそのまま使用します")]
    [ToggleSlider]
    public bool SecondCustomStyleEnabled { get => field; set => Set(ref field, value); } = false;

    [Display(GroupName = "秒/文字", Name = "フォント", Description = "秒の数値部分のフォント")]
    [FontComboBox]
    public string SecondFont { get => field; set => Set(ref field, value); } = "メイリオ";

    [Display(GroupName = "秒/文字", Name = "サイズ", Description = "文字グループのサイズに対する割合(%)。100で同じサイズ", AutoGenerateField = true)]
    [AnimationSlider("F0", "%", 1, 1000)]
    public Animation SecondFontSize { get; } = new Animation(100.0, 1.0, 100000.0);

    [Display(GroupName = "秒/文字", Name = "文字色", Description = "秒の数値部分の文字色")]
    [ColorPicker]
    public Color SecondFontColor { get => field; set => Set(ref field, value); } = Colors.White;

    [Display(GroupName = "秒/文字", Name = "太字", Description = "秒の数値部分を太字にします")]
    [ToggleSlider]
    public bool SecondBold { get => field; set => Set(ref field, value); } = false;

    [Display(GroupName = "秒/文字", Name = "イタリック", Description = "秒の数値部分を斜体にします")]
    [ToggleSlider]
    public bool SecondItalic { get => field; set => Set(ref field, value); } = false;

    [Display(GroupName = "秒/文字", Name = "下線", Description = "秒の数値部分に下線を付けます")]
    [ToggleSlider]
    public bool SecondUnderline { get => field; set => Set(ref field, value); } = false;

    [Display(GroupName = "秒/文字", Name = "打ち消し線", Description = "秒の数値部分に打ち消し線を付けます")]
    [ToggleSlider]
    public bool SecondStrikeThrough { get => field; set => Set(ref field, value); } = false;

    [Display(GroupName = "秒/文字", Name = "文字ごとに分割", Description = "秒の数値部分を文字ごとに個別のグリフとして配置します")]
    [ToggleSlider]
    public bool SecondSplitByCharacter { get => field; set => Set(ref field, value); } = false;

    [Display(GroupName = "秒/文字", Name = "装飾", Description = "秒の数値部分の縁取り等の装飾")]
    [EnumComboBox]
    public YukkuriMovieMaker.Project.Items.Style SecondStyle { get => field; set => Set(ref field, value); } = YukkuriMovieMaker.Project.Items.Style.Normal;

    [Display(GroupName = "秒/文字", Name = "装飾色", Description = "秒の数値部分の装飾色")]
    [ColorPicker]
    public Color SecondStyleColor { get => field; set => Set(ref field, value); } = Colors.Black;

    [Display(GroupName = "小数秒", Name = "小数秒を使用する", Description = "書式が「カスタム」のとき、小数秒の表示を有効にします")]
    [ToggleSlider]
    public bool FractionEnabled { get => field; set => Set(ref field, value); } = true;

    [Display(GroupName = "小数秒", Name = "小数秒の桁数", Description = "0埋めする桁数")]
    [TextBoxSlider("F0", "", 1, 6)]
    [DefaultValue(2)]
    [Range(1, 6)]
    public int FractionDigits { get => field; set => Set(ref field, value); } = 2;

    [Display(GroupName = "小数秒", Name = "小数秒:前文字", Description = "小数秒の数値の前に表示する文字列(通常は空でOK。秒の後文字に小数点を入れます)")]
    [TextEditor]
    public string FractionPrefix { get => field; set => Set(ref field, value); } = "";

    [Display(GroupName = "小数秒", Name = "小数秒:後文字", Description = "小数秒の数値の後ろに表示する文字列")]
    [TextEditor]
    public string FractionSuffix { get => field; set => Set(ref field, value); } = "";

    [Display(GroupName = "小数秒", Name = "表示行", Description = "表示する行番号(1〜)")]
    [TextBoxSlider("F0", "", 1, 10)]
    [DefaultValue(1)]
    [Range(1, 10)]
    public int FractionLine { get => field; set => Set(ref field, value); } = 1;

    [Display(GroupName = "小数秒", Name = "個別設定", Description = "ONのときはこの単位専用のスタイル設定を使用します。OFFのときは文字グループの設定をそのまま使用します")]
    [ToggleSlider]
    public bool FractionCustomStyleEnabled { get => field; set => Set(ref field, value); } = false;

    [Display(GroupName = "小数秒/文字", Name = "フォント", Description = "小数秒の数値部分のフォント")]
    [FontComboBox]
    public string FractionFont { get => field; set => Set(ref field, value); } = "メイリオ";

    [Display(GroupName = "小数秒/文字", Name = "サイズ", Description = "文字グループのサイズに対する割合(%)。100で同じサイズ", AutoGenerateField = true)]
    [AnimationSlider("F0", "%", 1, 1000)]
    public Animation FractionFontSize { get; } = new Animation(100.0, 1.0, 100000.0);

    [Display(GroupName = "小数秒/文字", Name = "文字色", Description = "小数秒の数値部分の文字色")]
    [ColorPicker]
    public Color FractionFontColor { get => field; set => Set(ref field, value); } = Colors.White;

    [Display(GroupName = "小数秒/文字", Name = "太字", Description = "小数秒の数値部分を太字にします")]
    [ToggleSlider]
    public bool FractionBold { get => field; set => Set(ref field, value); } = false;

    [Display(GroupName = "小数秒/文字", Name = "イタリック", Description = "小数秒の数値部分を斜体にします")]
    [ToggleSlider]
    public bool FractionItalic { get => field; set => Set(ref field, value); } = false;

    [Display(GroupName = "小数秒/文字", Name = "下線", Description = "小数秒の数値部分に下線を付けます")]
    [ToggleSlider]
    public bool FractionUnderline { get => field; set => Set(ref field, value); } = false;

    [Display(GroupName = "小数秒/文字", Name = "打ち消し線", Description = "小数秒の数値部分に打ち消し線を付けます")]
    [ToggleSlider]
    public bool FractionStrikeThrough { get => field; set => Set(ref field, value); } = false;

    [Display(GroupName = "小数秒/文字", Name = "文字ごとに分割", Description = "小数秒の数値部分を文字ごとに個別のグリフとして配置します")]
    [ToggleSlider]
    public bool FractionSplitByCharacter { get => field; set => Set(ref field, value); } = false;

    [Display(GroupName = "小数秒/文字", Name = "装飾", Description = "小数秒の数値部分の縁取り等の装飾")]
    [EnumComboBox]
    public YukkuriMovieMaker.Project.Items.Style FractionStyle { get => field; set => Set(ref field, value); } = YukkuriMovieMaker.Project.Items.Style.Normal;

    [Display(GroupName = "小数秒/文字", Name = "装飾色", Description = "小数秒の数値部分の装飾色")]
    [ColorPicker]
    public Color FractionStyleColor { get => field; set => Set(ref field, value); } = Colors.Black;

    [Display(GroupName = "文字", Name = "フォント")]
    [FontComboBox]
    public string Font { get => field; set => Set(ref field, value); } = "メイリオ";

    [Display(GroupName = "文字", Name = "サイズ", AutoGenerateField = true)]
    [AnimationSlider("F0", "px", 1, 500)]
    public Animation FontSize { get; } = new Animation(34.0, 1.0, 100000.0);

    [Display(GroupName = "文字", Name = "文字間隔", AutoGenerateField = true)]
    [AnimationSlider("F0", "px", -100, 100)]
    public Animation LetterSpacing2 { get; } = new Animation(0.0, -100000.0, 100000.0);

    [Display(GroupName = "文字", Name = "文字揃え")]
    [EnumComboBox]
    public BasePoint BasePoint { get => field; set => Set(ref field, value); } = BasePoint.CenterCenter;

    [Display(GroupName = "文字", Name = "文字色")]
    [ColorPicker]
    public Color FontColor { get => field; set => Set(ref field, value); } = Colors.White;

    [Display(GroupName = "文字", Name = "装飾")]
    [EnumComboBox]
    public YukkuriMovieMaker.Project.Items.Style Style { get => field; set => Set(ref field, value); } = YukkuriMovieMaker.Project.Items.Style.Normal;

    [Display(GroupName = "文字", Name = "装飾色")]
    [ColorPicker]
    public Color StyleColor { get => field; set => Set(ref field, value); } = Colors.Black;

    [Display(GroupName = "文字", Name = "太字")]
    [ToggleSlider]
    public bool Bold { get => field; set => Set(ref field, value); } = false;

    [Display(GroupName = "文字", Name = "イタリック")]
    [ToggleSlider]
    public bool Italic { get => field; set => Set(ref field, value); } = false;

    [Display(GroupName = "文字", Name = "下線")]
    [ToggleSlider]
    public bool Underline { get => field; set => Set(ref field, value); } = false;

    [Display(GroupName = "文字", Name = "打ち消し線")]
    [ToggleSlider]
    public bool StrikeThrough { get => field; set => Set(ref field, value); } = false;

    [Display(GroupName = "文字", Name = "文字ごとに分割", Description = "文字ごとに個別のグリフとして配置します(合字・カーニングを無効化)")]
    [ToggleSlider]
    public bool SplitByCharacter { get => field; set => Set(ref field, value); } = false;

    public override IShapeSource CreateShapeSource(IGraphicsDevicesAndContext devices)
        => new TimerPlusShapeSource(devices, this);

    public override IEnumerable<string> CreateShapeItemExoFilter(int keyFrameIndex, ExoOutputDescription exoOutputDescription)
        => Array.Empty<string>();

    public override IEnumerable<string> CreateMaskExoFilter(int keyFrameIndex, ExoOutputDescription exoOutputDescription, ShapeMaskExoOutputDescription shapeMaskParameters)
        => Array.Empty<string>();

    protected override IEnumerable<IAnimatable> GetAnimatables()
        => [InitialValueOffset, PlaybackRate, FontSize, LetterSpacing2, DayFontSize, HourFontSize, MinuteFontSize, SecondFontSize, FractionFontSize];

    internal TimerPlusCustomSettings CreateCustomSettings() => new(
        new TimerPlusUnitSettings(DayEnabled, DayDigits, DayPrefix, DaySuffix, DayLine, DayCustomStyleEnabled),
        new TimerPlusUnitSettings(HourEnabled, HourDigits, HourPrefix, HourSuffix, HourLine, HourCustomStyleEnabled),
        new TimerPlusUnitSettings(MinuteEnabled, MinuteDigits, MinutePrefix, MinuteSuffix, MinuteLine, MinuteCustomStyleEnabled),
        new TimerPlusUnitSettings(SecondEnabled, SecondDigits, SecondPrefix, SecondSuffix, SecondLine, SecondCustomStyleEnabled),
        new TimerPlusUnitSettings(FractionEnabled, FractionDigits, FractionPrefix, FractionSuffix, FractionLine, FractionCustomStyleEnabled));

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
        SplitByCharacter = data.SplitByCharacter;
        BasePoint = data.BasePoint;

        Format = data.Format;
        Direction = data.Direction;
        ReverseBehavior = data.ReverseBehavior;

        InitialTime = data.InitialTime;
        InitialValueBaseFrames = data.InitialValueBaseFrames;

        DayEnabled = data.DayEnabled;
        DayDigits = data.DayDigits;
        DayPrefix = data.DayPrefix;
        DaySuffix = data.DaySuffix;
        DayLine = data.DayLine;
        DayCustomStyleEnabled = data.DayCustomStyleEnabled;
        DayFont = data.DayFont;
        DayFontSize.CopyFrom(data.DayFontSize);
        DayFontColor = data.DayFontColor;
        DayBold = data.DayBold;
        DayItalic = data.DayItalic;
        DayUnderline = data.DayUnderline;
        DayStrikeThrough = data.DayStrikeThrough;
        DaySplitByCharacter = data.DaySplitByCharacter;
        DayStyle = data.DayStyle;
        DayStyleColor = data.DayStyleColor;

        HourEnabled = data.HourEnabled;
        HourDigits = data.HourDigits;
        HourPrefix = data.HourPrefix;
        HourSuffix = data.HourSuffix;
        HourLine = data.HourLine;
        HourCustomStyleEnabled = data.HourCustomStyleEnabled;
        HourFont = data.HourFont;
        HourFontSize.CopyFrom(data.HourFontSize);
        HourFontColor = data.HourFontColor;
        HourBold = data.HourBold;
        HourItalic = data.HourItalic;
        HourUnderline = data.HourUnderline;
        HourStrikeThrough = data.HourStrikeThrough;
        HourSplitByCharacter = data.HourSplitByCharacter;
        HourStyle = data.HourStyle;
        HourStyleColor = data.HourStyleColor;

        MinuteEnabled = data.MinuteEnabled;
        MinuteDigits = data.MinuteDigits;
        MinutePrefix = data.MinutePrefix;
        MinuteSuffix = data.MinuteSuffix;
        MinuteLine = data.MinuteLine;
        MinuteCustomStyleEnabled = data.MinuteCustomStyleEnabled;
        MinuteFont = data.MinuteFont;
        MinuteFontSize.CopyFrom(data.MinuteFontSize);
        MinuteFontColor = data.MinuteFontColor;
        MinuteBold = data.MinuteBold;
        MinuteItalic = data.MinuteItalic;
        MinuteUnderline = data.MinuteUnderline;
        MinuteStrikeThrough = data.MinuteStrikeThrough;
        MinuteSplitByCharacter = data.MinuteSplitByCharacter;
        MinuteStyle = data.MinuteStyle;
        MinuteStyleColor = data.MinuteStyleColor;

        SecondEnabled = data.SecondEnabled;
        SecondDigits = data.SecondDigits;
        SecondPrefix = data.SecondPrefix;
        SecondSuffix = data.SecondSuffix;
        SecondLine = data.SecondLine;
        SecondCustomStyleEnabled = data.SecondCustomStyleEnabled;
        SecondFont = data.SecondFont;
        SecondFontSize.CopyFrom(data.SecondFontSize);
        SecondFontColor = data.SecondFontColor;
        SecondBold = data.SecondBold;
        SecondItalic = data.SecondItalic;
        SecondUnderline = data.SecondUnderline;
        SecondStrikeThrough = data.SecondStrikeThrough;
        SecondSplitByCharacter = data.SecondSplitByCharacter;
        SecondStyle = data.SecondStyle;
        SecondStyleColor = data.SecondStyleColor;

        FractionEnabled = data.FractionEnabled;
        FractionDigits = data.FractionDigits;
        FractionPrefix = data.FractionPrefix;
        FractionSuffix = data.FractionSuffix;
        FractionLine = data.FractionLine;
        FractionCustomStyleEnabled = data.FractionCustomStyleEnabled;
        FractionFont = data.FractionFont;
        FractionFontSize.CopyFrom(data.FractionFontSize);
        FractionFontColor = data.FractionFontColor;
        FractionBold = data.FractionBold;
        FractionItalic = data.FractionItalic;
        FractionUnderline = data.FractionUnderline;
        FractionStrikeThrough = data.FractionStrikeThrough;
        FractionSplitByCharacter = data.FractionSplitByCharacter;
        FractionStyle = data.FractionStyle;
        FractionStyleColor = data.FractionStyleColor;
    }
}