using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using StardewValley;
using StardewValley.Audio;
using StardewValley.Extensions;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ichortower.VoxBox;

internal class VoiceEntry
{
    private int[] _delay = new int[]{30};
    private int[] _pitch = new int[]{};
    private float[] _volume = new float[]{};

    public string CueName = "dialogueCharacter";
    public int[] Delay {
        get {
            return _delay;
        }
        set {
            _delay = value.Select(i => Math.Max(0, i)).ToArray();
        }
    }
    public int[] Pitch {
        get {
            return _pitch;
        }
        set {
            _pitch = value.Select(i => Math.Max(-1200, Math.Min(i, 1200))).ToArray();
        }
    }
    public float[] Volume {
        get {
            return _volume;
        }
        set {
            _volume = value.Select(i => MathF.Max(0f, i)).ToArray();
        }
    }

    public VoiceEntry Clone()
    {
        VoiceEntry res = new();
        res.CueName = CueName;
        res.Delay = Delay;
        res.Pitch = Pitch;
        res.Volume = Volume;
        return res;
    }

    public VoiceEntry Mixin(VoiceOverride other) {
        VoiceEntry res = Clone();
        if (other.CueName != null) {
            res.CueName = other.CueName;
        }
        if (other.Delay != null) {
            res.Delay = other.Delay;
        }
        if (other.Pitch != null) {
            res.Pitch = other.Pitch;
        }
        if (other.Volume != null) {
            res.Volume = other.Volume;
        }
        return res;
    }

    public static VoiceEntry Vanilla = new();
}

internal class VoiceOverride
{
    public string CueName = null;
    public int[] Delay = null;
    public int[] Pitch = null;
    public float[] Volume = null;
}

internal class VoiceData
{
    public static string AssetName = $"Mods/{VoxBox.ModId}/VoiceData";
    public static Dictionary<string, VoiceEntry> Data = new();

    public static VoiceEntry CurrentVoice = VoiceEntry.Vanilla.Clone();
    public static Action OnOpen = null;
    public static int DelayTimer = 0;
    internal static ICue ActiveCue = null;

    public static void Load()
    {
        Data = Game1.content.Load<Dictionary<string, VoiceEntry>>(AssetName);
    }

    public static void LoadSpeakerVoice(NPC speaker)
    {
        Load();
        DelayTimer = 0;
        CurrentVoice = EntryFor(speaker).Clone();
    }

    public static VoiceEntry EntryFor(NPC speaker)
    {
        if (speaker is null || !Data.TryGetValue(speaker.Name, out VoiceEntry res)) {
            return VoiceEntry.Vanilla;
        }
        return res;
    }

    public static bool BlurbSpeech(DialogueBox db, GameTime time)
    {
        // if delays is an empty array, wait for the last cue to finish playing
        if (CurrentVoice.Delay.Length == 0 && ActiveCue?.IsPlaying is true) {
            return true;
        }
        DelayTimer -= time.ElapsedGameTime.Milliseconds;
        if (DelayTimer > 0) {
            return true;
        }
        DelayTimer = ChooseFrom(CurrentVoice.Delay, 0);
        // for mysterious reasons, pitch must be set after playing:
        // Game1.playSound(string, int?) does not respect the int parameter
        Game1.sounds.PlayLocal(CurrentVoice.CueName, null, null, null,
                SoundContext.Default, out ActiveCue);
        ActiveCue.Pitch = ChooseFrom(CurrentVoice.Pitch, 0) / 1200f;
        ActiveCue.Volume *= ChooseFrom(CurrentVoice.Volume, 1.0f);
        return true;
    }

    internal static T ChooseFrom<T>(IList<T> options, T fallback)
    {
        if (options == null || options.Count == 0) {
            return fallback;
        }
        if (options.Count == 1) {
            return options[0];
        }
        return Game1.random.ChooseFrom(options);
    }
}

internal class VoiceExpressions
{
    public static string AssetName = $"Mods/{VoxBox.ModId}/LineExpressions";
    public static Dictionary<string, List<VoiceOverride>> Data = new();
    public static List<VoiceOverride> Voices = new();
    public static VoiceEntry CurrentVoice = VoiceEntry.Vanilla.Clone();
    public static int DelayTimer = 0;
    internal static ICue ActiveCue = null;

    public static void Load()
    {
        Data = Game1.content.Load<Dictionary<string, List<VoiceOverride>>>(AssetName);
    }

    public static bool BlurbSpeech(DialogueBox db, GameTime time)
    {
        int dIndex = db.characterDialogue?.currentDialogueIndex ?? Int32.MaxValue;
        if (Voices != null && dIndex < Voices.Count) {
            CurrentVoice = VoiceData.CurrentVoice.Mixin(Voices[dIndex] ?? new VoiceOverride());
        }
        else {
            CurrentVoice = VoiceData.CurrentVoice.Clone();
        }
        // from here on, just a straight copy of VoiceData.BlurbSpeech

        // if delays is an empty array, wait for the last cue to finish playing
        if (CurrentVoice.Delay.Length == 0 && ActiveCue?.IsPlaying is true) {
            return true;
        }
        DelayTimer -= time.ElapsedGameTime.Milliseconds;
        if (DelayTimer > 0) {
            return true;
        }
        DelayTimer = VoiceData.ChooseFrom(CurrentVoice.Delay, 0);
        // for mysterious reasons, pitch must be set after playing:
        // Game1.playSound(string, int?) does not respect the int parameter
        Game1.sounds.PlayLocal(CurrentVoice.CueName, null, null, null,
                SoundContext.Default, out ActiveCue);
        ActiveCue.Pitch = VoiceData.ChooseFrom(CurrentVoice.Pitch, 0) / 1200f;
        ActiveCue.Volume *= VoiceData.ChooseFrom(CurrentVoice.Volume, 1.0f);
        return true;
    }
}

