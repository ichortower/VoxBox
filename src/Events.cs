using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System.Collections.Generic;

namespace ichortower.VoxBox;

internal class Events
{
    public static void OnAssetRequested(object sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(VoiceData.AssetName)) {
            e.LoadFrom(() => new Dictionary<string, VoiceEntry>(), AssetLoadPriority.Exclusive);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo(VoiceExpressions.AssetName)) {
            e.LoadFrom(() => new Dictionary<string, List<VoiceOverride>>(),
                    AssetLoadPriority.Exclusive);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo(LineReadings.AssetName)) {
            e.LoadFrom(() => new Dictionary<string, List<Line>>(), AssetLoadPriority.Exclusive);
        }
    }

    public static void OnAssetReady(object sender, AssetReadyEventArgs e)
    {
        //if (e.NameWithoutLocale.IsEquivalentTo(LineReadings.AssetName)) {
            //LineReadings.Load();
        //}
    }

    public static void OnAssetsInvalidated(object sender, AssetsInvalidatedEventArgs e)
    {
        foreach (var name in e.Names) {
            if (name.IsEquivalentTo(VoiceData.AssetName)) {
                Log.Trace("Invalidating voice data");
                VoiceData.Data.Clear();
            }
            else if (name.IsEquivalentTo(VoiceExpressions.AssetName)) {
                Log.Trace("Invalidating expression data");
                VoiceExpressions.Data.Clear();
            }
            // reload line readings when invalidated. unlike voice data, line readings aren't
            // cache-friendly, so we don't load it on demand
            else if (name.IsEquivalentTo(LineReadings.AssetName)) {
                Log.Trace("Reloading line reading data");
                LineReadings.Load();
            }
        }
    }

    public static void OnMenuChanged(object sender, MenuChangedEventArgs e)
    {
        if (e.NewMenu is not DialogueBox db) {
            return;
        }
        VoiceExpressions.Load();
        VoiceData.LoadSpeakerVoice(db.characterDialogue?.speaker);
        VoiceData.OnOpen?.Invoke();
        VoiceData.OnOpen = null;
    }

    public static void OnGameLaunched(object sender, GameLaunchedEventArgs e)
    {
    }

    public static void OnDayStarted(object sender, DayStartedEventArgs e)
    {
    }

    public static void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
    {
    }

    /*
     * This takes the place of GameLaunched to load the line readings data because Content
     * Patcher isn't ready until two ticks later.
     * It is done in advance because I expect it to be expensive, and it's difficult to make it
     * skip work on subsequent calls.
     * Unregister once we're done.
     */
    internal static int contentTimer = 3;
    public static void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
    {
        contentTimer--;
        if (contentTimer == 0) {
            LineReadings.Load();
            VoxBox.instance.Helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
        }
    }
}
