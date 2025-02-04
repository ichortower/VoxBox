using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Audio;
using StardewValley.Extensions;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace ichortower.VoxBox;

internal class Patches
{
    internal static void Apply()
    {
        Harmony harmony = new(VoxBox.ModId);
        //PatchMethod(harmony, typeof(ClassToPatch),
                //nameof(ClassToPatch.Method),
                //new[]{typeof(arg1), ...}, // or null
                //nameof(Patches.MethodToApply));
        PatchMethod(harmony, typeof(StardewValley.Menus.DialogueBox),
                nameof(StardewValley.Menus.DialogueBox.update),
                null,
                nameof(Patches.DialogueBox_update_Transpiler));
        PatchMethod(harmony, typeof(StardewValley.Menus.DialogueBox),
                nameof(StardewValley.Menus.DialogueBox.beginOutro),
                null,
                nameof(Patches.DialogueBox_beginOutro_Postfix));
        PatchMethod(harmony, typeof(StardewValley.Dialogue),
                nameof(StardewValley.Dialogue.prepareCurrentDialogueForDisplay),
                null,
                nameof(Patches.Dialogue_prepareCurrentDialogueForDisplay_Postfix));
    }

    internal static IEnumerable<CodeInstruction>
        DialogueBox_update_Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase original)
    {
        MethodInfo soundcb = typeof(Patches).GetMethod(nameof(SoundCallback),
                BindingFlags.NonPublic | BindingFlags.Static);
        CodeMatcher cm = new(instructions);
        cm.MatchStartForward(
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(i => i.opcode == OpCodes.Ldfld &&
                ((FieldInfo)i.operand).Name == "characterAdvanceTimer"),
            new CodeMatch(OpCodes.Ldarg_1))
        .InsertAndAdvance(
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Call, soundcb))
        .MatchStartForward(
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(i => i.opcode == OpCodes.Ldfld &&
                ((FieldInfo)i.operand).Name == "characterIndexInDialogue"),
            new CodeMatch(OpCodes.Ldc_I4_1),
            new CodeMatch(OpCodes.Ble_S));
        int b = cm.Pos;
        List<Label> labels = cm.Labels;
        cm.MatchStartForward(
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(i => i.opcode == OpCodes.Ldfld &&
                ((FieldInfo)i.operand).Name == "transitioning"),
            new CodeMatch(OpCodes.Brtrue_S))
        .AddLabels(labels)
        .RemoveInstructionsInRange(b, cm.Pos-1);
        return cm.InstructionEnumeration();
    }

    internal static void DialogueBox_beginOutro_Postfix(
            DialogueBox __instance)
    {
        Patches.SpeechDelegate = null;
        LineReadings.ClearCurrent();
    }

    internal enum ReadMode {
        None,
        Pitch,
        Delay,
        Volume,
    }

    internal static void Dialogue_prepareCurrentDialogueForDisplay_Postfix(
            Dialogue __instance)
    {
        // end the current voice audio, unless...
        // having 2 or more in characterDialoguesBrokenUp means a line ran over the single box
        // limit and had to be split. don't stop in this case
        if (Game1.activeClickableMenu is not DialogueBox db ||
                db.characterDialoguesBrokenUp.Count < 2) {
            Patches.SpeechDelegate = null;
            LineReadings.ClearCurrent();
        }

        // leaving the dialogue command dummied out for the beta
        return;

        /*
        string text = __instance.dialogues[__instance.currentDialogueIndex].Text;
        if (!text.StartsWith($"&{VoxBox.ModId} ")) {
            return;
        }
        int loc = text.IndexOf(" - ");
        if (loc == -1) {
            Log.Warn($"Ignoring incomplete command '&{VoxBox.ModId}': " +
                    "missing closing argument '-'");
            return;
        }
        string clipped = text.Substring(loc + " - ".Length);
        __instance.dialogues[__instance.currentDialogueIndex].Text = clipped;

        string[] args = ArgUtility.SplitBySpace(text.Substring(0, loc));
        if (args.Length >= 2 && args[1].EqualsIgnoreCase("vanilla")) {
            VoiceData.CurrentVoice = VoiceEntry.Vanilla.Clone();
            return;
        }
        else if (args.Length >= 2 && args[1].EqualsIgnoreCase("default")) {
            VoiceData.CurrentVoice = VoiceData.EntryFor(__instance.speaker).Clone();
            return;
        }

        string cueName = "";
        List<int> pitches = new();
        List<int> delays = new();
        List<float> volumes = new();
        ReadMode mode = ReadMode.None;
        for (int i = 1; i < args.Length; ++i) {
            if (args[i].EqualsIgnoreCase("pitch")) {
                mode = ReadMode.Pitch;
            }
            else if (args[i].EqualsIgnoreCase("delay")) {
                mode = ReadMode.Delay;
            }
            else if (args[i].EqualsIgnoreCase("volume")) {
                mode = ReadMode.Volume;
            }
            else if (mode == ReadMode.Pitch) {
                if (!int.TryParse(args[i], out int p)) {
                    Log.Warn($"Failed to parse int from pitch value '{args[i]}'");
                    return;
                }
                pitches.Add(p);
            }
            else if (mode == ReadMode.Delay) {
                if (!int.TryParse(args[i], out int d)) {
                    Log.Warn($"Failed to parse int from delay value '{args[i]}'");
                    return;
                }
                delays.Add(d);
            }
            else if (mode == ReadMode.Volume) {
                if (!float.TryParse(args[i], out float v)) {
                    Log.Warn($"Failed to parse float from volume value '{args[i]}'");
                    return;
                }
                volumes.Add(v);
            }
            else {
                Log.Warn($"Encountered unknown argument '{args[i]}'");
                return;
            }
        }

        Action setParameters = delegate {
            if (cueName != "") {
                VoiceData.CurrentVoice.CueName = cueName;
            }
            if (pitches.Count > 0) {
                VoiceData.CurrentVoice.Pitch = pitches.ToArray();
            }
            if (delays.Count > 0) {
                VoiceData.CurrentVoice.Delay = delays.ToArray();
            }
            if (volumes.Count > 0) {
                VoiceData.CurrentVoice.Volume = volumes.ToArray();
            }
        };
        if (Game1.activeClickableMenu is not DialogueBox) {
            VoiceData.OnOpen = setParameters;
        }
        else {
            setParameters.Invoke();
        }
        */
    }


    //internal static ICue voiceCue = null;
    //internal static bool lineWasPlayed = false;

    internal static Func<DialogueBox, GameTime, bool> SpeechDelegate = null;

    internal static void SoundCallback(DialogueBox db, GameTime time)
    {
        if (!Game1.options.dialogueTyping) {
            return;
        }
        // upper bound of characterIndexInDialogue is checked at call site
        if (db.characterIndexInDialogue < 0) {
            return;
        }
        if (SpeechDelegate?.Invoke(db, time) is true) {
            return;
        }

        LineReadings.ClearCurrent();
        if (!GetEventString(out string lineKey)) {
            lineKey = db.characterDialogue?.TranslationKey;
        }
        if (string.IsNullOrEmpty(lineKey) || db.characterDialogue == null) {
            SpeechDelegate = VoiceData.BlurbSpeech;
        }
        else if (LineReadings.Data.TryGetValue(lineKey, out LineReadings.Cues)) {
            SpeechDelegate = LineReadings.LineSpeech;
        }
        else if (VoiceExpressions.Data.TryGetValue(lineKey, out VoiceExpressions.Voices)) {
            SpeechDelegate = VoiceExpressions.BlurbSpeech;
        }
        else {
            SpeechDelegate = VoiceData.BlurbSpeech;
        }

        _ = SpeechDelegate(db, time);
        return;
    }

    internal static bool GetEventString(out string ret)
    {
        if (Game1.currentLocation.currentEvent is Event e) {
            if (Game1.eventUp && e.CurrentCommand < e.eventCommands.Length) {
                ret = $"{e.id}:{e.eventCommands[e.CurrentCommand]}";
                return true;
            }
        }
        ret = null;
        return false;
    }

    private static void PatchMethod(Harmony harmony, Type t, string name,
            Type[] argTypes, string patch)
    {
        string[] parts = patch.Split("_");
        string last = parts[parts.Length-1];
        if (last != "Prefix" && last != "Postfix" && last != "Transpiler") {
            Log.Error($"Skipping patch method '{patch}': bad type '{last}'");
            return;
        }
        try {
            MethodInfo m;
            if (argTypes is null) {
                m = t.GetMethod(name,
                        BindingFlags.Public | BindingFlags.NonPublic |
                        BindingFlags.Instance | BindingFlags.Static);
            }
            else {
                m = t.GetMethod(name,
                        BindingFlags.Public | BindingFlags.NonPublic |
                        BindingFlags.Instance | BindingFlags.Static,
                        null, argTypes, null);
            }
            HarmonyMethod func = new(typeof(Patches), patch);
            if (last == "Prefix") {
                harmony.Patch(original: m, prefix: func);
            }
            else if (last == "Postfix") {
                harmony.Patch(original: m, postfix: func);
            }
            else if (last == "Transpiler") {
                harmony.Patch(original: m, transpiler: func);
            }
            Log.Trace($"Patched method '{t.Name}.{m.Name}' ({last})");
        }
        catch (Exception e) {
            Log.Error($"Patch failed ({patch}): {e}");
        }
    }
}
