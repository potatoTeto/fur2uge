using System.IO.Compression;
using System.Text;

namespace fur2Uge
{
    public partial class FurFile
    {
        private const int PATTERN_LEN_LIMIT = 256;
        private const int INSTRUMENT_COUNT_LIMIT = 256;
        private const int WAVETABLE_COUNT_LIMIT = 256;
        private const int SAMPLE_COUNT_LIMIT = 256;

        StringBuilder sb = new StringBuilder();

        private FurHeader _furHeader;
        private FurModuleInfo _furModuleInfo;
        private List<FurSong> _furSongs;
        private string[] _noteNames = new string[]{
          "c_5", "c+5", "d_5", "d+5", "e_5", "f_5", "f+5", "g_5", "g+5", "a_5", "a+5", "b_5",
          "c_4", "c+4", "d_4", "d+4", "e_4", "f_4", "f+4", "g_4", "g+4", "a_4", "a+4", "b_4",
          "c_3", "c+3", "d_3", "d+3", "e_3", "f_3", "f+3", "g_3", "g+3", "a_3", "a+3", "b_3",
          "c_2", "c+2", "d_2", "d+2", "e_2", "f_2", "f+2", "g_2", "g+2", "a_2", "a+2", "b_2",
          "c_1", "c+1", "d_1", "d+1", "e_1", "f_1", "f+1", "g_1", "g+1", "a_1", "a+1", "b_1",
          "C-0", "C#0", "D-0", "D#0", "E-0", "F-0", "F#0", "G-0", "G#0", "A-0", "A#0", "B-0",
          "C-1", "C#1", "D-1", "D#1", "E-1", "F-1", "F#1", "G-1", "G#1", "A-1", "A#1", "B-1",
          "C-2", "C#2", "D-2", "D#2", "E-2", "F-2", "F#2", "G-2", "G#2", "A-2", "A#2", "B-2",
          "C-3", "C#3", "D-3", "D#3", "E-3", "F-3", "F#3", "G-3", "G#3", "A-3", "A#3", "B-3",
          "C-4", "C#4", "D-4", "D#4", "E-4", "F-4", "F#4", "G-4", "G#4", "A-4", "A#4", "B-4",
          "C-5", "C#5", "D-5", "D#5", "E-5", "F-5", "F#5", "G-5", "G#5", "A-5", "A#5", "B-5",
          "C-6", "C#6", "D-6", "D#6", "E-6", "F-6", "F#6", "G-6", "G#6", "A-6", "A#6", "B-6",
          "C-7", "C#7", "D-7", "D#7", "E-7", "F-7", "F#7", "G-7", "G#7", "A-7", "A#7", "B-7",
          "C-8", "C#8", "D-8", "D#8", "E-8", "F-8", "F#8", "G-8", "G#8", "A-8", "A#8", "B-8",
          "C-9", "C#9", "D-9", "D#9", "E-9", "F-9", "F#9", "G-9", "G#9", "A-9", "A#9", "B-9",
          "Note Off", "Note Release", "Macro Release"
        };

