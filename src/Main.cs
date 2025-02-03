using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace ichortower.VoxBox;

internal sealed class VoxBox : Mod
{
    public static VoxBox instance;
    public static string ModId;

    public override void Entry(IModHelper helper)
    {
        instance = this;
        ModId = instance.ModManifest.UniqueID;

        helper.Events.Content.AssetRequested += Events.OnAssetRequested;
        helper.Events.Content.AssetReady += Events.OnAssetReady;
        helper.Events.Content.AssetsInvalidated += Events.OnAssetsInvalidated;
        //helper.Events.GameLoop.GameLaunched += Events.OnGameLaunched;
        //helper.Events.GameLoop.DayStarted += Events.OnDayStarted;
        //helper.Events.GameLoop.ReturnedToTitle += Events.OnReturnedToTitle;
        helper.Events.Display.MenuChanged += Events.OnMenuChanged;
        helper.Events.GameLoop.UpdateTicked += Events.OnUpdateTicked;

        Patches.Apply();
    }
}

internal class Log
{
    public static void Trace(string text) {
        VoxBox.instance.Monitor.Log(text, LogLevel.Trace);
    }
    public static void Debug(string text) {
        VoxBox.instance.Monitor.Log(text, LogLevel.Debug);
    }
    public static void Info(string text) {
        VoxBox.instance.Monitor.Log(text, LogLevel.Info);
    }
    public static void Warn(string text) {
        VoxBox.instance.Monitor.Log(text, LogLevel.Warn);
    }
    public static void Error(string text) {
        VoxBox.instance.Monitor.Log(text, LogLevel.Error);
    }
    public static void Alert(string text) {
        VoxBox.instance.Monitor.Log(text, LogLevel.Alert);
    }
    public static void Verbose(string text) {
        VoxBox.instance.Monitor.VerboseLog(text);
    }
}

internal class TR
{
    public static string Get(string key) {
        return VoxBox.instance.Helper.Translation.Get(key);
    }
}
