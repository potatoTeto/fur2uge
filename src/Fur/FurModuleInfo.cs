namespace fur2Uge
{
    public partial class FurFile
    {
        public struct FurModuleInfo
        {
            public FurModuleInfo() { }

            public Dictionary<FurChipType, int> ChanCountLookup;
            public List<FurChipCounter> SoundChipChanCountList;

            public int InstrumentCount;
            public int WavetableCount;
            public int SampleCount;
            public int PatternCountGlobal;
            public List<FurChipType> SoundChipTypeList;
            public sbyte[] SoundChipVolList;
            public sbyte[] SoundChipPanList;
            public byte[] SoundChipFlagPointers;
            public bool[] SoundChipLegacyFlags;
            public string GlobalModuleName;
            public string GlobalAuthor;
            public float A4Tuning;
            public byte LimitSlides;
            public FurPitchLinearity LinearPitch;
            public byte LoopModality;
            public byte ProperNoiseLayout;
            public byte WaveDutyIsVolume;
            public byte ResetMacroOnPorta;
            public byte LegacyVolumeSlides;
            public byte CompatibleArpeggio;
            public byte NoteOffResetsSlides;
            public byte TargetResetsSlides;
            public byte ArpsInhibitPortamento;
            public byte WackAlgoMacro;
            public byte BrokenShortcutSlides;
            public byte IgnoreDuplicateSlides;
            public byte StopPortamentoOnNoteOff;
            public byte ContinuousVibrato;
            public byte BrokenDACMode;
            public byte OneTickCut;
            public byte InstrumentChangeAllowedDuringPorta;
            public byte ResetNoteBaseOnArpEffectStop;
            public List<int> InstrumentPointers;
            public List<int> WavetablePointers;
            public List<int> SamplePointers;
            public List<int> PatternPointers;
            public int TotalChanCount = 0;
            public string SongComment;
            public float MasterVolume;

            public byte BrokenSpeedSelection;
            public byte NoSlidesOnFirstTick;
            public byte NextRowResetArpPos;
            public byte IgnoreJumpAtEnd;
            public byte BuggyPortamentoAfterSlide;
            public byte NewInsAffectsEnvelope;
            public byte ExtChChannelStateIsShared;
            public byte IgnoreDACModeChangeOutsideOfIntendedChannel;
            public byte E1XYAndE2XYAlsoTakePriorityOverSlide00;
            public byte NewSegaPCM;
            public byte WeirdFNum;
            public byte SNDutyMacroAlwaysResetsPhase;
            public byte PitchMacroIsLinear;
            public byte PitchSlideSpeedInFullLinearPitchMode;
            public byte OldOctaveBoundaryBehavior;
            public byte DisableOPN2DACVolControl;
            public byte NewVolScalingStrat;
            public byte VolMacroStillAppliesAfterEnd;
            public byte BrokenOutVol;
            public byte E1XYAndE2XYStopOnSameNote;
            public byte BrokenInitialPosOfPortaAfterArp;
            public byte SNPeriodsUnder8AreTreatedAs1;
            public byte CutDelayEffectPolicy;
            public byte BAndDEffectTreatment;
            public byte AutoSysNameDetection;
            public byte DisableSampleMacro;
            public byte BrokenOutVolEpisode2;
            public byte OldArpStrat;
            public byte NumOfAdditionalSubsongs;
            public int ReservedAdditionalSubsongs;
            public List<int> SubsongDataPointers;
            public string SysName;
            public string AlbumCategoryGameName;
            public string SongNameJP;
            public string GlobalAuthorJP;
            public string SysNameJP;
            public string AlbumCategoryGameNameJP;

            public int PatchBayConnectionCount;
            public List<FurPatchBay> PatchBayList;

            public byte AutoPatchBay;
            public byte BrokenPortamentoDuringLegato;
            public byte BrokenMacroDuringNoteOffInSomeFMChips;
            public byte C64PreNoteDoesNotCompensateForPortamentoOrLegato;
            public byte DisableNewNESDPCMFeatures;
            public byte ResetArpEffectPhaseOnNewNote;
            public byte LinearVolumeScalingRoundsUp;
            public byte LegacyAlwaysSetVolBehavior;
            public byte ReservedMoreCompatFlags;

            public byte GrooveCount;
            public List<FurGrooveEntry> GrooveEntriesList;

            public int InstrumentDirectoriesPointer;
            public int WavetableDirectoriesPointer;
            public int SampleDirectoriesPointer;

            public List<FurInstrument> GlobalInstruments;
            public List<FurWavetable> GlobalWavetables;
            public List<FurSample> GlobalSamples;
        }
    }
}