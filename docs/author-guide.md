# Vox Box - Author Guide

This document explains how to use this framework to give characters voices in
Stardew Valley.


## Contents

* [Introduction](#introduction)
* [Type 1: Voice Data](#type-1-voice-data)
  * [Content Patcher Example: Type 1](#content-patcher-example-type-1)
* [Type 1i: Line Expressions](#type-1i-line-expressions)
  * [Dialogue IDs](#dialogue-ids)
  * [Content Patcher Example: Type 1i](#content-patcher-example-type-1i)
  * [Dialogue Command](#dialogue-command)
* [Type 2: Line Readings](#type-2-line-readings)
  * [Content Patcher Example - Type 2](#content-patcher-example-type-2)
* [Caveats and Advice](#caveats-and-advice)


## Introduction

Vox Box works by providing data assets for other mods to edit. At this time,
those mods are expected to use Content Patcher to perform their edits.

There are two and a half different ways to define character voices:

1. Voice data, which sets general voice parameters to apply to an NPC.
    1. Line expressions, to override an NPC's general voice data for specific
    lines.
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
names, and the `object` model has the following fields:

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
set up your cues in `Data/AudioChanges`. But, **you should set StreamedVorbis
to false for any .ogg files you use in your VoiceData cues** (see [Caveats and
Advice](#caveats-and-advice) for more).

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
value `10` produces the same result as the value `0`.

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
here is to allow [the special dialogue command](#dialogue-command) and [Line
Expressions](#type-1i-line-expressions) to override it to add expression to
type 1 voice lines.

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

## Type 1i: Line Expressions

This type has the same effect as Type 1: creating voices out of
randomly-selected sounds. The difference is that the voice data is applied to a
specific dialogue line instead of broadly to an NPC. This lets you, for
example, make one line louder and higher-pitched to reflect an NPC saying
something angrily.

To add expressions, target the following asset:

```
Mods/ichortower.VoxBox/LineExpressions
```

The asset is a `string->list(object)` dictionary. The `string` keys are
dialogue IDs (see below), and the values are lists (`[]`) of objects following
the same model described above for Type 1 voice data. The only difference in
the object model is that in this asset, the default value for every field is
`null`, which means not to change that field (since this asset is specifying
*overrides*).

Within the list, the index of each object determines which segment of the
dialogue line it applies to. A dialogue line might look like this:

```
"Fall seeds are here! Crops don't grow in winter so this is your last shot until spring.#$b#Better go all out, huh?"
```

This will be displayed using two dialogue boxes (they are separated by the
`#$b#`). When targeting this line (which happens to be
`Characters/Dialogue/Pierre:fall_Mon`), you can provide two objects in the
list, which will target the first and second pieces, respectively.

Setting an object to `null` causes no override to take place, and the mod will
use either the NPC's general Type 1 voice data, or the vanilla sounds.
Likewise, any fields within a provided object that are `null` (or not
specified) will cause no change, so you need only provide the fields you wish
to modify.


### Dialogue IDs

When patching in Line Expressions (or [Line Readings](#type-2-line-readings),
below), your patch will need to use dialogue IDs as keys. When the dialogue
matching the ID is shown, this mod will automatically override the vanilla or
Type 1 dialogue sounds using the data provided.

A dialogue ID for Vox Box's purposes comes in two formats:

1. A **translation key** identifying a dialogue string in a game asset, in the
   form `<asset name>:<key>`\
   (example: `Characters/Dialogue/Marnie:Fri6`).
2. An **event key** identifying a dialogue line in an event, in the form
   `<event id>:<full text of event command>`\
   (example: `45:speak Lewis \"Hey! What do you think you're doing? That's private property!$4\"`).

Unfortunately, for infelicitous reasons, you do have to provide the full,
expanded event command as the key when using the second format. This can be
unwieldy, but [there may be alternatives](#caveats-and-advice).


### Content Patcher Example: Type 1i

Here's how you might patch a line expression into the example from Pierre's
dialogue, above:

```js
{
  "Target": "Mods/ichortower.VoxBox/LineExpressions",
  "Action": "EditData",
  "Entries": {
    "Characters/Dialogue/Pierre:fall_Mon": [
      null,
      { "Pitch": [100], "Volume": [1.2] }
    ]
  }
}
```

This will leave the first line unchanged, and make him say the second one
("Better go all out, huh?") slightly louder and slightly higher pitched.


### Dialogue Command

The dialogue command is not currently available for use. Future betas and/or
the 1.0 release will probably include it, pending feedback.


## Type 2: Line Readings

This type of voice allows you to specify a single audio cue that will play when
a particular dialogue line is displayed. When a matching dialogue is shown, all
other dialogue sounds are ignored and the given cue is played exactly once. The
goal of this feature is to let you include recorded voice acting, foleys, other
sound effects, etc., which will take the place of any vanilla, type 1, or type
1i audio during the text display.

To add line readings, target the following asset:

```
Mods/ichortower.VoxBox/LineReadings
```

The asset is a `string->list(object)` dictionary. The `string` keys are
[dialogue IDs](#dialogue-ids), just like line expressions, and the values are
lists (`[]`) of objects. The `object` model has the following fields:

<table>

<tr>
<th>Field</th>
<th>Type</th>
<th>Purpose</th>
</tr>

<tr>
<td><code>Delay</code></td>
<td>integer</td>
<td>

How long, in milliseconds, to wait before starting playback of the sound.

*Default:* `0`

</td>
</tr>

<tr>
<td><code>Sound</code></td>
<td>string</td>
<td>

An absolute file path of the sound file to play.

This mod expects to read an absolute file path here, and will use it to
construct a cue in the soundback on your behalf. The purpose of doing this is
to save you work; you would normally need to create your own cue for each sound
file, which would require a lot of copy and paste busywork. Instead, you
should use Content Patcher's `{{AbsoluteFilePath}}` token here and let Vox Box
make the cue.

This field is required, since it is the purpose of patching in line readings.

If you use an Ogg Vorbis (.ogg) file here, which I recommend in general, this
mod will automatically set StreamedVorbis to `true` on the resulting cue. This
can cause major problems if your file is too short (see [Caveats and
Advice](#caveats-and-advice)), so either use longer sounds or switch to .wav
for very short ones.

</td>
</tr>

</table>

Just like line expressions, the list of objects corresponds to the indexes in
the dialogue string. Set an object to null to avoid setting a line reading for
that segment.


### Content Patcher Example: Type 2

Here's what it might look like to add readings for specific lines:

```js
{
  "Target": "Mods/ichortower.VoxBox/LineReadings",
  "Action": "EditData",
  "Entries": {
    "Characters/Dialogue/Lewis:GreenRain": [
      {
        "Sound": "{{AbsoluteFilePath: assets/audio/Lewis/Dialogue_GreenRain_0.ogg}}"
      },
      {
        "Sound": "{{AbsoluteFilePath: assets/audio/Lewis/Dialogue_GreenRain_1.ogg}}"
      }
    ],
    "14:speak Haley \"Oh! @.$8\"": [
      {
        "Sound": "{{AbsoluteFilePath: assets/audio/Haley/Event14_Line0.ogg}}",
        "Delay": 220
      }
    ],
    "14:speak Haley \"The lighting is so nice right now... I had to come out and take some nature shots.\"": [
      {
        "Sound": "{{AbsoluteFilePath: assets/audio/Haley/Event14_Line1.ogg}}",
      }
    ]
  }
},
```


## Caveats and Advice

- line readings is highest priority, then expressions, then voice data
- StreamedVorbis not recommended for VoiceData (clips are replayed frequently)
- StreamedVorbis has minimum length
- event command matching is a bit unwieldy
- you can use translation keys in event command `speak`
- see the sample pack!
