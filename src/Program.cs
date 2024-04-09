using System.IO;
using static Fur2Uge.FurFile;
using static Fur2Uge.UgeFile;

namespace Fur2Uge
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string fIn = "";
            string fOut = "";
            bool dumpCompressedFur = false;
            string decompressedFurPath = "";

            // File in/out arg parsing
            if (args.Length >= 2)
            {
                fIn = args[0];
                fOut = args[1];

                // Create the directories, if they do not exist.
                bool exists = System.IO.Directory.Exists(Path.GetDirectoryName(fIn));
                if (!exists)
                    System.IO.Directory.CreateDirectory(Path.GetDirectoryName(fIn));
                exists = System.IO.Directory.Exists(Path.GetDirectoryName(fOut));
                if (!exists)
                    System.IO.Directory.CreateDirectory(Path.GetDirectoryName(fOut));
            }
            if (args.Length >= 3)
            {
                if (args[2].Equals("-d")) {
                    dumpCompressedFur = true;
                    try
                    {
                        decompressedFurPath = args[3];
                    } catch(IndexOutOfRangeException e)
                    {
                        decompressedFurPath = Path.GetDirectoryName(fOut);
                    }
                }
            }

            // Read the .fur into memory
            FurFile furFile = new FurFile(fIn, dumpCompressedFur, decompressedFurPath);

            // Prepare a .uge data tree to modify
            UgeFile ugeFile = new UgeFile();

            // Parse the .fur file, and then populate its data into the .uge data tree we created
            ParseFur(furFile, ugeFile);

            // Write the .uge data to a binary.
            ugeFile.Write(fOut);
        }

        public static void ParseFur(FurFile furFile, UgeFile ugeFile)
        {
            FurModuleInfo moduleInfo = furFile.GetModuleInfo();

            // Grab the first song
            FurSong furSong = furFile.GetSong(0);

            // Ensure we are dealing with a valid GB Module
            if (moduleInfo.SoundChipTypeList[0] != FurChipType.GAME_BOY)
                throw new Exception("This .fur is not a GB module! Aborting...");
            if (furSong.PatternLen != 64)
                throw new Exception("Invalid pattern count! It MUST be set to 64! Aborting...");

            // Grab the header info
            ugeFile.Header.SongArtist.Val = moduleInfo.GlobalAuthor;
            ugeFile.Header.SongName.Val = moduleInfo.GlobalModuleName;
            ugeFile.Header.SongComment.Val = moduleInfo.SongComment;

            var patCon = ugeFile.GetUgeSongPatternController();

            // Populate the module Wavetables
            List<FurWavetable> furWTs = moduleInfo.GlobalWavetables;
            for (var i = 0; i < furWTs.Count; i++)
            {
                if (i > 15)
                    break;
                byte[] furWT = furWTs[i].EmitBytes();
                if (furWT.Length > 32)
                    throw new Exception(string.Format("Wavetable is too long! Wave: {0}", i));
                ugeFile.SetWavetable(i, furWT);
            }

            // Set the project speed
            ugeFile.SetSpeed(furSong.Speed1);

            // Populate the song's order table (For the first chip only [This MUST be GB chip!])
            // Might end up optimizing this in the future...
            var orderTableHeight = furSong.OrderTable.GetLength(1);
            for (var i = 0; i < orderTableHeight; i++)
            {
                ugeFile.AddNewOrderRow(1);
            }

            // Keep track of all the new instruments we define, on a per channel basis (Pulse 1 & 2 count as the same channel in this case)
            List<FurInstrument> furPulseInstruments = new List<FurInstrument>();
            List<FurInstrument> furWaveInstruments = new List<FurInstrument>();
            List<FurInstrument> furNoiseInstruments = new List<FurInstrument>();
            UgeGBChannelState[] ugeGBChannelStates = new UgeGBChannelState[4];
            for (var i = 0; i < ugeGBChannelStates.Length; i++)
                ugeGBChannelStates[i] = new UgeGBChannelState(i);

            for (int orderRow = 0; orderRow < orderTableHeight; orderRow++)
            {
                for (int chanID = 0; chanID < 4; chanID++)
                {
                    int orderID = furSong.OrderTable[chanID, orderRow];
                    FurPatternData thisPattern = furSong.Channels[chanID].GetPattern(orderID);
                    List<FurPatternRowData> patRowData = thisPattern.GetAllRowData();

                    /// Read all the pattern data and store it into the uge file, in order.
                    foreach (FurPatternRowData thisRowData in patRowData)
                    {
                        var rowIndex = thisRowData.GetRowIndex();

                        // Notes first
                        int noteVal = thisRowData.GetNoteVal();
                        if (noteVal >= 0)
                        {
                            patCon.SetNote((GBChannel)chanID, (byte)orderID, rowIndex, FurNoteToUgeNote(noteVal));
                        }

                        // Now instruments
                        int instrVal = thisRowData.GetInstrumentVal();
                        bool duplicateInstr = false;
                        int duplicateCheckingIndex = 0;
                        if (instrVal >= 0)
                        {
                            // Keep track of every unique instrument used (will be defined later)
                            switch (chanID)
                            {
                                default:
                                case 0:
                                case 1:
                                    foreach (FurInstrument inst in furPulseInstruments)
                                    {
                                        if (inst == moduleInfo.GlobalInstruments[instrVal])
                                        {
                                            duplicateInstr = true;
                                            instrVal = duplicateCheckingIndex;
                                            break;
                                        }
                                        duplicateCheckingIndex++;
                                    }
                                    if (!duplicateInstr)
                                    {
                                        furPulseInstruments.Add(moduleInfo.GlobalInstruments[instrVal]);
                                        instrVal = furPulseInstruments.Count - 1;
                                    }
                                    break;
                                case 2:
                                    foreach (FurInstrument inst in furWaveInstruments)
                                    {
                                        if (inst == moduleInfo.GlobalInstruments[instrVal])
                                        {
                                            duplicateInstr = true;
                                            instrVal = duplicateCheckingIndex;
                                            break;
                                        }
                                        duplicateCheckingIndex++;
                                    }
                                    if (!duplicateInstr)
                                    {
                                        furWaveInstruments.Add(moduleInfo.GlobalInstruments[instrVal]);
                                        instrVal = furWaveInstruments.Count - 1;
                                    }
                                    break;
                                case 3:
                                    foreach (FurInstrument inst in furNoiseInstruments)
                                    {
                                        if (inst == moduleInfo.GlobalInstruments[instrVal])
                                        {
                                            duplicateInstr = true;
                                            instrVal = duplicateCheckingIndex;
                                            break;
                                        }
                                        duplicateCheckingIndex++;
                                    }
                                    if (!duplicateInstr)
                                    {
                                        furNoiseInstruments.Add(moduleInfo.GlobalInstruments[instrVal]);
                                        instrVal = furNoiseInstruments.Count - 1;
                                    }
                                    break;
                            }
                            patCon.SetInstrument((GBChannel)chanID, (byte)orderID, rowIndex, instrVal);
                        }

                        // Now copy the volume column (which might or might not get overwritten if an effect is present)
                        int volVal = thisRowData.GetVolume();
                        if (volVal >= 0)
                            patCon.SetEffect((GBChannel)chanID, (byte)orderID, rowIndex, UgeEffectTable.SET_VOL, (byte)volVal);

                        // Finally, copy all of the effect columns for this channel's row
                        List<byte> furAllChannelFxColumns = thisRowData.GetEffectData();

                        byte furFxCmd;
                        byte furFxVal;
                        byte ugeFXVal;

                        // Before writing any new effects, update the state of the channel,
                        // just in case we turned off any effects on this row, for any FX column...
                        for (var i = 0; i < furAllChannelFxColumns.Count; i += 2)
                        {
                            furFxCmd = furAllChannelFxColumns[i];
                            furFxVal = furAllChannelFxColumns[i + 1];

                            if (furFxVal == 0x00)
                            {
                                // We're turning off a command. Which command is this?
                                switch (furFxCmd)
                                {
                                    default:
                                    case 0x0F: // Set Speed
                                    case 0x0B: // Jump to Pattern
                                    case 0x0D: // Pattern Break
                                    case 0x12: // Set Duty Cycle
                                    case 0xEC: // Note Cut
                                    case 0xED: // Note Delay
                                        // Not supported; Ignore this command.
                                        break;
                                    case 0x00: // Arps
                                        break;
                                    case 0x01: // Port Slide Up
                                        break;
                                    case 0x02: // Port Slide Down
                                        break;
                                    case 0x03: // Tone portamento
                                        break;
                                    case 0x04: // Vibrato
                                        break;
                                    case 0x0A: // Vol slide
                                        break;
                                    case 0x08: // Hardware Pan
                                    case 0x80: // Software pan
                                        // Update this channel's pan values
                                        bool rightSpeakerOn = false;
                                        bool leftSpeakerOn = false;
                                        ugeGBChannelStates[chanID].SetPan(leftSpeakerOn, rightSpeakerOn);

                                        // Update the pan value based on all of the GB Channels' current pan states
                                        ugeFXVal = 0x0;
                                        foreach (UgeGBChannelState chanState in ugeGBChannelStates)
                                        {
                                            ugeFXVal |= chanState.GetPan();
                                        }
                                        break;
                                }
                            }
                        }

                        // Now that the channel state is up-to-date, we should parse the command in FX Slot 1. We will ignore any other commands in the other FX slots.
                        furFxCmd = furAllChannelFxColumns[0];
                        furFxVal = furAllChannelFxColumns[1];

                        UgeEffectTable? ugeFxCmd = null;
                        ugeFXVal = furFxVal;

                        switch (furFxCmd)
                        {
                            default:
                                // Not supported; Ignore this command.
                                break;
                            case 0x00:
                                ugeFxCmd = UgeEffectTable.ARPEGGIO;
                                break;
                            case 0x01:
                                ugeFxCmd = UgeEffectTable.PORTAMENTO_UP;
                                break;
                            case 0x02:
                                ugeFxCmd = UgeEffectTable.PORTAMENTO_DOWN;
                                break;
                            case 0x03:
                                ugeFxCmd = UgeEffectTable.TONE_PORTAMENTO;
                                break;
                            case 0x04:
                                ugeFxCmd = UgeEffectTable.VIBRATO;
                                break;
                            case 0x08:
                                ugeFxCmd = UgeEffectTable.SET_PANNING;

                                // Update this channel's pan values
                                bool rightSpeakerOn = ((byte)(furFxVal & 0x0F) > 0) ? true : false;
                                bool leftSpeakerOn = ((byte)((furFxVal & 0xF0) >> 4) > 0) ? true : false;
                                ugeGBChannelStates[chanID].SetPan(leftSpeakerOn, rightSpeakerOn);

                                // Update the pan value based on all of the GB Channels' current pan states
                                ugeFXVal = 0x0;
                                foreach (UgeGBChannelState chanState in ugeGBChannelStates)
                                {
                                    ugeFXVal |= chanState.GetPan();
                                }
                                break;
                            case 0x0A:
                                ugeFxCmd = UgeEffectTable.VOLUME_SLIDE;
                                break;
                            case 0x0B:
                                ugeFxCmd = UgeEffectTable.POSITION_JUMP;
                                ugeFXVal++;
                                break;
                            case 0x0D:
                                ugeFxCmd = UgeEffectTable.PATTERN_BREAK;
                                ugeFXVal++;
                                break;
                            case 0x0F:
                                ugeFxCmd = UgeEffectTable.SET_SPEED;
                                break;
                            case 0x12:
                                ugeFxCmd = UgeEffectTable.SET_DUTY_CYCLE;
                                break;
                            case 0x80:
                                ugeFxCmd = UgeEffectTable.SET_PANNING;
                                break;
                            case 0xEC:
                                ugeFxCmd = UgeEffectTable.NOTE_CUT;
                                break;
                            case 0xED:
                                ugeFxCmd = UgeEffectTable.NOTE_DELAY;
                                break;
                        }

                        if (ugeFXVal >= 0 && ugeFxCmd != null)
                            patCon.SetEffect((GBChannel)chanID, (byte)orderID, rowIndex, (UgeEffectTable)ugeFxCmd, ugeFXVal);

                    }
                }
            }

            // By this point, we know every single instrument that has been used in the song.
            // Now, we will create those instruments.
            int instrIndex = 0;
            foreach (FurInstrument pulseInst in furPulseInstruments)
            {
                string name = pulseInst.GetName();
                FurInstrGB gbInstr = pulseInst.GetInstrGB();
                if (gbInstr == null)
                    throw new Exception(string.Format("Invalid GB Pulse Instrument: {0}", name));

                var gbParams = gbInstr.GetParams(); // returns (_gbEnvLen, _gbEnvDir, _gbEnvVol, _gbSndLen, _gbFlags, _gbHWSeqLen, _gbHWSeqCmds)
                var hwSeq = gbInstr.GetHWSeq();
                List<FurInstrMacro> macros = pulseInst.GetMacros();


                //return (_gbHWSeqSweepSpeed, _gbHWSeqSweepDir, _gbHWSeqShiftVal);
                UgeInstrument ugePulse = new UgeInstrument(name, (uint)UgeInstrumentType.PULSE, gbParams.Item1, (gbParams.Item2 > 0x1) ? 0U : 1U,
                    gbParams.Item3, gbParams.Item4, gbParams.Item5, gbParams.Item6, gbParams.Item7, macros, null
                    );

                ugeFile.SetPulseInstr(instrIndex, ugePulse);
                instrIndex++;
            }

            instrIndex = 0;

            foreach (FurInstrument waveInst in furWaveInstruments)
            {
                string name = waveInst.GetName();
                FurInstrGB gbInstr = waveInst.GetInstrGB();
                FurInstrWS wsInstr = waveInst.GetInstrWS();
                if (gbInstr == null)
                    throw new Exception(string.Format("Invalid GB Wave Instrument: {0}", name));

                var gbParams = gbInstr.GetParams(); // returns (_gbEnvLen, _gbEnvDir, _gbEnvVol, _gbSndLen, _gbFlags, _gbHWSeqLen, _gbHWSeqCmds)
                var wsParams = wsInstr.GetParams(); // returns (_wsFirstWave, _wsSecondWave, _wsRateDivider, _wsEffect, _singleEffect, _wsEnabled, _wsGlobal, _wsSpeed, _wsParam1, _wsParam2, _wsParam3, _wsParam4)
                List<FurInstrMacro> macros = waveInst.GetMacros();

                uint wtVol = (uint)UgeWaveVolumes.FULL;
                var wtIndex = 0x0;

                var wsEnabled = wsParams.Item6;
                if (wsEnabled)
                    wtIndex = wsParams.Item1;

                UgeInstrument ugeWave = new UgeInstrument(name, (uint)UgeInstrumentType.WAVETABLE, wtVol, (uint)wtIndex, macros);

                ugeFile.SetWaveInstr(instrIndex, ugeWave);
                instrIndex++;
            }

            instrIndex = 0;
            foreach (FurInstrument noiseInst in furNoiseInstruments)
            {
                string name = noiseInst.GetName();
                FurInstrGB gbInstr = noiseInst.GetInstrGB();
                if (gbInstr == null)
                    throw new Exception(string.Format("Invalid GB Noise Instrument: {0}", name));

                var gbParams = gbInstr.GetParams(); // returns (_gbEnvLen, _gbEnvDir, _gbEnvVol, _gbSndLen, _gbFlags, _gbHWSeqLen, _gbHWSeqCmds)
                List<FurInstrMacro> macros = noiseInst.GetMacros();

                UgeInstrument ugeNoise = new UgeInstrument(name, (uint)UgeInstrumentType.NOISE, gbParams.Item1, (gbParams.Item2 > 0x1) ? 0U : 1U, gbParams.Item3, gbParams.Item4, gbParams.Item5, gbParams.Item6, gbParams.Item7, macros, null);

                ugeFile.SetNoiseInstr(instrIndex, ugeNoise);
                instrIndex++;
            }
        }

        private static UgeNoteTable FurNoteToUgeNote(int furNoteVal)
        {
            if (furNoteVal == 180)
                return (UgeNoteTable)180; // Handle this note off event as a note cut effect later...
            return (UgeNoteTable)(furNoteVal - 84);
        }
    }
}
