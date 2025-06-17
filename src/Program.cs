using Microsoft.Extensions.Configuration;
using static fur2Uge.FurFile;
using static fur2Uge.UgeFile;

namespace fur2Uge
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder().AddCommandLine(args).Build();

            bool dumpCompressedFur = false;
            string decompressedFurPath = "";
            string fIn = string.Empty, fOut = string.Empty;
            int panMacroOnChannel = 0;

            // File in/out arg parsing
            if (!string.IsNullOrEmpty(config["i"]) && !string.IsNullOrEmpty(config["o"]))
            {
                fIn = config["i"];
                fOut = config["o"];

                // Create the directories, if they do not exist.
                string inDir = Path.GetDirectoryName(fIn);
                if (!string.IsNullOrEmpty(inDir) && !Directory.Exists(inDir))
                    Directory.CreateDirectory(inDir);

                string outDir = Path.GetDirectoryName(fOut);
                if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(outDir))
                    Directory.CreateDirectory(outDir);
            }
            else
            {
                throw new Exception("Please provide an input and output path.\n\nUsage:\nfur2uge --i <input>.fur --o <output>.uge");
            }

            if (config["d"] != null)
            {
                dumpCompressedFur = true;
                try
                {
                    decompressedFurPath = config["d"];
                }
                catch (IndexOutOfRangeException e)
                {
                    decompressedFurPath = Path.GetDirectoryName(fOut);
                }
            }

            if (config["pan"] != null)
            {
                try
                {
                    int panChanID = Int32.Parse(config["pan"]);
                    if (panChanID < 0 || panChanID > 3)
                        throw new Exception(string.Format("Invalid Channel to Pan: {0}. Aborting...", panChanID));

                    panMacroOnChannel = panChanID + 1;
                }
                catch (FormatException)
                {
                    Console.WriteLine($"Unable to parse '{config["pan"]}'");
                }
            }

            bool autoVolumeDetection = true;
            if (config["v"] != null)
            {
                try
                {
                    int volOption = Int32.Parse(config["v"]);
                    if (volOption == 0)
                        autoVolumeDetection = false;
                    else if (volOption > 1)
                        autoVolumeDetection = true;
                    else
                        throw new Exception(string.Format("Invalid argument for auto-volume detection: --v {0}.\n\nPlease specify 0 for Disabled or 1 for Enabled.\nAborting...", volOption));
                }
                catch (FormatException)
                {
                    Console.WriteLine($"Unable to parse '{config["v"]}'");
                }
            }

            int ugeVersion = 6;
            if (config["u"] != null)
            {
                try
                {
                    int vr = Int32.Parse(config["u"]);
                    if (ugeVersion >= 0 && ugeVersion <= 6)
                        ugeVersion = vr;
                    else
                        throw new Exception(string.Format("Invalid argument for uge format version: --u {0}.\n\nPlease specify a version number between 0 and 6.\nAborting...", ugeVersion));
                }
                catch (FormatException)
                {
                    Console.WriteLine($"Unable to parse '{config["a"]}'");
                }
            }

            // Read the .fur into memory
            FurFile furFile = new FurFile(fIn, dumpCompressedFur, decompressedFurPath);

            // Prepare a .uge data tree to modify
            UgeFile ugeFile = new UgeFile((uint)ugeVersion);

            // Parse the .fur file, and then populate its data into the .uge data tree we created
            ParseFur(furFile, ugeFile, autoVolumeDetection, panMacroOnChannel);

            // Write the .uge data to a binary.
            ugeFile.Write(fOut);
        }

        public static void ParseFur(FurFile furFile, UgeFile ugeFile, bool autoVolumeDetection, int panMacroOnChannel)
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
            int furBaseSpeed = furSong.Speed1;
            int furStartTick = furSong.TimeBase;
            int[] furSpeedPatternSteps = furSong.SpeedPattern;
            int furSpeedPatternLen = furSong.SpeedPatternLen;
            int tempoNum = furSong.VirtualTempoNumerator;
            int tempoDen = furSong.VirtualTempoDenominator;
            float furTicksPerSecond = furSong.TicksPerSecond;
            int furHighlightA = furSong.HighlightA;

            // Compute average speed pattern if more than 1 value, else use first value
            double furSpeedAvg;
            if (furSpeedPatternLen > 1 && furSpeedPatternSteps.Length >= furSpeedPatternLen)
            {
                furSpeedAvg = furSpeedPatternSteps.Take(furSpeedPatternLen).Average(s => (double)s);
            }
            else
            {
                furSpeedAvg = furSpeedPatternSteps.Length > 0 ? (double)furSpeedPatternSteps[0] : furBaseSpeed;
            }

            // Pass single-value array with effective speed into conversion
            double furHz = (double)furTicksPerSecond;

            var ugeSpeedData = TempoConversionHelper.ConvertFurTempoToUge(
                furStartTick,
                furBaseSpeed,
                furSpeedAvg,
                tempoNum,
                tempoDen,
                furHz,
                furHighlightA);

            if (ugeSpeedData.WasApproximate && ugeSpeedData.FurnaceSuggestion != null)
            {
                Console.WriteLine("WARNING: Tempo was approximated.");
                Console.WriteLine(ugeSpeedData.FurnaceSuggestion);
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey(true);
            }

            ugeFile.SetSongSpeed((
                ugeSpeedData.TicksPerRow,
                ugeSpeedData.TimerDivider,
                ugeSpeedData.TimerEnabled,
                ugeSpeedData.SourceBpm,
                ugeSpeedData.ResultBpm));

            /// Sort the Furnace unique-per-channel order table to accomodate for hUGETracker's global pattern layout
            // Input a uge Pattern Index, get the fur pattern index
            Dictionary<int, int> furPointerLookup = new Dictionary<int, int>();

            // Input a fur Pattern Index, get the uge pattern index
            Dictionary<int, int> ugePointerLookup = new Dictionary<int, int>();

            int[,] ugeOrderTable = new int[furSong.OrderTable.GetLength(0), furSong.OrderTable.GetLength(1)];

            // Populate the table with unique indices
            for (int chanID = 0; chanID < furSong.OrderTable.GetLength(0); chanID++)
            {
                int pointerOffset = (chanID + 1) * 0x1000;
                for (int row = 0; row < furSong.OrderTable.GetLength(1); row++)
                {
                    int furPointer = furSong.OrderTable[chanID, row] + pointerOffset;

                    // Check if the furPointer already exists in the lookup table
                    if (!furPointerLookup.ContainsKey(furPointer))
                    {
                        // If it doesn't exist, add it to the lookup tables
                        int ugePointer = furPointerLookup.Count;
                        furPointerLookup[furPointer] = ugePointer;
                        ugePointerLookup[ugePointer] = furPointer;
                    }

                    // Map the furPointer to the corresponding ugePointer
                    ugeOrderTable[chanID, row] = furPointerLookup[furPointer];
                }
            }

            // Set the order table in the UGE file
            ugeFile.SetOrderTable(ugeOrderTable);

            // Populate the Uge Pattern Tables
            for (int rowIndex = 0; rowIndex < ugeOrderTable.GetLength(1); rowIndex++)
            {
                for (int chanID = 0; chanID < ugeOrderTable.GetLength(0); chanID++)
                {
                    int ugePointer = ugeOrderTable[chanID, rowIndex];
                    ugeFile.AppendSongPattern(ugePointer);
                }
            }
            var orderTableHeight = ugeOrderTable.GetLength(1);

            // Keep track of all the new instruments we define, on a per channel basis (Pulse 1 & 2 count as the same channel in this case)
            List<FurInstrument> furPulseInstruments = new List<FurInstrument>();
            List<FurInstrument> furWaveInstruments = new List<FurInstrument>();
            List<FurInstrument> furNoiseInstruments = new List<FurInstrument>();
            Dictionary<int, List<int>> seenVolumes = new Dictionary<int, List<int>>();
            Dictionary<(int, byte), int> clonedVolInstrumentLookup = new Dictionary<(int, byte), int>();  // Instrument Lookup: [ InstrumentID, Volume Level ] = remapped GB Instrument
            for (var i = 0; i < moduleInfo.GlobalInstruments.Count; i++)
            {
                seenVolumes[i] = new List<int>();
            }

            /// Create 4 channels to simulate the song as we parse it
            UgeGBChannelState[] ugeGBChannelStates = new UgeGBChannelState[4];
            for (var i = 0; i < ugeGBChannelStates.Length; i++)
                ugeGBChannelStates[i] = new UgeGBChannelState(i);

            for (int chanID = 0; chanID < 4; chanID++)
            {
                for (int orderRow = 0; orderRow < orderTableHeight; orderRow++)
                {
                    int ugePatternID = ugeOrderTable[chanID, orderRow];
                    //int targChannel = ((furPointer & 0xF000) >> 12) - 1;

                    int furPatternID = furSong.OrderTable[chanID, orderRow];

                    FurPatternData thisPattern = furSong.Channels[chanID].GetPattern(furPatternID);
                    List<FurPatternRowData> patRowData = thisPattern.GetAllRowData();

                    /// Read all the pattern data and store it into the uge file, in order.
                    // Also declaring some variables, for later...
                    byte furFxCmd = 0x0;
                    byte furFxVal = 0x0;
                    bool furFxCmdIsPresent;
                    bool furFxValIsPresent;
                    byte ugeFXVal = 0x0;
                    UgeEffectTable? ugeFxCmd = 0x0;
                    int previousRowIndex = 0;
                    int rowsSincePreviousData = 0;

                    foreach (FurPatternRowData thisRowData in patRowData)
                    {
                        // First check: Is this the last row that contains data in the entire pattern?
                        if (patRowData[patRowData.Count - 1] == thisRowData)
                        {
                            // This is the last row in the pattern that contains data; calculate how far we are from the end of the pattern
                            int patLen = thisPattern.GetLength();
                            int rowsToEndOfPattern = patLen - thisRowData.GetRowIndex();

                            // Now, iterate through all the rows we're about to skip that don't contain any data, and populate them with any active effects from this final data row
                            for (int i = 1; i < rowsToEndOfPattern; i++)
                            {
                                // Stop iteratively writing effects, if there are no useful effects to write to the .uge
                                if (ugeFXVal <= 0 || ugeFxCmd == null)
                                    break;

                                // Do not attempt to write certain effects iteratively
                                if (ugeFxCmd == UgeEffectTable.NOTE_DELAY || ugeFxCmd == UgeEffectTable.NOTE_CUT || ugeFxCmd == UgeEffectTable.EMPTY || ugeFxCmd == UgeEffectTable.SET_SPEED || ugeFxCmd == UgeEffectTable.SET_PANNING)
                                    break;

                                patCon.SetEffect((GBChannel)chanID, (byte)ugePatternID, thisRowData.GetRowIndex() + i, (UgeEffectTable)ugeFxCmd, ugeFXVal);
                            }
                        }

                        // Count how many rows we skipped since the previous row containing data
                        if (previousRowIndex > 0)
                        {
                            rowsSincePreviousData = thisRowData.GetRowIndex() - previousRowIndex;
                        }
                        previousRowIndex = thisRowData.GetRowIndex();

                        // Iterate through all the previously-skipped rows, and populate them with any active effects from the previous row
                        for (int i = rowsSincePreviousData - 1; i > 0; i--)
                        {
                            // Stop iteratively writing effects, if there are no useful effects to write to the .uge
                            if (ugeFXVal <= 0 || ugeFxCmd == null)
                                break;

                            // Do not attempt to write certain effects iteratively
                            if (ugeFxCmd == UgeEffectTable.NOTE_DELAY || ugeFxCmd == UgeEffectTable.NOTE_CUT || ugeFxCmd == UgeEffectTable.EMPTY || ugeFxCmd == UgeEffectTable.SET_SPEED || ugeFxCmd == UgeEffectTable.SET_PANNING)
                                break;
                            
                            patCon.SetEffect((GBChannel)chanID, (byte)ugePatternID, thisRowData.GetRowIndex() - i, (UgeEffectTable)ugeFxCmd, ugeFXVal);
                        }



                        // Grab all the data about this row
                        var rowIndex = thisRowData.GetRowIndex();
                        int noteVal = thisRowData.GetNoteVal();
                        int volVal = thisRowData.GetVolume();
                        if (volVal >= 0)
                            ugeGBChannelStates[chanID].SetVol((byte)volVal);

                        FurPatternRowDataEffectCell[] furAllChannelFxColumns = thisRowData.GetEffectData();


                        bool newVolInstr = false;

                        // Notes first
                        if (noteVal >= 0)
                        {
                            patCon.SetNote((GBChannel)chanID, (byte)ugePatternID, rowIndex, FurNoteToUgeNote(noteVal));
                        }

                        // Now instruments
                        int instrVal = thisRowData.GetInstrumentVal();
                        bool duplicateInstr = false;
                        int duplicateCheckingIndex = 0;
                        byte gbEnvVol;
                        if (instrVal >= 0)
                        {
                            if (autoVolumeDetection)
                            {
                                // Keep track of every unique instrument used (will be defined later)
                                var chanCurrVol = ugeGBChannelStates[chanID].GetVol();

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
                            }
                            else
                            {

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
                            }

                            patCon.SetInstrument((GBChannel)chanID, (byte)ugePatternID, rowIndex, instrVal);
                        }

                        // Now copy the volume column (which might or might not get overwritten if an effect is present)
                        if (volVal >= 0 && (!newVolInstr || !autoVolumeDetection)) {

                            byte outVol = (byte)volVal;

                            if (instrVal >= 0)
                            {
                                (int, int, int) env = moduleInfo.GlobalInstruments[instrVal].GetInstrGB().GetEnvParams(); // returns (_gbEnvVol, _gbEnvLen, _gbEnvDir)
                                var envVol = env.Item1;
                                var envLen = env.Item2;
                                var envDir = env.Item3 <= 0 ? 1 : -1;

                                if (chanID == 2)
                                {
                                    envLen = 0;
                                    envDir = 0;
                                }
                                int hiNybble = ((envLen * envDir) << 4) & 0xFF;

                                outVol += (byte)(hiNybble);
                            }

                            patCon.SetEffect((GBChannel)chanID, (byte)ugePatternID, rowIndex, UgeEffectTable.SET_VOL, outVol);
                        }

                        // Finally, copy all of the effect columns for this channel's row

                        // Before writing any new effects, update the state of the channel,
                        // just in case we turned off any effects on this row, for any FX column...
                        for (var i = 0; i < furAllChannelFxColumns.Length; i++)
                        {
                            furFxCmdIsPresent = furAllChannelFxColumns[i].GetCommandIsPresent();
                            if (furFxCmdIsPresent)
                                furFxCmd = furAllChannelFxColumns[i].GetCommand();
                            furFxValIsPresent = furAllChannelFxColumns[i].GetValueIsPresent(); 
                            
                            if (furFxValIsPresent)
                                furFxVal = furAllChannelFxColumns[i].GetValue();

                            if (furFxVal == 0x00 && furFxValIsPresent)
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

                        // Now that the channel state is up-to-date, we should parse the commands in the FX Slots, in order from left to right

                        for (var j = 0; j < furAllChannelFxColumns.Length; j++)
                        {
                            furFxCmd = furAllChannelFxColumns[j].GetCommand();
                            furFxCmdIsPresent = furAllChannelFxColumns[j].GetCommandIsPresent();
                            furFxVal = furAllChannelFxColumns[j].GetValue();
                            furFxValIsPresent = furAllChannelFxColumns[j].GetValueIsPresent();

                            if (furFxCmdIsPresent)
                                ugeFxCmd = (UgeEffectTable)furFxCmd;

                            // If there should be nothing on this row, and the last effect was a Note Cut or Note Delay, don't iteratively write Note Cut/Note Delay on other rows
                            if (furFxCmd == 0xF1 && (ugeFxCmd == UgeEffectTable.NOTE_CUT || ugeFxCmd == UgeEffectTable.NOTE_DELAY))
                                ugeFxCmd = UgeEffectTable.EMPTY;

                            if (furFxCmdIsPresent)
                                ugeFXVal = furFxVal;

                            bool rightSpeakerOn, leftSpeakerOn;
                            switch ((UgeEffectTable)furFxCmd)
                            {
                                default:
                                    break;
                                case (UgeEffectTable)0xEC: // Note cut
                                    ugeFxCmd = UgeEffectTable.NOTE_CUT;
                                    break;
                                case (UgeEffectTable)0xED: // Note delay
                                    ugeFxCmd = UgeEffectTable.NOTE_DELAY;
                                    break;
                                case (UgeEffectTable)0x80: // Channel Pan
                                    // Update this channel's pan values
                                    rightSpeakerOn = (furFxVal == 0x80 || furFxVal == 0xFF);
                                    leftSpeakerOn = (furFxVal == 0x00 || furFxVal == 0x80);
                                    ugeGBChannelStates[chanID].SetPan(leftSpeakerOn, rightSpeakerOn);
                                    ugeFxCmd = UgeEffectTable.SET_PANNING;

                                    // Update the pan value based on all of the GB Channels' current pan states
                                    ugeFXVal = 0x0;
                                    foreach (UgeGBChannelState chanState in ugeGBChannelStates)
                                    {
                                        ugeFXVal |= chanState.GetPan();
                                    }
                                    break;
                                case UgeEffectTable.SET_PANNING:
                                    // Update this channel's pan values
                                    rightSpeakerOn = ((byte)(furFxVal & 0x0F) > 0) ? true : false;
                                    leftSpeakerOn = ((byte)((furFxVal & 0xF0) >> 4) > 0) ? true : false;
                                    ugeGBChannelStates[chanID].SetPan(leftSpeakerOn, rightSpeakerOn);

                                    // Update the pan value based on all of the GB Channels' current pan states
                                    ugeFXVal = 0x0;
                                    foreach (UgeGBChannelState chanState in ugeGBChannelStates)
                                    {
                                        ugeFXVal |= chanState.GetPan();
                                    }
                                    break;
                                case UgeEffectTable.POSITION_JUMP:
                                    ugeFXVal++;
                                    break;
                                case UgeEffectTable.PATTERN_BREAK:
                                    ugeFXVal++;
                                    break;
                            }

                            //if (ugeFXVal > 0 && ugeFxCmd != null && ugeFxCmd < UgeEffectTable.MAX)
                            if (ugeFXVal > 0 && ugeFxCmd != UgeEffectTable.EMPTY)
                                patCon.SetEffect((GBChannel)chanID, (byte)ugePatternID, rowIndex, (UgeEffectTable)ugeFxCmd, ugeFXVal);

                            switch (ugeFxCmd)
                            {
                                case UgeEffectTable.SET_SPEED:
                                case UgeEffectTable.SET_PANNING:
                                    ugeFxCmd = UgeEffectTable.EMPTY;
                                    break;
                            }
                        }

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
                UgeInstrument ugePulse = new UgeInstrument(name, (uint)UgeInstrumentType.PULSE, gbParams.Item1, (gbParams.Item2 >= 0x1) ? 1U : 0U,
                    gbParams.Item3, gbParams.Item4, gbParams.Item5, gbParams.Item6, gbParams.Item7, macros, null, 0x1, 0x1, panMacroOnChannel
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

                    switch (m.GetMacroCode())
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
