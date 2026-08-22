using System.ComponentModel.DataAnnotations;

namespace TimerPlusPlugin;

public enum TimerPlusFormat
{
    [Display(GroupName = "書式", Name = "1", Description = "1")]
    S,

    [Display(GroupName = "書式", Name = "01", Description = "01")]
    SS,

    [Display(GroupName = "書式", Name = "001", Description = "001")]
    SSS,

    [Display(GroupName = "書式", Name = "0001", Description = "0001")]
    SSSS,

    [Display(GroupName = "書式", Name = "01.00", Description = "01.00")]
    SSFF,

    [Display(GroupName = "書式", Name = "00:01", Description = "00:01")]
    MMSS,

    [Display(GroupName = "書式", Name = "00:01.00", Description = "00:01.00")]
    MMSSFF,

    [Display(GroupName = "書式", Name = "00:00:01", Description = "00:00:01")]
    HHMMSS,

    [Display(GroupName = "書式", Name = "00:00:01.00", Description = "00:00:01.00")]
    HHMMSSFF,

    [Display(GroupName = "書式", Name = "カスタム", Description = "書式や配置をカスタムできます")]
    Custom,
}

public enum TimerPlusCountDirection
{
    [Display(GroupName = "モード", Name = "カウントアップ", Description = "初期値から増加していきます")]
    CountUp,

    [Display(GroupName = "モード", Name = "カウントダウン", Description = "初期値まで減少していきます")]
    CountDown,
}