        public Dictionary<string, FurInstrFeature> InstrFeatureDict;
        public FurFile(string fIn, bool dumpCompressedFur = false, string decompressedFurPath = "")
        {
            InitChanLookupDict();
            InitInstrFeatureLookupDict();
            // Read the input .fur file
            byte[] furBytes = File.ReadAllBytes(fIn);

            // Zlib-decompress the .fur file, if applicable. (Check for Zlib "78 9C - Default Compression" Magic)
            if (furBytes[0] == 0x78 && furBytes[1] == 0x9C)
            {
                furBytes = DecompressZLib(furBytes);

                if (dumpCompressedFur)
                {
                    using var writer = new BinaryWriter(File.OpenWrite(Path.Combine(Path.GetDirectoryName(decompressedFurPath), Path.GetFileNameWithoutExtension(fIn) + "_decompressed.fur")));
                    writer.Write(furBytes);
                }
            }

            // Initialize the Memory Stream, String Builder, and the first Song within the module
            MemoryStream ms = new MemoryStream(furBytes);
            StringBuilder sb = new StringBuilder();
            _furSongs = new List<FurSong>();
            FurSong firstSong = new FurSong();
            _furSongs.Add(firstSong);


            // Begin reading the .fur file
            using (BinaryReader reader = new BinaryReader(ms))
            {
                reader.BaseStream.Seek(0, SeekOrigin.Begin);

                long streamSize = reader.BaseStream.Length;

                // Ensure that the magic is correct
                string magic = System.Text.Encoding.Default.GetString(reader.ReadBytes(16));
                if (!magic.Equals("-Furnace module-"))
                    throw new Exception("Invalid file, or file is corrupt.");

                // Grab the rest of the .fur's header
                int version = reader.ReadUInt16();
                int reserved = reader.ReadUInt16();
                int songInfoPointer = reader.ReadInt32();
                byte[] reserved2 = reader.ReadBytes(8);
                _furHeader = new FurHeader(magic, version, reserved, songInfoPointer, reserved2);

                // Populate all of the song info
                // More detail on the format here: https://github.com/tildearrow/furnace/blob/master/papers/format.md#song-info
                _furModuleInfo.InfoBlockID = System.Text.Encoding.Default.GetString(reader.ReadBytes(4));
                _furModuleInfo.BlockSize = reader.ReadInt32();
                var firstTimeBase = reader.ReadByte();
                var firstSpeed1 = reader.ReadByte();
                var firstSpeed2 = reader.ReadByte();
                var firstInitialArpTime = reader.ReadByte();
                var firstTicksPerSecond = BitConverter.ToSingle(reader.ReadBytes(4), 0);
                var firstPatternLen = reader.ReadUInt16();
                if (firstPatternLen > PATTERN_LEN_LIMIT)
                    throw new Exception(string.Format("Invalid pattern length: {0}", firstPatternLen));
                var firstOrdersLen = reader.ReadUInt16();
                if (_furHeader.GetVersion() >= 80)
                {
                    if (firstOrdersLen > 256)
                        throw new Exception(string.Format("Invalid number of Orders: {0} (Furnace version: {1})", firstPatternLen, _furHeader.GetVersion()));
                }
                else
                {
                    if (firstOrdersLen > 127)
                        throw new Exception(string.Format("Invalid number of Orders: {0} (Furnace version: {1})", firstPatternLen, _furHeader.GetVersion()));
                }

                var firstHighlightA = reader.ReadByte();
                var firstHighlightB = reader.ReadByte();

                firstSong.InitDataA(firstTimeBase, firstSpeed1, firstSpeed2, firstInitialArpTime, firstTicksPerSecond, firstPatternLen, firstOrdersLen, firstHighlightA, firstHighlightB);

                _furModuleInfo.InstrumentCount = reader.ReadUInt16();
                if (_furModuleInfo.InstrumentCount > INSTRUMENT_COUNT_LIMIT)
                    throw new Exception(string.Format("Too many instruments in the module! Count: {0}", _furModuleInfo.InstrumentCount));
                _furModuleInfo.WavetableCount = reader.ReadUInt16();
                if (_furModuleInfo.WavetableCount > WAVETABLE_COUNT_LIMIT)
                    throw new Exception(string.Format("Too many instruments in the module! Count: {0}", _furModuleInfo.WavetableCount));
                _furModuleInfo.SampleCount = reader.ReadUInt16();
                if (_furModuleInfo.SampleCount > SAMPLE_COUNT_LIMIT)
                    throw new Exception(string.Format("Too many instruments in the module! Count: {0}", _furModuleInfo.SampleCount));

                _furModuleInfo.PatternCountGlobal = reader.ReadInt32();

                // Populate the list of Chips used in this .fur module; Abort as soon as we hit the "END_OF_LIST"
                byte[] chipTypeListByte = reader.ReadBytes(32);
                _furModuleInfo.SoundChipTypeList = new List<FurChipType>();
                foreach (byte b in chipTypeListByte)
                {
                    var thisChip = (FurChipType)b;
                    if (thisChip == FurChipType.END_OF_LIST)
                        break;
                    _furModuleInfo.SoundChipTypeList.Add(thisChip);
                }

                /// Populate the global volume for each individual chip in the module
                // Legacy info, as of Version 135
                // signed char, 64=1.0, 127=~2.0
                byte[] chipVolListByte = reader.ReadBytes(32);
                _furModuleInfo.SoundChipVolList = new sbyte[32];
                for (var i = 0; i < chipVolListByte.Length; i++)
                {
                    sbyte s = unchecked((sbyte)chipVolListByte[i]);
                    _furModuleInfo.SoundChipVolList[i] = s;
                }

                /// Populate the global stereo pan for each individual chip in the module
                /// Legacy info, as of Version 135
                // - signed char, -128=left, 127=right
                byte[] chipPanListByte = reader.ReadBytes(32);
                _furModuleInfo.SoundChipPanList = new sbyte[32];
                for (var i = 0; i < chipPanListByte.Length; i++)
                {
                    sbyte s = unchecked((sbyte)chipPanListByte[i]);
                    _furModuleInfo.SoundChipPanList[i] = s;
                }

                /// Populate the flag pointer list for each individual chip in the module
                if (_furHeader.GetVersion() >= 118)
                {
                    // Before 118, these were 32-bit flags.
                    // For conversion details, see the "converting from old flags" section.
                    _furModuleInfo.SoundChipFlagPointers = reader.ReadBytes(128);
                }
                else
                {
                    _furModuleInfo.SoundChipLegacyFlags = new bool[128];
                    for (var i = 0; i < _furModuleInfo.SoundChipLegacyFlags.Length; i++)
                    {
                        _furModuleInfo.SoundChipLegacyFlags[i] = reader.ReadByte() > 0x0;
                    }
                }

                /// Populate more metadata about the .fur
                _furModuleInfo.GlobalModuleName = GetNextString(sb, reader);
                _furModuleInfo.GlobalAuthor = GetNextString(sb, reader);

                _furModuleInfo.A4Tuning = BitConverter.ToSingle(reader.ReadBytes(4), 0);

                _furModuleInfo.LimitSlides = reader.ReadByte();
                _furModuleInfo.LinearPitch = (FurPitchLinearity)reader.ReadByte();
                _furModuleInfo.LoopModality = reader.ReadByte();
                _furModuleInfo.ProperNoiseLayout = reader.ReadByte();
                _furModuleInfo.WaveDutyIsVolume = reader.ReadByte();
                _furModuleInfo.ResetMacroOnPorta = reader.ReadByte();
                _furModuleInfo.LegacyVolumeSlides = reader.ReadByte();
                _furModuleInfo.CompatibleArpeggio = reader.ReadByte();
                _furModuleInfo.NoteOffResetsSlides = reader.ReadByte();
                _furModuleInfo.TargetResetsSlides = reader.ReadByte();
                _furModuleInfo.ArpsInhibitPortamento = reader.ReadByte();
                _furModuleInfo.WackAlgoMacro = reader.ReadByte();
                _furModuleInfo.BrokenShortcutSlides = reader.ReadByte();
                _furModuleInfo.IgnoreDuplicateSlides = reader.ReadByte();
                _furModuleInfo.StopPortamentoOnNoteOff = reader.ReadByte();
                _furModuleInfo.ContinuousVibrato = reader.ReadByte();
                _furModuleInfo.BrokenDACMode = reader.ReadByte();
                _furModuleInfo.OneTickCut = reader.ReadByte();
                _furModuleInfo.InstrumentChangeAllowedDuringPorta = reader.ReadByte();
                _furModuleInfo.ResetNoteBaseOnArpEffectStop = reader.ReadByte();


                /// Prepare a list of pointers for all our Instruments/Wavetables/etc
                _furModuleInfo.InstrumentPointers = new List<int>();
                _furModuleInfo.WavetablePointers = new List<int>();
                _furModuleInfo.SamplePointers = new List<int>();
                _furModuleInfo.PatternPointers = new List<int>();

                // Pointers for all the different Instruments
                for (var i = 0; i < _furModuleInfo.InstrumentCount; i++)
                {
                    int thisPointer = reader.ReadInt32();
                    _furModuleInfo.InstrumentPointers.Add(thisPointer);
                }

                // Pointers for all the different Wavetables
                for (var i = 0; i < _furModuleInfo.WavetableCount; i++)
                {
                    int thisPointer = reader.ReadInt32();
                    _furModuleInfo.WavetablePointers.Add(thisPointer);
                }

                // Pointers for all the different Samples
                for (var i = 0; i < _furModuleInfo.SampleCount; i++)
                {
                    int thisPointer = reader.ReadInt32();
                    _furModuleInfo.SamplePointers.Add(thisPointer);
                }

                // Pointers for all the different Patterns
                for (var i = 0; i < _furModuleInfo.PatternCountGlobal; i++)
                {
                    int thisPointer = reader.ReadInt32();
                    _furModuleInfo.PatternPointers.Add(thisPointer);
                }

                /// Orders (Of first song)
                // First, we must determine how many channels we are dealing with, and for which chips.
                _furModuleInfo.SoundChipChanCountList = new List<FurChipCounter>();
                foreach (FurChipType c in _furModuleInfo.SoundChipTypeList)
                {
                    _furModuleInfo.SoundChipChanCountList.Add(new FurChipCounter(c, _furModuleInfo.ChanCountLookup[c]));
                    _furModuleInfo.TotalChanCount += _furModuleInfo.ChanCountLookup[c];
                }

                // Now, populate the order table for the first song (Top to bottom first, then left to right)
                int[,] firstOrderTable = new int[_furModuleInfo.TotalChanCount, firstOrdersLen];
                for (var x = 0; x < _furModuleInfo.TotalChanCount; x++)
                {
                    for (var y = 0; y < firstOrdersLen; y++)
                    {
                        var orderVal = reader.ReadByte();
                        if (_furHeader.GetVersion() < 80)
                        {
                            if (orderVal > 0x7F)
                                throw new Exception(string.Format("Order value is too high! (Val: {0})", orderVal));
                        }
                        else if (orderVal > 0xFF)
                            throw new Exception(string.Format("Order value is too high! (Val: {0})", orderVal));
                        firstOrderTable[x, y] = orderVal;
                    }
                }
                firstSong.PopulateOrderTable(firstOrderTable);

                // Number of effect columns (of the first song)
                for (var x = 0; x < _furModuleInfo.TotalChanCount; x++)
                {
                    var effectColumnCount = reader.ReadByte();
                    firstSong.AddEffectColumnCount(x, effectColumnCount);
                }

                // Channel hide status (of the first song)
                for (var x = 0; x < _furModuleInfo.TotalChanCount; x++)
                {
                    var channelHideStatus = reader.ReadByte();
                    firstSong.AddChanHideStatus(x, channelHideStatus);
                }

                // Channel collapse status (of the first song)
                for (var x = 0; x < _furModuleInfo.TotalChanCount; x++)
                {
                    var channelCollapseStatus = reader.ReadByte();
                    firstSong.AddChanCollapseStatus(x, channelCollapseStatus);
                }

                // Channel Names (of the first song)
                for (var x = 0; x < _furModuleInfo.TotalChanCount; x++)
                {
                    var chanName = GetNextString(sb, reader);
                    firstSong.AddChanName(x, chanName);
                }

                // Channel Short Names (of the first song)
                for (var x = 0; x < _furModuleInfo.TotalChanCount; x++)
                {
                    var chanName = GetNextString(sb, reader);
                    firstSong.AddChanShortName(x, chanName);
                }

                _furModuleInfo.SongComment = GetNextString(sb, reader);

                // 1.0f=100% (>=59)
                // this is 2.0f for modules before 59
                _furModuleInfo.MasterVolume = BitConverter.ToSingle(reader.ReadBytes(4), 0);

                /// **extended compatibility flags** (>=70)
                _furModuleInfo.BrokenSpeedSelection = reader.ReadByte();
                _furModuleInfo.NoSlidesOnFirstTick = reader.ReadByte();
                _furModuleInfo.NextRowResetArpPos = reader.ReadByte();
                _furModuleInfo.IgnoreJumpAtEnd = reader.ReadByte();
                _furModuleInfo.BuggyPortamentoAfterSlide = reader.ReadByte();
                _furModuleInfo.NewInsAffectsEnvelope = reader.ReadByte(); // For Gameboy
                _furModuleInfo.ExtChChannelStateIsShared = reader.ReadByte();
                _furModuleInfo.IgnoreDACModeChangeOutsideOfIntendedChannel = reader.ReadByte();
                _furModuleInfo.E1XYAndE2XYAlsoTakePriorityOverSlide00 = reader.ReadByte();
                _furModuleInfo.NewSegaPCM = reader.ReadByte(); // (With macros and proper vol/pan)
                _furModuleInfo.WeirdFNum = reader.ReadByte(); // block-based chip pitch slides
                _furModuleInfo.SNDutyMacroAlwaysResetsPhase = reader.ReadByte();
                _furModuleInfo.PitchMacroIsLinear = reader.ReadByte();
                _furModuleInfo.PitchSlideSpeedInFullLinearPitchMode = reader.ReadByte();
                _furModuleInfo.OldOctaveBoundaryBehavior = reader.ReadByte();
                _furModuleInfo.DisableOPN2DACVolControl = reader.ReadByte();
                _furModuleInfo.NewVolScalingStrat = reader.ReadByte();
                _furModuleInfo.VolMacroStillAppliesAfterEnd = reader.ReadByte();
                _furModuleInfo.BrokenOutVol = reader.ReadByte();
                _furModuleInfo.E1XYAndE2XYStopOnSameNote = reader.ReadByte();
                _furModuleInfo.BrokenInitialPosOfPortaAfterArp = reader.ReadByte();
                _furModuleInfo.SNPeriodsUnder8AreTreatedAs1 = reader.ReadByte();
                _furModuleInfo.CutDelayEffectPolicy = reader.ReadByte();
                _furModuleInfo.BAndDEffectTreatment = reader.ReadByte();
                _furModuleInfo.AutoSysNameDetection = reader.ReadByte(); // This one isn't a compatibility flag, but it's here for convenience
                _furModuleInfo.DisableSampleMacro = reader.ReadByte();
                _furModuleInfo.BrokenOutVolEpisode2 = reader.ReadByte();
                _furModuleInfo.OldArpStrat = reader.ReadByte();

                /// Virtual Tempo Data
                var virtualTempoNumeratorOfFirstSong = reader.ReadUInt16();
                var virtualTempoDenominatorOfFirstSong = reader.ReadUInt16();
                firstSong.SetVirtualTempo(virtualTempoNumeratorOfFirstSong, virtualTempoDenominatorOfFirstSong);

                /// Additional Subsongs
                _furModuleInfo.FirstSubsongName = GetNextString(sb, reader);
                _furModuleInfo.FirstSubsongComment = GetNextString(sb, reader);
                _furModuleInfo.NumOfAdditionalSubsongs = reader.ReadByte();
                _furModuleInfo.ReservedAdditionalSubsongs = reader.ReadByte() << 16 | reader.ReadByte() << 8 | reader.ReadByte();

                // Subsong Data Pointers
                _furModuleInfo.SubsongDataPointers = new List<int>();
                for (var i = 0; i < _furModuleInfo.NumOfAdditionalSubsongs; i++)
                {
                    int thisPointer = reader.ReadInt32();
                    _furModuleInfo.SubsongDataPointers.Add(thisPointer);
                }

                /// Additional Metadata
                _furModuleInfo.SysName = GetNextString(sb, reader);
                _furModuleInfo.AlbumCategoryGameName = GetNextString(sb, reader);
                _furModuleInfo.SongNameJP = GetNextString(sb, reader);
                _furModuleInfo.GlobalAuthorJP = GetNextString(sb, reader);
                _furModuleInfo.SysNameJP = GetNextString(sb, reader);
                _furModuleInfo.AlbumCategoryGameNameJP = GetNextString(sb, reader);

                /// **Extra Chip Output Settings (× chipCount)** (>=135)
                for (var i = 0; i < _furModuleInfo.SoundChipTypeList.Count; i++)
                {
                    var extChipVol = BitConverter.ToSingle(reader.ReadBytes(4), 0);
                    var extChipPan = BitConverter.ToSingle(reader.ReadBytes(4), 0);
                    var extChipFrontRearBalance = BitConverter.ToSingle(reader.ReadBytes(4), 0);
                }

                // Patchbay (>= 135)
                _furModuleInfo.PatchBayConnectionCount = reader.ReadInt32();

                _furModuleInfo.PatchBayList = new List<FurPatchBay>();
                for (var i = 0; i < _furModuleInfo.PatchBayConnectionCount; i++)
                {
                    FurPatchBay thisPB = new FurPatchBay(new byte[] { reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte() });
                    _furModuleInfo.PatchBayList.Add(thisPB);
                }

                _furModuleInfo.AutoPatchBay = reader.ReadByte();

                /// A couple more compat flags (>=138)
                _furModuleInfo.BrokenPortamentoDuringLegato = reader.ReadByte();
                _furModuleInfo.BrokenMacroDuringNoteOffInSomeFMChips = reader.ReadByte();
                _furModuleInfo.C64PreNoteDoesNotCompensateForPortamentoOrLegato = reader.ReadByte();
                _furModuleInfo.DisableNewNESDPCMFeatures = reader.ReadByte();
                _furModuleInfo.ResetArpEffectPhaseOnNewNote = reader.ReadByte();
                _furModuleInfo.LinearVolumeScalingRoundsUp = reader.ReadByte();
                _furModuleInfo.LegacyAlwaysSetVolBehavior = reader.ReadByte();
                _furModuleInfo.ReservedMoreCompatFlags = reader.ReadByte();

                /// Speed Pattern of First Song (>=139)
                var speedPatternLen = reader.ReadByte();
                // (Fail if this is lower than 0 or higher than 16)
                if (speedPatternLen < 0 || speedPatternLen > 16)
                    throw new Exception(string.Format("Speed Pattern Length out of Range (0-16): {0}", speedPatternLen));

                var speedPattern = reader.ReadBytes(16);
                firstSong.SetSpeedPattern(speedPatternLen, speedPattern);

                /// Groove List
                _furModuleInfo.GrooveCount = reader.ReadByte();
                _furModuleInfo.GrooveEntriesList = new List<FurGrooveEntry>();
                for (var i = 0; i < _furModuleInfo.GrooveCount; i++)
                {
                    byte grooveLen = reader.ReadByte();
                    byte[] groovePattern = reader.ReadBytes(16);
                    FurGrooveEntry thisGroove = new FurGrooveEntry(grooveLen, groovePattern);
                    _furModuleInfo.GrooveEntriesList.Add(thisGroove);
                }

                /// Pointers to Asset Directories (>=156)
                _furModuleInfo.InstrumentDirectoriesPointer = reader.ReadInt32();
                _furModuleInfo.WavetableDirectoriesPointer = reader.ReadInt32();
                _furModuleInfo.SampleDirectoriesPointer = reader.ReadInt32();

                /// Define all of the Instruments based on their pointers and the module version string
                _furModuleInfo.GlobalInstruments = new List<FurInstrument>();
                int bit0, bit1, bit2, bit3, bit4, bit5, bit6, bit7;

                for (var i = 0; i < _furModuleInfo.InstrumentCount; i++)
                {
                    if (_furHeader.GetVersion() >= 127)
                    {
                        reader.BaseStream.Seek(_furModuleInfo.InstrumentPointers[i], SeekOrigin.Begin);
                        string finsFormatMagic = System.Text.Encoding.Default.GetString(reader.ReadBytes(4));
                        int myBlockSize = reader.ReadInt32();
                        int instFormatVersion = reader.ReadUInt16();
                        FurInstrumentType instrType = (FurInstrumentType)reader.ReadUInt16();

                        var thisInstr = new FurInstrument(finsFormatMagic, myBlockSize, instFormatVersion, instrType);

                        bool stopLoop = false;
                        while (!stopLoop)
                        {
                            string featureCode = System.Text.Encoding.Default.GetString(reader.ReadBytes(2));
                            if (InstrFeatureDict[featureCode] == FurInstrFeature.END_OF_FEATURES)
                                break;
                            int blockLen = reader.ReadUInt16();

                            switch (InstrFeatureDict[featureCode])
                            {
                                case FurInstrFeature.INSTRUMENT_NAME:
                                    var name = GetNextString(sb, reader);
                                    thisInstr.SetName(name);
                                    blockLen -= name.Length + 1;
                                    break;
                                case FurInstrFeature.FM_INS_DATA:
                                    /// TODO: Make better use of this data
                                    var flags = reader.ReadByte();
                                    byte opCount = (byte)(flags & 0x0F);   // Op Count

                                    // Base Data
                                    var baseData1 = reader.ReadByte();
                                    var baseData2 = reader.ReadByte();
                                    var baseData3 = reader.ReadByte();
                                    blockLen -= 4;

                                    // Data for each active Operator
                                    List<FurOpData> furOPData = new List<FurOpData>();
                                    for (var j = 0; j < opCount; j++)
                                    {
                                        var opData = reader.ReadBytes(8);
                                        blockLen -= 8; // Subtract the number of bytes we just read from the feature block length

                                        FurOpData thisOpData = new FurOpData(opData);
                                        furOPData.Add(thisOpData);
                                    }

                                    FurInstrFM thisFMInst = new FurInstrFM(flags, opCount, baseData1, baseData2, baseData3, furOPData);
                                    thisInstr.AddFMInstData(thisFMInst);
                                    break;
                                case FurInstrFeature.MACRO_DATA:
                                    var macroHeaderLen = reader.ReadUInt16();
                                    blockLen -= 2;

                                    while (blockLen > 0)
                                    {
                                        if (blockLen == 1)
                                        {
                                            // Eat the last 0xFF byte, because this is the end of all of the macro definitions
                                            reader.ReadByte();
                                            blockLen--;
                                            break;
                                        }
                                        FurInstrMacroCode macroCode = (FurInstrMacroCode)reader.ReadByte();
                                        var macroLen = reader.ReadByte();
                                        var macroLoop = reader.ReadByte();
                                        var macroRelease = reader.ReadByte();
                                        var macroMode = reader.ReadByte();

                                        var macroOpenTypeWordSizeByte = reader.ReadByte();
                                        bit6 = GetBit(macroOpenTypeWordSizeByte, 6) ? 1 : 0;
                                        bit7 = GetBit(macroOpenTypeWordSizeByte, 7) ? 1 : 0;
                                        FurInstrMacroWordSize macroOpenTypeWordSize = (FurInstrMacroWordSize)((bit7 << 1) | bit6);

                                        var instantRelease = GetBit(macroOpenTypeWordSizeByte, 3) ? 1 : 0;


                                        bit1 = GetBit(macroOpenTypeWordSizeByte, 1) ? 1 : 0;
                                        bit2 = GetBit(macroOpenTypeWordSizeByte, 2) ? 1 : 0;
                                        FurInstrMacroType macroType = (FurInstrMacroType)((bit2 << 1) | bit1);
                                        var macroWindowIsOpen = GetBit(macroOpenTypeWordSizeByte, 0) ? 1 : 0;


                                        var macroDelay = reader.ReadByte();
                                        var macroSpeed = reader.ReadByte();

                                        blockLen -= 8; // Subtract the number of bytes we just read from the feature block length

                                        List<int> macroData = new List<int>();
                                        int macroWordLenInBytes = 0x0;
                                        switch (macroOpenTypeWordSize)
                                        {
                                            default:
                                            case FurInstrMacroWordSize.UNSIGNED_8BIT:
                                                macroWordLenInBytes = 0x1;
                                                break;
                                            case FurInstrMacroWordSize.SIGNED_8BIT:
                                                macroWordLenInBytes = 0x1;
                                                break;
                                            case FurInstrMacroWordSize.SIGNED_16BIT:
                                                macroWordLenInBytes = 0x2;
                                                break;
                                            case FurInstrMacroWordSize.SIGNED_32BIT:
                                                macroWordLenInBytes = 0x4;
                                                break;
                                        }
                                        var totalMacroDataLen = macroLen * macroWordLenInBytes;
                                        for (var j = 0; j < totalMacroDataLen; j++)
                                        {
                                            switch (macroOpenTypeWordSize)
                                            {
                                                case FurInstrMacroWordSize.UNSIGNED_8BIT:
                                                    byte macroByte = reader.ReadByte();
                                                    macroData.Add((int)macroByte);
                                                    blockLen--;
                                                    break;
                                                case FurInstrMacroWordSize.SIGNED_8BIT:
                                                    sbyte macroSByte = reader.ReadSByte();
                                                    macroData.Add((int)macroSByte & 0xFF);
                                                    blockLen--;
                                                    break;
                                                case FurInstrMacroWordSize.SIGNED_16BIT:
                                                    short macroSigned16Bit = reader.ReadInt16();
                                                    macroData.Add((int)macroSigned16Bit & 0xFFFF);
                                                    blockLen -= 2;
                                                    j++;
                                                    break;
                                                case FurInstrMacroWordSize.SIGNED_32BIT:
                                                    int macroSigned32Bit = reader.ReadInt32();
                                                    macroData.Add((int)(macroSigned32Bit & 0xFFFFFFFF));
                                                    blockLen -= 4;
                                                    j += 3;
                                                    break;
                                            }
                                        }

                                        FurInstrMacro thisMacro = new FurInstrMacro(macroCode, macroLoop, macroRelease, macroMode, instantRelease, macroType, macroWindowIsOpen, macroDelay, macroSpeed, macroData);
                                        thisInstr.AddMacro(thisMacro);
                                    }
                                    break;
                                case FurInstrFeature.C64_INS_DATA:
                                    break;
                                case FurInstrFeature.GAME_BOY_INS_DATA:
                                    byte gbInstEnvParams = reader.ReadByte();
                                    bit5 = GetBit(gbInstEnvParams, 5) ? 1 : 0;
                                    bit6 = GetBit(gbInstEnvParams, 6) ? 1 : 0;
                                    bit7 = GetBit(gbInstEnvParams, 7) ? 1 : 0;
                                    byte gbEnvLen = (byte)((bit7 << 2) | (bit6 << 1) | bit5);

                                    var gbEnvDir = GetBit(gbInstEnvParams, 4) ? 1 : 0;

                                    bit0 = GetBit(gbInstEnvParams, 0) ? 1 : 0;
                                    bit1 = GetBit(gbInstEnvParams, 1) ? 1 : 0;
                                    bit2 = GetBit(gbInstEnvParams, 2) ? 1 : 0;
                                    bit3 = GetBit(gbInstEnvParams, 3) ? 1 : 0;

                                    var gbEnvVol = (byte)((bit3 << 3) | (bit2 << 2) | (bit1 << 1) | bit0);

                                    byte gbInstSndLen = reader.ReadByte(); // 64 is infinity

                                    byte gbInstFlags = reader.ReadByte();

                                    byte gbInstHWSeqLen = reader.ReadByte();

                                    blockLen -= 4;

                                    List<FurInstrGBHWSeqCmd> gBHWSeqCmds = new List<FurInstrGBHWSeqCmd>();

                                    for (var j = 0; j < gbInstHWSeqLen; j++)
                                    {
                                        FurInstrGBHWSeqCmdType gbHWSeqCmdType = (FurInstrGBHWSeqCmdType)reader.ReadByte();
                                        var gbHWSeqDat1 = reader.ReadByte();
                                        var gbHWSeqDat2 = reader.ReadByte();

                                        FurInstrGBHWSeqCmd thisGBHWSeqCmd = new FurInstrGBHWSeqCmd(gbHWSeqCmdType, gbHWSeqDat1, gbHWSeqDat2);
                                        gBHWSeqCmds.Add(thisGBHWSeqCmd);

                                        blockLen -= 3;
                                    }

                                    FurInstrGB thisGBInstr = new FurInstrGB(gbEnvLen, gbEnvDir, gbEnvVol, gbInstSndLen, gbInstFlags, gbInstHWSeqLen, gBHWSeqCmds);
                                    var myName = thisInstr.GetName();
                                    thisInstr.SetInstrGB(thisGBInstr);
                                    break;
                                case FurInstrFeature.SAMPLE_INS_DATA:
                                    // SM
                                    short smInitialSample = reader.ReadInt16();
                                    var smFlags = reader.ReadByte();
                                    bool smUseSampleMap = GetBit(smFlags, 0);
                                    bool smUseSample = GetBit(smFlags, 1);
                                    bool smUseWave = GetBit(smFlags, 2);
                                    var smWaveformLen = reader.ReadByte();
                                    blockLen -= 4;

                                    List<FurSampleMapEntry> smMap = new List<FurSampleMapEntry>();
                                    if (smUseSampleMap)
                                    {
                                        for (var j = 0; j < 120; j++)
                                        {
                                            short smNoteToPlay = reader.ReadInt16();
                                            short smSampletoPlay = reader.ReadInt16();

                                            FurSampleMapEntry thisSampleMapEntry = new FurSampleMapEntry(smNoteToPlay, smSampletoPlay);
                                            smMap.Add(thisSampleMapEntry);
                                            blockLen -= 4;
                                        }
                                    }

                                    FurInstrSM thisSMInstr = new FurInstrSM(smInitialSample, smUseSampleMap, smUseSample, smUseWave, smWaveformLen, smMap);
                                    thisInstr.SetInstrSM(thisSMInstr);
                                    break;
                                case FurInstrFeature.OPERATOR_1_MACROS:
                                    break;
                                case FurInstrFeature.OPERATOR_2_MACROS:
                                    break;
                                case FurInstrFeature.OPERATOR_3_MACROS:
                                    break;
                                case FurInstrFeature.OPERATOR_4_MACROS:
                                    break;
                                case FurInstrFeature.OPL_DRUMS_MODE_DATA:
                                    /// TODO: Make better use of this data
                                    var fixedFreqMode = reader.ReadByte();
                                    short kickFreq = reader.ReadInt16();
                                    short snareHatFreq = reader.ReadInt16();
                                    short tomTopFreq = reader.ReadInt16();

                                    blockLen -= 7;
                                    break;
                                case FurInstrFeature.SNES_INS_DATA:
                                    break;
                                case FurInstrFeature.NAMCO_163_INS_DATA:
                                    break;
                                case FurInstrFeature.FDS_VIRTUAL_BOY_INS_DATA:
                                    break;
                                case FurInstrFeature.WAVETABLE_SYNTH_DATA:
                                    var wsFirstWave = reader.ReadInt32();
                                    var wsSecondWave = reader.ReadInt32();
                                    var wsRateDivider = reader.ReadByte();
                                    var wsEffect = reader.ReadByte();
                                    bool singleEffect = GetBit(wsEffect, 7); // If not single, then it is dual

                                    bool wsEnabled = reader.ReadByte() > 0;
                                    bool wsGlobal = reader.ReadByte() == 0;
                                    var wsSpeed = reader.ReadByte() + 1;

                                    var wsParam1 = reader.ReadByte();
                                    var wsParam2 = reader.ReadByte();
                                    var wsParam3 = reader.ReadByte();
                                    var wsParam4 = reader.ReadByte();

                                    FurInstrWS thisWSInstr = new FurInstrWS(wsFirstWave, wsSecondWave, wsRateDivider, wsEffect, singleEffect, wsEnabled, wsGlobal, wsSpeed, wsParam1, wsParam2, wsParam3, wsParam4);
                                    thisInstr.SetInstrWS(thisWSInstr);

                                    blockLen -= 17; // Subtract the number of bytes we just read from the feature block length
                                    break;
                                case FurInstrFeature.LIST_OF_SAMPLES:
                                    break;
                                case FurInstrFeature.LIST_OF_WAVETABLES:
                                    break;
                                case FurInstrFeature.MULTIPCM_INS_DATA:
                                    break;
                                case FurInstrFeature.SOUND_UNIT_INS_DATA:
                                    break;
                                case FurInstrFeature.ES5506_INS_DATA:
                                    break;
                                case FurInstrFeature.X1_010_INS_DATA:
                                    break;
                                case FurInstrFeature.NES_DPCM_SAMPLE_MAP_DATA:
                                    var useSampleMap = reader.ReadByte();
                                    List<byte> newDPCMSampleMapData = new List<byte>();
                                    if (useSampleMap > 0)
                                    {
                                        for (var samplePosition = 0; samplePosition < 120; samplePosition++) {
                                            var smpData = reader.ReadByte();
                                            newDPCMSampleMapData.Add(smpData);
                                        }

                                    }
                                    break;
                                case FurInstrFeature.ESFM_INS_DATA:
                                    // Undocumented? Takes up 19 bytes apparently...
                                    for (var j = 17; j > 0; j--)
                                    {
                                        reader.ReadByte();
                                        blockLen--;
                                    }
                                    break;
                                case FurInstrFeature.POWERNOISE_INS_DATA:
                                    break;
                                case FurInstrFeature.END_OF_FEATURES:
                                    stopLoop = true; // redundant failsafe
                                    break;
                            }
                        }
                        thisInstr.SetID(_furModuleInfo.GlobalInstruments.Count);
                        _furModuleInfo.GlobalInstruments.Add(thisInstr);
                    }
                    else
                    {
                        /// TODO: Add support for the legacy instrument format
                    }
                }

                /// Define all of the Wavetables based on their pointers
                _furModuleInfo.GlobalWavetables = new List<FurWavetable>();
                for (var i = 0; i < _furModuleInfo.WavetableCount; i++)
                {
                    reader.BaseStream.Seek(_furModuleInfo.WavetablePointers[i], SeekOrigin.Begin);
                    string wtBlockID = System.Text.Encoding.Default.GetString(reader.ReadBytes(4));
                    int wtBlockSize = reader.ReadInt32();
                    string wtName = GetNextString(sb, reader);
                    int wtWidth = reader.ReadInt32();
                    int wtReserved = reader.ReadInt32();
                    int wtHeight = reader.ReadInt32();

                    List<int> wtData = new List<int>();
                    for (var j = 0; j < wtWidth; j++)
                    {
                        int thisWTDataBlock = reader.ReadInt32();
                        wtData.Add(thisWTDataBlock);
                    }

                    FurWavetable thisWT = new FurWavetable(wtBlockID, wtBlockSize, wtName, wtWidth, wtReserved, wtHeight, wtData);
                    _furModuleInfo.GlobalWavetables.Add(thisWT);
                }

                _furModuleInfo.GlobalSamples = new List<FurSample>();
                for (var i = 0; i < _furModuleInfo.SampleCount; i++)
                {
                    if (_furHeader.GetVersion() >= 102)
                    {
                        reader.BaseStream.Seek(_furModuleInfo.SamplePointers[i], SeekOrigin.Begin);
                        string smpBlockID = System.Text.Encoding.Default.GetString(reader.ReadBytes(4));
                        int smpBlockSize = reader.ReadInt32();
                        string smpName = GetNextString(sb, reader);
                        int smpLen = reader.ReadInt32();
                        int smpCompatibilityRate = reader.ReadInt32();
                        int smpC4Rate = reader.ReadInt32();
                        FurSampleDepth smpDepth = (FurSampleDepth)reader.ReadByte();
                        byte smpLoopDirection = reader.ReadByte();
                        byte smpFlags = reader.ReadByte();
                        byte smpFlags2 = reader.ReadByte();

                        int smpLoopStart = reader.ReadInt32(); // -1 means No Loop
                        int smpLoopEnd = reader.ReadInt32(); // -1 means No Loop

                        // Sample Presence Bitfields
                        // For future use
                        // Indicates whether the sample should be present in the memory of a system.
                        // Read 4 32-bit numbers (for 4 memory banks per system,
                        // e.g.YM2610 does ADPCM - A and ADPCM - B on separate memory banks).
                        reader.ReadBytes(16);

                        // Sample Data
                        List<byte> sampleBlocks = new List<byte>();
                        byte smpBlock;
                        switch (smpDepth)
                        {
                            case FurSampleDepth.ZX_SPECTRUM_OVERLAY_DRUM_1BIT:
                                break;
                            case FurSampleDepth.ONE_BIT_NES_DPCM_1BIT:
                                break;
                            case FurSampleDepth.YMZ_ADPCM:
                                break;
                            case FurSampleDepth.QSOUND_ADPCM:
                                break;
                            case FurSampleDepth.ADPCM_A:
                                break;
                            case FurSampleDepth.ADPCM_B:
                                break;
                            case FurSampleDepth.K05_ADPCM:
                                break;
                            case FurSampleDepth.EIGHT_BIT_PCM:
                                break;
                            case FurSampleDepth.BRR_SNES:
                                float brrResolution = 9 / 16f;
                                int brrLen = (int)Math.Round(smpLen * brrResolution);
                                for (var j = 0; j < brrLen; j++)
                                {
                                    smpBlock = reader.ReadByte();
                                    sampleBlocks.Add(smpBlock);
                                }
                                break;
                            case FurSampleDepth.VOX:
                                break;
                            case FurSampleDepth.EIGHT_BIT_ULAW_PCM:
                                break;
                            case FurSampleDepth.C219_PCM:
                                break;
                            case FurSampleDepth.IMA_ADPCM:
                                break;
                            case FurSampleDepth.SIXTEEN_BIT_PCM:
                                break;
                        }
                        FurSample thisSample = new FurSample(smpBlockID, smpBlockSize, smpName, smpLen, smpCompatibilityRate, smpC4Rate, smpDepth, smpLoopDirection, smpFlags, smpFlags2, smpLoopStart, smpLoopEnd, sampleBlocks);
                        //thisSample.Decode();
                        _furModuleInfo.GlobalSamples.Add(thisSample);









                    }
                    else
                    {

                    }
                }

                // Populate all the channels for every song.
                foreach (FurSong s in _furSongs)
                {
                    var chnIDCounter = 0;
                    foreach (FurChipCounter fcc in _furModuleInfo.SoundChipChanCountList)
                    {
                        var chnCount = fcc.ChanCount;
                        for (var i = 0; i < chnCount; i++)
                        {
                            s.AddChan(fcc.Chip, chnIDCounter);
                            chnIDCounter++;
                        }
                    }
                }

                // Patterns
                for (var i = 0; i < _furModuleInfo.PatternCountGlobal; i++)
                {
                    if (_furHeader.GetVersion() >= 157)
                    {
                        reader.BaseStream.Seek(_furModuleInfo.PatternPointers[i], SeekOrigin.Begin);

                        string ptnBlockID = System.Text.Encoding.Default.GetString(reader.ReadBytes(4));
                        int ptnBlockSize = reader.ReadInt32();
                        int ptnSubSong = reader.ReadByte();
                        int ptnChannelID = reader.ReadByte();
                        int ptnIndex = reader.ReadUInt16();
                        string ptnName = GetNextString(sb, reader);

                        FurSong ptnParentSong = _furSongs[ptnSubSong];
                        FurChannel ptnChannel = ptnParentSong.GetChannel(ptnChannelID);
                        int ptnEffectColumnCount = ptnParentSong.EffectColumnCounts[ptnChannelID];
                        int ptnLen = ptnParentSong.PatternLen;

                        FurPatternData thisPattern = new FurPatternData(ptnBlockID, ptnParentSong, ptnChannel, ptnLen, ptnIndex);
                        for (var row = 0; row < ptnLen; row++)
                        {
                            FurPatternRowData thisRow = new FurPatternRowData();
                            bool dataWritten = false;

                            var firstByte = reader.ReadByte();
                            if (firstByte == 0xFF)
                                break;

                            bit7 = GetBit(firstByte, 7) ? 1 : 0;
                            if (bit7 > 0)
                            {
                                byte skipCounter = (byte)(firstByte & 0b00111111); // Masking to extract the first 6 bits
                                row += skipCounter + 1;
                                continue;
                            }
                            else
                            {
                                int rowNotePresent = 0;
                                int rowInstPresent = 0;
                                int rowVolPresent = 0;
                                int rowFX0Present = 0;
                                int rowFXVal0Present = 0;
                                int otherFX03Present = 0;
                                int otherFX47Present = 0;

                                int rowFX1Present = 0;
                                int rowFXVal1Present = 0;
                                int rowFX2Present = 0;
                                int rowFXVal2Present = 0;
                                int rowFX3Present = 0;
                                int rowFXVal3Present = 0;
                                int rowFX4Present = 0;
                                int rowFXVal4Present = 0;
                                int rowFX5Present = 0;
                                int rowFXVal5Present = 0;
                                int rowFX6Present = 0;
                                int rowFXVal6Present = 0;
                                int rowFX7Present = 0;
                                int rowFXVal7Present = 0;

                                /* 
                                 |   - bit 0: note present
                                 |   - bit 1: ins present
                                 |   - bit 2: volume present
                                 |   - bit 3: effect 0 present
                                 |   - bit 4: effect value 0 present
                                 |   - bit 5: other effects (0-3) present
                                 |   - bit 6: other effects (4-7) present
                                 */
                                rowNotePresent = bit0 = GetBit(firstByte, 0) ? 1 : 0;
                                rowInstPresent = bit1 = GetBit(firstByte, 1) ? 1 : 0;
                                rowVolPresent = bit2 = GetBit(firstByte, 2) ? 1 : 0;
                                rowFX0Present = bit3 = GetBit(firstByte, 3) ? 1 : 0;
                                rowFXVal0Present = bit4 = GetBit(firstByte, 4) ? 1 : 0;
                                otherFX03Present = bit5 = GetBit(firstByte, 5) ? 1 : 0;
                                otherFX47Present = bit6 = GetBit(firstByte, 6) ? 1 : 0;

                                if (bit5 > 0)
                                {
                                    /*
                                     | - if bit 5 is set, read another byte:
                                     |   - bit 0: effect 0 present
                                     |   - bit 1: effect value 0 present
                                     |   - bit 2: effect 1 present
                                     |   - bit 3: effect value 1 present
                                     |   - bit 4: effect 2 present
                                     |   - bit 5: effect value 2 present
                                     |   - bit 6: effect 3 present
                                     |   - bit 7: effect value 3 present*/
                                    var secondByte = reader.ReadByte();
                                    rowFX0Present = bit0 = GetBit(secondByte, 0) ? 1 : 0;
                                    rowFXVal0Present = bit1 = GetBit(secondByte, 1) ? 1 : 0;
                                    rowFX1Present = bit2 = GetBit(secondByte, 2) ? 1 : 0;
                                    rowFXVal1Present = bit3 = GetBit(secondByte, 3) ? 1 : 0;
                                    rowFX2Present = bit4 = GetBit(secondByte, 4) ? 1 : 0;
                                    rowFXVal2Present = bit5 = GetBit(secondByte, 5) ? 1 : 0;
                                    rowFX3Present = bit6 = GetBit(secondByte, 6) ? 1 : 0;
                                    rowFXVal3Present = bit7 = GetBit(secondByte, 7) ? 1 : 0;

                                    if (bit6 > 0)
                                    {
                                        /*
                                         | - if bit 6 is set, read another byte:
                                         |   - bit 0: effect 4 present
                                         |   - bit 1: effect value 4 present
                                         |   - bit 2: effect 5 present
                                         |   - bit 3: effect value 5 present
                                         |   - bit 4: effect 6 present
                                         |   - bit 5: effect value 6 present
                                         |   - bit 6: effect 7 present
                                         |   - bit 7: effect value 7 present
                                         */
                                        var thirdByte = reader.ReadByte();
                                        rowFX4Present = bit0 = GetBit(thirdByte, 0) ? 1 : 0;
                                        rowFXVal4Present = bit1 = GetBit(thirdByte, 1) ? 1 : 0;
                                        rowFX5Present = bit2 = GetBit(thirdByte, 2) ? 1 : 0;
                                        rowFXVal5Present = bit3 = GetBit(thirdByte, 3) ? 1 : 0;
                                        rowFX6Present = bit4 = GetBit(thirdByte, 4) ? 1 : 0;
                                        rowFXVal6Present = bit5 = GetBit(thirdByte, 5) ? 1 : 0;
                                        rowFX7Present = bit6 = GetBit(thirdByte, 6) ? 1 : 0;
                                        rowFXVal7Present = bit7 = GetBit(thirdByte, 7) ? 1 : 0;
                                    }
                                }

                                /*
                                 | - then read note, ins, volume, effects and effect values depending on what is present.
                                 | - for note:
                                 |   - 0 is C-(-5)
                                 |   - 179 is B-9
                                 |   - 180 is note off
                                 |   - 181 is note release
                                 |   - 182 is macro release
                                 */

                                int rowNoteVal = -9999, rowInstVal = -9999, rowVolVal = -9999,
                                    rowFX0 = -9999, rowFX0Val = -9999, rowFX1 = -9999, rowFX1Val = -9999,
                                    rowFX2 = -9999, rowFX2Val = -9999, rowFX3 = -9999, rowFX3Val = -9999,
                                    rowFX4 = -9999, rowFX4Val = -9999, rowFX5 = -9999, rowFX5Val = -9999,
                                    rowFX6 = -9999, rowFX6Val = -9999, rowFX7 = -9999, rowFX7Val = -9999;
                                string rowNoteName = "Undefined";

                                if (rowNotePresent > 0)
                                {
                                    rowNoteVal = reader.ReadByte();
                                    rowNoteName = _noteNames[rowNoteVal];
                                    dataWritten = true;
                                }

                                if (rowInstPresent > 0)
                                {
                                    rowInstVal = reader.ReadByte();
                                    dataWritten = true;
                                }

                                if (rowVolPresent > 0)
                                {
                                    rowVolVal = reader.ReadByte();
                                    dataWritten = true;
                                }

                                if (rowFX0Present > 0)
                                {
                                    rowFX0 = reader.ReadByte();
                                    dataWritten = true;
                                }

                                if (rowFXVal0Present > 0)
                                {
                                    rowFX0Val = reader.ReadByte();
                                    dataWritten = true;
                                }

                                if (rowFX1Present > 0)
                                {
                                    rowFX1 = reader.ReadByte();
                                    dataWritten = true;
                                }

                                if (rowFXVal1Present > 0)
                                {
                                    rowFX1Val = reader.ReadByte();
                                    dataWritten = true;
                                }

                                if (rowFX2Present > 0)
                                {
                                    rowFX2 = reader.ReadByte();
                                    dataWritten = true;
                                }

                                if (rowFXVal2Present > 0)
                                {
                                    rowFX2Val = reader.ReadByte();
                                    dataWritten = true;
                                }

                                if (rowFX3Present > 0)
                                {
                                    rowFX3 = reader.ReadByte();
                                    dataWritten = true;
                                }

                                if (rowFXVal3Present > 0)
                                {
                                    rowFX3Val = reader.ReadByte();
                                    dataWritten = true;
                                }

                                if (rowFX4Present > 0)
                                {
                                    rowFX4 = reader.ReadByte();
                                    dataWritten = true;
                                }

                                if (rowFXVal4Present > 0)
                                {
                                    rowFX4Val = reader.ReadByte();
                                    dataWritten = true;
                                }

                                if (rowFX5Present > 0)
                                {
                                    rowFX5 = reader.ReadByte();
                                    dataWritten = true;
                                }

                                if (rowFXVal5Present > 0)
                                {
                                    rowFX5Val = reader.ReadByte();
                                    dataWritten = true;
                                }

                                if (rowFX6Present > 0)
                                {
                                    rowFX6 = reader.ReadByte();
                                    dataWritten = true;
                                }

                                if (rowFXVal6Present > 0)
                                {
                                    rowFX6Val = reader.ReadByte();
                                    dataWritten = true;
                                }

                                if (rowFX7Present > 0)
                                {
                                    rowFX7 = reader.ReadByte();
                                    dataWritten = true;
                                }

                                if (rowFXVal7Present > 0)
                                {
                                    rowFX7Val = reader.ReadByte();
                                    dataWritten = true;
                                }

                                if (dataWritten)
                                {
                                    thisRow.SetData(row, rowNoteVal, rowNoteName, rowInstVal, rowVolVal,
                                        rowFX0Present, rowFXVal0Present, rowFX0, rowFX0Val,
                                        rowFX1Present, rowFXVal1Present, rowFX1, rowFX1Val,
                                        rowFX2Present, rowFXVal2Present, rowFX2, rowFX2Val,
                                        rowFX3Present, rowFXVal3Present, rowFX3, rowFX3Val,
                                        rowFX4Present, rowFXVal4Present, rowFX4, rowFX4Val,
                                        rowFX5Present, rowFXVal5Present, rowFX5, rowFX5Val,
                                        rowFX6Present, rowFXVal6Present, rowFX6, rowFX6Val,
                                        rowFX7Present, rowFXVal7Present, rowFX7, rowFX7Val);
                                    thisPattern.AppendRowData(thisRow);
                                }
                            }
                        }

                        ptnChannel.AddPattern(ptnIndex, thisPattern);

                    }
                    else
                    {
                        /// Old pattern behavior
                        /*
                         * size | description
                            -----|------------------------------------
                              4  | "PATR" block ID
                              4  | size of this block
                              2  | channel
                              2  | pattern index
                              2  | subsong (>=95) or reserved
                              2  | reserved
                             ??? | pattern data
                                 | - size: rows*(4+effectColumns*2)*2
                                 | - read shorts in this order:
                                 |   - note
                                 |     - 0: empty/invalid
                                 |     - 1: C#
                                 |     - 2: D
                                 |     - 3: D#
                                 |     - 4: E
                                 |     - 5: F
                                 |     - 6: F#
                                 |     - 7: G
                                 |     - 8: G#
                                 |     - 9: A
                                 |     - 10: A#
                                 |     - 11: B
                                 |     - 12: C (of next octave)
                                 |       - this is actually a leftover of the .dmf format.
                                 |     - 100: note off
                                 |     - 101: note release
                                 |     - 102: macro release
                                 |   - octave
                                 |     - this is an signed char stored in a short.
                                 |     - therefore octave value 255 is actually octave -1.
                                 |     - yep, another leftover of the .dmf format...
                                 |   - instrument
                                 |   - volume
                                 |   - effect and effect data (× effect columns)
                                 | - for note/octave, if both values are 0 then it means empty.
                                 | - for instrument, volume, effect and effect data, a value of -1 means empty.
                             STR | pattern name (>=51)
                         */
                    }
                }

            }
        }

