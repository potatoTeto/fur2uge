using System;
using System.IO;
using System.Linq;
using System.Threading.Channels;
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




            /// Create 4 channels to simulate the song as we parse it
            UgeGBChannelState[] ugeGBChannelStates = new UgeGBChannelState[4];
            for (var i = 0; i < ugeGBChannelStates.Length; i++)
                ugeGBChannelStates[i] = new UgeGBChannelState(i);




            /// Sort the Furnace unique-per-channel order table to accomodate for hUGETracker's global pattern layout
            // Input a uge Pattern Index, get the fur pattern index
            Dictionary<int, int> furPointerLookup = new Dictionary<int, int>();

            // Input a fur Pattern Index, get the uge pattern index
            Dictionary<int, int> ugePointerLookup = new Dictionary<int, int>();

            int[,] ugeOrderTable = new int[furSong.OrderTable.GetLength(0), furSong.OrderTable.GetLength(1)];

            // Populate the table with unique indices
            for (int chanID = 0; chanID < furSong.OrderTable.GetLength(0); chanID++)
            {
                // Unique offset for each row, to differentiate them more easily during conversion
                int pointerOffset = (chanID + 1) * 0x1000;
                for (int row = 0; row < furSong.OrderTable.GetLength(1); row++)
                {
                    int furPointer = furSong.OrderTable[chanID, row] + pointerOffset;
                    if (!ugePointerLookup.ContainsKey(furPointer))
                    {
                        // ugePointer value will increment from 0, based on how many there are
                        int ugePointer = ugePointerLookup.Count;
                        ugePointerLookup[furPointer] = ugePointer;
                        furPointerLookup[ugePointer] = furPointer;
                    }
                    ugeOrderTable[chanID, row] = ugePointerLookup[furPointer];
                }
            }

            // Populate the Uge Pattern Tables
            for (int rowIndex = 0; rowIndex < ugeOrderTable.GetLength(1); rowIndex++)
            {
                for (int chanID = 0; chanID < ugeOrderTable.GetLength(0); chanID++)
                {
                    int ugePointer = ugeOrderTable[chanID, rowIndex];
                    int furPointer = furPointerLookup[ugePointer];


                    int targChannel = ((furPointer & 0xF000) >> 12) - 1;
                    int furPatternID = (furPointer & 0x7);


                    //ugeGBChannelStates[targChannel].AddUgePattern(ugePointer);

                    //ugeFile.AddNewOrderRow(1);




                    //Console.Write(furPointer.ToString("X") + "\t");
                    Console.Write(ugePointer.ToString("X") + "\t");


                }
                Console.WriteLine();
            }
            Console.WriteLine("Done");


            var orderTableHeight = ugeOrderTable.GetLength(1);


            // Keep track of all the new instruments we define, on a per channel basis (Pulse 1 & 2 count as the same channel in this case)
            List<FurInstrument> furPulseInstruments = new List<FurInstrument>();
            List<FurInstrument> furWaveInstruments = new List<FurInstrument>();
            List<FurInstrument> furNoiseInstruments = new List<FurInstrument>();
            Dictionary<int, List<byte>> seenVolumes = new Dictionary<int, List<byte>>();
            Dictionary<(int, byte), int> clonedVolInstrumentLookup = new Dictionary<(int, byte), int>();  // Instrument Lookup: [ InstrumentID, Volume Level ] = remapped GB Instrument
            for(var i = 0; i < moduleInfo.GlobalInstruments.Count; i++)
            {
                seenVolumes[i] = new List<byte>();
            }


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
                        // Grab all the data about this row
                        var rowIndex = thisRowData.GetRowIndex();
                        int noteVal = thisRowData.GetNoteVal();
                        int volVal = thisRowData.GetVolume();
                        if (volVal >= 0)
                            ugeGBChannelStates[chanID].SetVol((byte)volVal);
                        
                        List<byte> furAllChannelFxColumns = thisRowData.GetEffectData();

                        // Also declaring some variables, for later...
                        byte furFxCmd;
                        byte furFxVal;
                        byte ugeFXVal;
                        UgeEffectTable? ugeFxCmd;
                        bool newVolInstr = false;

                        // Notes first
                        if (noteVal >= 0)
                        {
                            patCon.SetNote((GBChannel)chanID, (byte)orderID, rowIndex, FurNoteToUgeNote(noteVal));
                        }

                        // Now instruments
                        int instrVal = thisRowData.GetInstrumentVal();
                        bool duplicateInstr = false;
                        int duplicateCheckingIndex = 0;
                        byte gbEnvVol;
                        if (instrVal >= 0)
                        {
                            var chanCurrVol = ugeGBChannelStates[chanID].GetVol();
                            // Keep track of every unique instrument used (will be defined later)
                            switch (chanID)
                            {
                                default:
                                case 0:
                                case 1:
                                    foreach (FurInstrument inst in furPulseInstruments)
                                    {
                                        var instrID = inst.GetID();

                                        /// Check if we've already played this instrument previously
                                        if (instrID == moduleInfo.GlobalInstruments[instrVal].GetID())
                                        {
                                            // Check if the volume on this row is going to be the same as a previously-played instrument (also consider Uge Cxx). If it's not, we need to mark it as a duplicate, too.

                                            // Consider if we're setting a new volume here
                                            gbEnvVol = inst.GetInstrGB().GetEnvVol();
                                            if (volVal >= 0)
                                            {
                                                if (gbEnvVol != volVal)
                                                {
                                                    if (!seenVolumes[instrID].Contains((byte)volVal))
                                                    {
                                                        newVolInstr = true;
                                                    }
                                                    else
                                                    {
                                                        duplicateInstr = true;
                                                        instrVal = duplicateCheckingIndex;

                                                        // Correct the instrVal if we've already seen this volume before
                                                        if (seenVolumes[instrID].Contains((byte)volVal))
                                                        {
                                                            instrVal = clonedVolInstrumentLookup[(instrID, (byte)volVal)];
                                                            ugeGBChannelStates[chanID].SetVol((byte)volVal);
                                                            ugeGBChannelStates[chanID].SetCurrInstrID(instrVal);
                                                            chanCurrVol = (byte)volVal;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    duplicateInstr = true;
                                                    instrVal = duplicateCheckingIndex;
                                                }
                                            }
                                            else if (chanCurrVol != gbEnvVol)
                                            {
                                                if (!seenVolumes[instrID].Contains(gbEnvVol))
                                                    newVolInstr = true;
                                                else
                                                {
                                                    duplicateInstr = true;
                                                    instrVal = duplicateCheckingIndex;
                                                }
                                            }
                                            else
                                            {
                                                duplicateInstr = true;
                                                instrVal = duplicateCheckingIndex;
                                            }
                                            break;
                                        }
                                        duplicateCheckingIndex++;
                                    }
                                    
                                    if (!duplicateInstr)
                                    {
                                        FurInstrument instr = moduleInfo.GlobalInstruments[instrVal];
                                        var instrID = instr.GetID();
                                        gbEnvVol = instr.GetInstrGB().GetEnvVol();
                                        if (volVal >= 0)
                                        {
                                            if (gbEnvVol != volVal)
                                                if (!seenVolumes[instrID].Contains((byte)volVal)) 
                                                    newVolInstr = true;
                                        }

                                        // Because this envelope * volume column does not match anything else we've played before, we need to create a modified copy of the instrument that contains the new volume value.
                                        if (newVolInstr) {
                                            instr = moduleInfo.GlobalInstruments[instrVal].ShallowCopy();
                                            int newVolVal;
                                            if (volVal > 0)
                                            {
                                                instr.SetGBVol((byte)volVal);
                                                newVolVal = volVal;
                                            }
                                            else
                                            {
                                                instr.SetGBVol((byte)chanCurrVol);
                                                newVolVal = chanCurrVol;
                                            }

                                            seenVolumes[instrID].Add((byte)newVolVal);

                                            clonedVolInstrumentLookup[(instrID, (byte)newVolVal)] = furPulseInstruments.Count;
                                            volVal = -9999;
                                        }

                                        furPulseInstruments.Add(instr);
                                        instrVal = furPulseInstruments.Count - 1;
                                        ugeGBChannelStates[chanID].SetCurrInstrID(instrVal);
                                    } else
                                    {
                                        try
                                        {
                                            instrVal = clonedVolInstrumentLookup[(furPulseInstruments[ugeGBChannelStates[chanID].GetCurrInstrID()].GetID(), chanCurrVol)];
                                        }
                                        catch (KeyNotFoundException) {
                                        }
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
                        if (volVal >= 0 && !newVolInstr && ugeGBChannelStates[chanID].GetVol() != volVal)
                            patCon.SetEffect((GBChannel)chanID, (byte)orderID, rowIndex, UgeEffectTable.SET_VOL, (byte)volVal);

                        // Finally, copy all of the effect columns for this channel's row

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

                        ugeFxCmd = null;
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

                uint wtVol = (uint)UgeWaveVolumes.FULL;
                var wtIndex = 0x0;

                // Grab the parameters for the GB Instrument
                var gbParams = gbInstr.GetParams(); // returns (_gbEnvLen, _gbEnvDir, _gbEnvVol, _gbSndLen, _gbFlags, _gbHWSeqLen, _gbHWSeqCmds)
                List<FurInstrMacro> macros = waveInst.GetMacros();

                // Grab the wave from the Wave Synthesizer
                // This data will be overwritten if a Wave Macro is present (parsed further below)
                if (wsInstr != null)
                {
                    var wsParams = wsInstr.GetParams(); // returns (_wsFirstWave, _wsSecondWave, _wsRateDivider, _wsEffect, _singleEffect, _wsEnabled, _wsGlobal, _wsSpeed, _wsParam1, _wsParam2, _wsParam3, _wsParam4)
                    var wsEnabled = wsParams.Item6;
                    if (wsEnabled)
                        wtIndex = wsParams.Item1;
                }

                // Handle all the different kinds of macros. If the macro length is > 0, we will additionally add it to the subpattern data
                UgeSongPattern ugeSubPattern = new UgeSongPattern((uint)instrIndex, true);
                foreach (FurInstrMacro m in macros)
                {
                    var data = m.GetMacroData();

                    switch(m.GetMacroCode())
                    {
                        case FurInstrMacroCode.PITCH:

                            break;
                        case FurInstrMacroCode.VOL:
                            wtVol = (uint)data[0];
                            break;
                        case FurInstrMacroCode.WAVE:
                            wtIndex = data[0];
                            break;
                    }
                }

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
