# fur2uge
## Convert Furnace .uge modules into hUGETracker .uge modules

fur2uge is a conversion tool for Furnace .uge modules to hUGETracker .uge. The tool is designed to allow [Furnace](https://github.com/tildearrow/furnace) users to convert their project files to [hUGETracker](https://github.com/SuperDisk/hUGETracker) project files, so that they can be exported for homebrew use (including [GB Studio](https://www.gbstudio.dev/)).

## Features

- Automatic volume column->effect conversion (If an effect is also on the row, the volume will get overwritten)
- All .uge effects supported, minus Callback Routines and Global Volume
- Note that this is not a perfect conversion tool: It is designed to convert at least 90% of the work; some adjustments may be expected to get it to sound ideal.

## Usage
### Casual Usage
Place all of your prepared .fur files in the /input/ folder, located at the same location that the program is. Double-click on "convert.bat" to get the files in the /output/ folder.
### Terminal Usage
``Fur2Uge <input>.fur <output>.uge``

You can use the ``-d <DPath>`` flag to output the zlib-decompressed .fur file, too. If ``<DPath>`` is not specified, the program will output the decompressed .fur to the output .uge file's directory.

## Caveats

- All patterns must have 64 rows. They cannot be increased or decreased. However, cutting the pattern short with Dxx and Bxx is allowed.
- Only up to 16 waveforms are allowed. They must be exactly 32 blocks in length, with a height of 16 (0-F)
- Only hardware envelopes are allowed for Pulse 1, Pulse 2, and Noise. “Initialize Envelope on Every Note” must be enabled.
- Hardware sequence is allowed, but only on Pulse 1, and only for one tick (do not add more than one–the rest will be ignored!)
- Macros are not allowed for any of the channels, with a few exceptions:
- On the Pulse Channels, Duty Cycle may have a length of 1
-- It will otherwise default to a 12.5% Pulse
- On Wave Channel, Wave Macro must have a length of 1.
-- Also on Wave Channel, Volume Macro must have a length of 1.
- The effect column may not be expanded for any channel: Only one effect is allowed at any given time.
- No subsongs are allowed.
- Messing with Virtual Tempo/Base Tempo is not allowed. Only use Speed (For now… Unless I figure out the song speed/tempo math between both trackers)
- Volume may only be defined if an effect is not on the same row for the channel. Axy (Volume Slide) is an exception, however.
- Volume and Axy may only be used whenever a note is played. This is to ensure that audio output is converted accurately and as expected.
- No more than 16 different unique instruments may be played for the entire song. A single instrument playing on all 4 channels will count as 3 instruments (Pulse, Wave, Noise).

—
# Implementation
Each channel must be handled one at a time, row by row. Every new instrument must get added and considered on a per-channel basis (Pulses get merged into one instrument list)
This is because the channels can share instruments, but Wave has different rules in hUGETracker. Wave needs its own list of instruments.
### Supported effects:
00xy - Arpeggio
01xx - Portamento Up
02xx - Portamento Down
03xx - Tone Portamento
04xx - Vibrato

EDxx - Note Delay
80xx - Set Panning
12xx - Set Duty Cycle
0Axy - Volume Slide
0Bxx - Position Jump

0Dxx - Pattern Break
ECxx - Note Cut
0Fxx - Set Speed

- FFxx (Stop song) is not supported. If you want the song to stop, create another row and loop it forever with 0Bxx.

## Building

There are no dependencies required, beyond .NET and C#: Clone the project and build in your favorite IDE (e.g. Visual Studio)

## License

MIT