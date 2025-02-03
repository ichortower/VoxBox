using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using StardewValley;
using StardewValley.Audio;
using StardewValley.GameData;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ichortower.VoxBox;

internal class Line
{
    public int Delay = 0;
    public string Sound = "";
}

internal class LineReadings
{
    public static string AssetName = $"Mods/{VoxBox.ModId}/LineReadings";
    internal static Dictionary<string, List<Line>> RawData = new();
    public static Dictionary<string, List<Line>> Data = new();

    internal static int DelayTimer = 0;
    internal static string DelayedName = "";
    internal static ICue ActiveCue = null;
    internal static List<Line> Cues = null;

    public static void ClearCurrent() {
        ActiveCue?.Stop(AudioStopOptions.Immediate);
        DelayTimer = 0;
        DelayedName = "";
        ActiveCue = null;
        Cues = null;
    }

    public static void Load() {
        RawData = Game1.content.Load<Dictionary<string, List<Line>>>(AssetName);
        Data.Clear();
        foreach (var kvp in RawData) {
            List<Line> cues = new();
            for (int i = 0; i < kvp.Value.Count; ++i) {
                string path = kvp.Value[i].Sound;
                if (string.IsNullOrEmpty(path)) {
                    continue;
                }
                AudioCueData acd = new();
                acd.Id = $"{VoxBox.ModId}_{kvp.Key}|{i}";
                acd.FilePaths = new List<string>{path};
                acd.Category = "Sound";
                acd.StreamedVorbis = path.EndsWith(".ogg");
                Game1.CueModification.cueModificationData[acd.Id] = acd;
                Line l = new();
                l.Delay = kvp.Value[i].Delay;
                l.Sound = acd.Id;
                cues.Add(l);
            }
            Data[kvp.Key] = cues;
            Log.Warn(string.Join(", ", cues.Select(c => c.Sound).ToArray()));
        }
        Game1.CueModification.ApplyAllCueModifications();
    }

    public static bool LineSpeech(DialogueBox db, GameTime time)
    {
        if (ActiveCue != null) {
            return true;
        }
        int dIndex = db.characterDialogue?.currentDialogueIndex ?? Int32.MaxValue;
        if (Cues == null || dIndex >= Cues.Count || string.IsNullOrEmpty(Cues[dIndex].Sound)) {
            return true;
        }
        if (DelayTimer > 0) {
            DelayTimer -= time.ElapsedGameTime.Milliseconds;
            if (DelayTimer <= 0) {
                string val = DelayedName;
                ClearCurrent();
                Game1.sounds.PlayLocal(val, null, null, null, SoundContext.Default, out ActiveCue);
            }
            return true;
        }
        if (Cues[dIndex].Delay > 0) {
            DelayTimer = Cues[dIndex].Delay;
            DelayedName = Cues[dIndex].Sound;
        }
        else {
            Game1.sounds.PlayLocal(Cues[dIndex].Sound, null, null, null, SoundContext.Default,
                    out ActiveCue);
        }
        return true;
    }
}