        private void InitInstrFeatureLookupDict()
        {
            InstrFeatureDict = new Dictionary<string, FurInstrFeature>();
            InstrFeatureDict.Add("NA", FurInstrFeature.INSTRUMENT_NAME);
            InstrFeatureDict.Add("FM", FurInstrFeature.FM_INS_DATA);
            InstrFeatureDict.Add("MA", FurInstrFeature.MACRO_DATA);
            InstrFeatureDict.Add("64", FurInstrFeature.C64_INS_DATA);
            InstrFeatureDict.Add("GB", FurInstrFeature.GAME_BOY_INS_DATA);
            InstrFeatureDict.Add("SM", FurInstrFeature.SAMPLE_INS_DATA);
            InstrFeatureDict.Add("O1", FurInstrFeature.OPERATOR_1_MACROS);
            InstrFeatureDict.Add("O2", FurInstrFeature.OPERATOR_2_MACROS);
            InstrFeatureDict.Add("O3", FurInstrFeature.OPERATOR_3_MACROS);
            InstrFeatureDict.Add("O4", FurInstrFeature.OPERATOR_4_MACROS);
            InstrFeatureDict.Add("LD", FurInstrFeature.OPL_DRUMS_MODE_DATA);
            InstrFeatureDict.Add("SN", FurInstrFeature.SNES_INS_DATA);
            InstrFeatureDict.Add("N1", FurInstrFeature.NAMCO_163_INS_DATA);
            InstrFeatureDict.Add("FD", FurInstrFeature.FDS_VIRTUAL_BOY_INS_DATA);
            InstrFeatureDict.Add("WS", FurInstrFeature.WAVETABLE_SYNTH_DATA);
            InstrFeatureDict.Add("SL", FurInstrFeature.LIST_OF_SAMPLES);
            InstrFeatureDict.Add("WL", FurInstrFeature.LIST_OF_WAVETABLES);
            InstrFeatureDict.Add("MP", FurInstrFeature.MULTIPCM_INS_DATA);
            InstrFeatureDict.Add("SU", FurInstrFeature.SOUND_UNIT_INS_DATA);
            InstrFeatureDict.Add("ES", FurInstrFeature.ES5506_INS_DATA);
            InstrFeatureDict.Add("X1", FurInstrFeature.X1_010_INS_DATA);
            InstrFeatureDict.Add("NE", FurInstrFeature.NES_DPCM_SAMPLE_MAP_DATA);
            InstrFeatureDict.Add("EF", FurInstrFeature.ESFM_INS_DATA);
            InstrFeatureDict.Add("PN", FurInstrFeature.POWERNOISE_INS_DATA);
            InstrFeatureDict.Add("EN", FurInstrFeature.END_OF_FEATURES);
        }

