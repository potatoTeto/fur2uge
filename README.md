# fur2uge
## Convert Furnace .fur modules into hUGETracker .uge modules
fur2uge is a conversion tool designed to convert [Furnace](https://github.com/tildearrow/furnace) .fur project files into [hUGETracker](https://github.com/SuperDisk/hUGETracker) .uge project files, so that they can be exported for homebrew use (including [GB Studio](https://www.gbstudio.dev/)).

# Download
https://github.com/potatoTeto/fur2uge/releases

## Features
- Automatic volume column->effect conversion (If an effect is also on the row, the volume will get overwritten)
- All .uge effects supported, minus Callback Routines and Global Volume
- Note that this is not a perfect conversion tool: It is designed to convert at least 90% of the work; some adjustments may be expected to get it to sound ideal. To ensure reliable debugging/accuracy, please remember to test your music in the [bgb emulator](https://bgb.bircd.org/)!
- Subsongs are supported. If multiple subsongs are present, each will be written as a separate `.uge` file with an index and, if available, the song name included in the filename. If a subsong name is empty, no extra underscore is added before the extension.

**Example:**

If your input path is:

    input/ExampleFilename.fur

And the `.fur` contains 3 subsongs:

    Forest Intro  
    Boss Battle  
    (unnamed subsong)  

The following `.uge` files will be created:

    output/ExampleFilename_Song1_Forest_Intro.uge  
    output/ExampleFilename_Song2_Boss_Battle.uge  
    output/ExampleFilename_Song3.uge  

If there is exactly one subsong, the original input filename is used for the output:

    output/ExampleFilename.uge

## Usage
### Casual Usage
Place all of your prepared .fur files in the ``/input/`` folder, located at the same location that the program is. Double-click on ``convert.bat`` to get the files in the ``/output/`` folder.
### Terminal Usage
``fur2Uge --i <input>.fur --o <output>.uge``

### Command Line Arguments
``--d <DPath>`` - Output the zlib-decompressed .fur file, too. If ``<DPath>`` is not specified, the program will output the decompressed .fur to the output .uge file's directory.

``--pan <channelNumber>`` - Pan Macros will only work on the channel specified by <channelNumber> (0-3). By default, it only works on Pulse 1.

``--v <0 or 1>`` - If **Disabled (0)**, a new instrument will **not** be made when considering the .fur's volume column. Please keep in mind that .uge limits users to 15 instruments per channel type (Duty/Wave/Noise). By default, this is **Enabled (1)**.

``--u <VersionNum 0-6>`` - Specify the output .uge Version. (Versions <=5 do not support Sub-patterns/Furnace macros!)

``--gbstmode <0 or 1>`` - By default, tempo conversion assumes you're targeting GBStudio, which uses a Timer Divider locked at ``192`` and may produce slightly off BPM results. Use this flag to control GBStudio mode explicitly: ``0`` disables GBStudio mode and allows precise BPM matching using standard hUGETracker timer calculations, while ``1`` enables GBStudio mode (default behavior). **GBStudio users must manually correct song speeds by spamming Fxx commands throughout the song as needed, either before or after conversion.**

## Caveats
- All patterns must have 64 rows. They cannot be increased or decreased. However, cutting the pattern short with Dxx and Bxx is allowed.
- No more than 15 different unique instruments may be used per channel type (Pulse, Wave, Noise). A single instrument playing on all 4 channels will count as 3 instruments (1 Pulse, 1 Wave, 1 Noise).
- Only up to 16 waveforms are allowed. They must be exactly 32 blocks in length, with a height of 16 (0-F)
- Only hardware envelopes are allowed for Pulse 1, Pulse 2, and Noise. “Initialize Envelope on Every Note” must be enabled.
- Hardware sequence is allowed, but only on Pulse 1, and only for one tick (do not add more than one–the rest will be ignored!)
- The effect column may not be expanded for any channel: Only one effect is allowed at any given time.
- Tick Rate is recommended to be set to 60hz exactly. The tool will attempt to convert any custom Tick Rates, but expect potential inaccuracies if you try using Base Tempo/custom Hz rates. Should work in most cases though.
- Volume may only be defined if an effect is not on the same row for the channel. Axy (Volume Slide) is an exception. Both Volume and Axy may only be used whenever a note is played. This is to ensure that audio output is converted accurately and as expected. This can be worked around by creating instruments that assign specific volume levels/volume ramps. Just keep in mind the 15-instrument limit mentioned in the previous bullet point above.
- I did a few hacks to make the volume column work: hUGETracker does not natively have a volume column, so I made the program automatically create instruments if the context requires a lower/higher volume that hasn't been defined yet... Don't go crazy w/the volume column, or you may hit the 15-instrument limit very quickly!
---
Macros are partially supported, with a few more things to note:
- Wave Macros may be utilized for setting the waveform on the Wavetable channel. Please note that the program will prioritize the first value in the macro. Alternatively, you can employ the Wave Sequencer for the same purpose, but it will only recognize the initial wave, devoid of any modulation, speed adjustment, or waveform morphing.
- All macros must either A) share the same length and loop point as all the other macros in the instrument, or B) Except for the longest macro within the instrument, all other macros must have loop points shorter than the longest one. This is crucial as the program heavily utilizes Subpatterns to enable macro support. While the program will adhere to the user's specifications, deviating from this restriction may yield unexpected auditory outcomes.
- Due to driver and design limitations, Panning Macros only work on the Pulse 1 channel and require the entire song to be in mono during their use.
To apply a Panning Macro to a different channel, use the ``--pan <channel_number 0–3>`` argument when running the program.

## Supported Furnace Effects:
- 00xy - Arpeggio
- 01xx - Portamento Up
- 02xx - Portamento Down
- 03xx - Tone Portamento
- 04xx - Vibrato
- 08xx - Set Pan (Hardware)
- 0Axy - Volume Slide
- 0Bxx - Position Jump
- 0Dxx - Pattern Break
- 0Fxx - Set Speed
- 12xx - Set Duty Cycle
- 80xx - Set Pan (Software)
- ECxx - Note Cut
- EDxx - Note Delay

**FFxx (Stop song) is not supported.** If you want the song to stop, create another row and loop it forever with 0Bxx.

## Building

There are no dependencies required, beyond .NET and C#: Clone the project and build in your favorite IDE (e.g. Visual Studio)

A Powershell script has been included to output all target platforms to ``publish/output``. Simply type:

```
.\build-and-package.ps1 -Version "v1.2.3"
```
(``1.2.3`` is an example of the version string to export with.)

## License

fur2uge Source Code - MIT

All included demo .fur songs - CC BY-NC 4.0
