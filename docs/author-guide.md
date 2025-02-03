# Vox Box - Author Guide

This document explains how to use this framework to give characters voices in
Stardew Valley.


## Contents

* [Introduction](#introduction)
* [Type 1: Voice Data](#type-1-voice-data)
  * [Content Patcher Example: Type 1](#content-patcher-example-type-1)
  * [Dialogue Command](#dialogue-command)
* [Type 2: Line Readings](#type-2-line-readings)
  * [Content Patcher Example - Type 2](#content-patcher-example-type-2)
* [Caveats and Advice](#caveats-and-advice)


## Introduction

Vox Box works by providing data assets for other mods to edit. At this time,
those mods are expected to use Content Patcher to perform their edits.

There are two different ways to define character voices:

1. Voice data, which sets general voice parameters to apply to an NPC.
2. Line readings, which sets specific audio to apply to specific dialogues.


## Type 1: Voice Data

This type of voice allows you to replace the vanilla typing noise (a fixed
sound effect that is played every 30 ms, which is typically two frames) with
sounds of your own choosing. You can use an existing audio cue or add your own,
and you can control pitch, volume, and how long the game waits between plays.
The goal of this feature is to let you give characters voices like the ones in
*Banjo-Kazooie* or *Animal Crossing*.

Voice data defined this way is tied to a particular character, and every time
they speak in a dialogue box, their voice data will be used instead of the
vanilla sounds.

To define voice data for a character, target the following asset:

```
Mods/ichortower.VoxBox/VoiceData
```

The asset is a `string->object` dictionary. The `string` keys are NPC internal
names, and the model (object) has the following fields:

<table>

<tr>
<th>Field</th>
<th>Type</th>
<th>Purpose</th>
</tr>

<tr>
<td><code>CueName</code></td>
<td>string</td>
<td>

The name of the audio cue to play as this NPC's speech sounds. This can be a
cue with a single sound, like the vanilla cue `dialogueCharacter`, or it can
include multiple sounds, which will be played randomly (I recommend setting up
your cues this way, to provide more character to the speech effect).

See [Modding:Audio on the Stardew Valley
Wiki](https://stardewvalleywiki.com/Modding:Audio) for more detail on how to
set up your cues in `Data/AudioChanges`. But, **it is strongly advised to set
StreamedVorbis to false for any .ogg files you use in your VoiceData cues**
(see [Caveats and Advice](#caveats-and-advice) for more).

*Default:* `"dialogueCharacter"`

</td>
</tr>

<tr>
<td><code>Delay</code></td>
<td>array(integer)</td>
<td>

This array of integers specifies a set of possible times (in milliseconds) to
wait between plays of CueName. Each time the cue is played, a value is picked
from this list at random, and the next play will not occur until (at least)
that much time has passed.

If this field is set to an empty array (`[]`), then this mod will wait until
the previous instance of the cue has finished playing before playing it again.
In my experience, cues take longer than I expect to finish playing, so this can
cause longer delays than you might wish; therefore, I recommend specifying
values here.

Note also that the function controlling this is called only once per frame, so
it has a minimum resolution of 16.67 milliseconds; therefore, for instance, the
value `10` is no different than the value `0`.

*Default:* `[30]`

</td>
</tr>

<tr>
<td><code>Pitch</code></td>
<td>array(integer)</td>
<td>

This array of integers specifies a set of possible adjustments to the pitch
used for playback of CueName. Each time the cue is played, a value is picked
from this list at random and added to the default pitch of 1200.

Since this pitch is additive, negative values will lower the pitch, and
positive ones will raise it. The allowable range is from -1200 (down one
octave) to +1200 (up one octave). Each 100 units of pitch is one half-step
(semitone).

If this field is set to an empty array (`[]`), then this mod will use a pitch
adjustment of `0` for every play, just as if you had specified `[0]`.

I expect that most voice data entries will either specify values here *or* use
a cue with multiple different backing sounds, but you are free to do both if
you wish.

*Default:* `[]`

</td>
</tr>

<tr>
<td><code>Volume</code></td>
<td>array(float)</td>
<td>

This array of floats specifies a set of possible volume multipliers applied to
the cue. Each time the cue is played, a value is picked from this list at
random.

Since it is a multiplier, `0.0` will generate complete silence, `1.0` will have
no effect, and the player's perception of increased volume will likely depend
on where their game volume sliders are set. **Use this setting with caution**;
in most cases, you should omit this in the general voice data. The reason it is
here is to allow [the special dialogue command](#dialogue-command) to override
it to add expression to type 1 voice lines.

If this field is set to an empty array (`[]`), then this mod will use a volume
multiplier of `1.0` for every play, just as if you had specified `[1.0]`.

*Default:* `[]`

</td>
</tr>

</table>

All fields in the model are optional; if omitted, the default values reproduce
the vanilla typing sound, which is the cue `dialogueCharacter` played every 30
milliseconds (typically, every other frame).


### Content Patcher Example: Type 1

Here's one way you could define a character voice:

```js
{
  "Target": "Mods/ichortower.VoxBox/VoiceData",
  "Action": "EditData",
  "Entries": {
    "Lewis": {
      "CueName": "cow",
      "Delay": [400, 600],
      "Pitch": [100],
    }
  }
}
```

This makes Lewis automatically play cow moos (pitched up slightly) whenever he
talks, which is a silly example for illustration purposes only.

You can also use your own cues (the intended approach):

```js
{
  "Target": "Mods/ichortower.VoxBox/VoiceData",
  "Action": "EditData",
  "Entries": {
    "Abigail": {
      "CueName": "{{ModId}}_AbigailVoiceBlurbs",
      "Delay": [30, 50, 70]
    }
  }
},
{
  "Target": "Data/AudioChanges",
  "Action": "EditData",
  "Entries": {
    "{{ModId}}_AbigailVoiceBlurbs": {
      "Id": "{{ModId}}_AbigailVoiceBlurbs",
      "FilePaths": [
        "{{AbsoluteFilePath: assets/Abigail/voice01.ogg}}",
        "{{AbsoluteFilePath: assets/Abigail/voice02.ogg}}",
        "{{AbsoluteFilePath: assets/Abigail/voice03.ogg}}"
      ],
      "Category": "Sound",
      "StreamedVorbis": false
    }
  }
}
```


## Type 2: Line Readings

This type of voice allows you to specify a single audio cue that will play when
a particular dialogue line is displayed. When a matching dialogue is shown, all
other dialogue sounds are ignored and the given cue is played exactly once. The
goal of this feature is to let you include recorded voice acting, foleys, other
sound effects, etc., which should take the place of any vanilla or type 1 audio
during the text display.

Line readings defined this way are tied to the ID (see below) of the dialogue
itself, and take priority over type 1 voice data.

### Content Patcher Example: Type 2

## Caveats and Advice

StreamedVorbis not recommended for VoiceData (clips are replayed frequently)
StreamedVorbis has minimum length
event command matching is a bit unwieldy
you can use translation keys in event command `speak`
see the sample pack!