        public FurSong GetSong(int songIndex)
        {
            return _furSongs[songIndex];
        }

        private string GetNextString(StringBuilder sb, BinaryReader reader)
        {
            sb.Clear();
            int strLenFailsafe = 5000;
            char rb = reader.ReadChar();
            while (rb != '\0')
            {
                sb.Append(rb);
                rb = reader.ReadChar();
                strLenFailsafe--;
                if (strLenFailsafe <= 0)
                    throw new Exception("String is too long or corrupted... Aborting.");
            }
            return sb.ToString();
        }

        private void InitChanLookupDict()
        {
            _furModuleInfo.ChanCountLookup = new Dictionary<FurChipType, int>();
            _furModuleInfo.ChanCountLookup.Add(FurChipType.END_OF_LIST, 0);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.YMU759, 17);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.GENESIS, 10);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.SMS_SN76489, 4);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.GAME_BOY, 4);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.PC_ENGINE, 6);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.NES, 5);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.C64_8580, 3);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.ARCADE_YM2151_AND_SEGAPCM, 13);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.NEO_GEO_CD_YM2610, 13);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.GENESIS_EXTENDED, 13);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.SMS_SN76489_AND_OPLL_YM2413, 13);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.NES_AND_VRC7, 11);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.C64_6581, 3);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.NEO_GEO_CD_EXTENDED, 16);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.AY_3_8910, 3);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.AMIGA, 4);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.YM2151, 8);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.YM2612, 6);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.TIA, 2);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.VIC_20, 4);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.PET, 1);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.SNES, 8);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.VRC6, 3);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.OPLL_YM2413, 9);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.FDS, 1);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.MMC5, 3);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.NAMCO_163, 8);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.YM2203, 6);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.YM2608, 16);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.OPL_YM3526, 9);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.OPL2_YM3812, 9);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.OPL3_YMF262, 18);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.MULTIPCM, 28);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.INTEL_8253_BEEPER, 1);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.POKEY, 4);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.RF5C68, 8);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.WONDERSWAN, 4);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.PHILIPS_SAA1099, 6);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.OPZ_YM2414, 8);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.POKEMON_MINI, 1);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.AY8930, 3);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.SEGAPCM, 16);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.VIRTUAL_BOY, 6);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.VRC7, 6);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.YM2610B, 16);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.ZX_SPECTRUM_BEEPER_TILDEARROW_ENGINE, 6);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.YM2612_EXTENDED, 9);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.KONAMI_SCC, 5);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.OPL_DRUMS_YM3526, 11);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.OPL2_DRUMS_YM3812, 11);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.OPL3_DRUMS_YMF262, 20);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.NEO_GEO_YM2610, 14);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.NEO_GEO_EXTENDED_YM2610, 17);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.OPLL_DRUMS_YM2413, 11);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.ATARI_LYNX, 4);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.SEGAPCM_FOR_DEFLEMASK_COMPATIBILITY, 5);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.MSM6295, 4);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.MSM6258, 1);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.COMMANDER_X16_VERA, 17);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.BUBBLE_SYSTEM_WSG, 2);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.OPL4_YMF278B, 42);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.OPL4_DRUMS_YMF278B, 44);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.SETA_ALLUMER_X1_010, 16);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.ENSONIQ_ES5506, 32);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.YAMAHA_Y8950, 10);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.YAMAHA_Y8950_DRUMS, 12);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.KONAMI_SCC_PLUS, 5);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.TILDEARROW_SOUND_UNIT, 8);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.YM2203_EXTENDED, 9);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.YM2608_EXTENDED, 19);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.YMZ280B, 8);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.NAMCO_WSG, 3);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.NAMCO_C15, 8);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.NAMCO_C30, 8);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.MSM5232, 8);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.YM2612_DUALPCM_EXTENDED, 11);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.YM2612_DUALPCM, 7);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.T6W28, 4);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.PCM_DAC, 1);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.YM2612_CSM, 10);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.NEO_GEO_CSM_YM2610, 18);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.YM2203_CSM, 10);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.YM2608_CSM, 20);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.YM2610B_CSM, 20);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.K007232, 2);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.GA20, 4);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.SM8521, 3);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.M114S, 16);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.ZX_SPECTRUM_BEEPER_QUADTONE_ENGINE, 5);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.CASIO_PV_1000, 3);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.K053260, 4);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.TED, 2);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.NAMCO_C140, 24);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.NAMCO_C219, 16);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.NAMCO_C352, 32);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.ESFM, 18);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.ENSONIQ_ES5503_HARD_PAN, 32);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.POWERNOISE, 4);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.DAVE, 6);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.NDS, 16);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.GAME_BOY_ADVANCE_DIRECT, 2);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.GAME_BOY_ADVANCE_MINMOD, 16);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.YM2610B_EXTENDED, 19);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.QSOUND, 19);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.FIVE_E_ZERO_ONE, 5);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.PONG, 1);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.DUMMY_SYSTEM, 8);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.RESERVED_FOR_DEVELOPMENT, 0);
            _furModuleInfo.ChanCountLookup.Add(FurChipType.RESERVED_FOR_DEVELOPMENT_2, 0);
        }

        public FurModuleInfo GetModuleInfo()
        {
            return _furModuleInfo;
        }

        public static bool GetBit(byte b, int bitNumber)
        {
            bitNumber++;
            return (b & 1 << bitNumber - 1) != 0;
        }

        public static byte[] DecompressZLib(byte[] gzip)
        {
            using (var stream = new ZLibStream(new MemoryStream(gzip), CompressionMode.Decompress))
            {
                var outStream = new MemoryStream();
                const int size = 999999;
                byte[] buffer = new byte[size];

                int read;
                while ((read = stream.Read(buffer, 0, size)) > 0)
                {
                    outStream.Write(buffer, 0, read);
                    read = 0;
                }

                return outStream.ToArray();
            }
        }

        public override string ToString()
        {
            return sb.ToString();
        }
    }
}