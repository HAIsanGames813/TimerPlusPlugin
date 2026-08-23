using YukkuriMovieMaker.Plugin;
using YukkuriMovieMaker.Plugin.Shape;
using YukkuriMovieMaker.Project;

namespace TimerPlusPlugin;

[PluginDetails(AuthorName = "ハイさん", ContentId = "")]
public class TimerPlusShapePlugin : IShapePlugin, IPlugin
{
    public string Name => "タイマー＋";

    public bool IsExoShapeSupported => false;

    public bool IsExoMaskSupported => false;

    public IShapeParameter CreateShapeParameter(SharedDataStore? store)
        => new TimerPlusShapeParameter(store);
}