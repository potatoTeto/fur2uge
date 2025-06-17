namespace fur2Uge
{
    public partial class UgeFile
    {
        public enum UgeInstrumentType
        {
            PULSE,
            WAVETABLE,
            NOISE
        };

        public enum UgeWaveVolumes
        {
            ZERO = 0x0,
            FULL = 0x1,
            HALF = 0x2,
            QUARTER = 0x3,
        };

        public class UgeInstrument
        {
            private uint _type;
            private UgeInstrumentType _typeEnum;
            private shortstring _name;
            private uint _length = 0x3F;
            private bool _lengthEnabled = false;
            private byte _initialVolume;
            private uint _volSweepDir; // 0 = Increase, 1 = Decrease
            private byte _volSweepSpeed;
            private uint _freqSweepTime;
            private uint _freqSweepDirection; // 1 = Enabled, 0 = Disabled
            private uint _freqSweepShift;
            private byte _dutyCycle;
            private uint _wavetableVolume;
            private uint _wavetableIndex;
            private byte panState = 0x3;

            // Write the below if Header Version Number < 6:
            private bool _subPatternEnabled;
            private UgePatternRow[] _subPattern;

            // Only use if Version Number >=4 && Version Number < 6
            public sbyte[] _noiseMacroData;

            private uint _noiseMode; // (0 = 15 bit, 1 = 7 bit)

            public UgeInstrument(uint instrumentType)
            {
                _name.Val = string.Empty;
                _type = instrumentType;
                _typeEnum = (UgeInstrumentType)instrumentType;
                _initialVolume = 0xF;
                _volSweepDir = 0x0;
                _freqSweepDirection = 0x1;
                _wavetableVolume = 0x1;
                _noiseMacroData = new sbyte[6];
                _subPattern = new UgePatternRow[64];
                for (var i = 0; i < _subPattern.Length; i++)
                {
                    _subPattern[i] = new UgePatternRow(true);
                }
            }

            public UgeInstrument(string name, uint instrumentType, uint wavetableVolume, uint wavetableIndex, List<FurInstrMacro> macros, int panMacroOnChannel = 1)
            {
                _name.Val = name;
                _type = instrumentType;
                _typeEnum = (UgeInstrumentType)instrumentType;
                _initialVolume = 0xF;
                _volSweepDir = 0x0;
                _freqSweepDirection = 0x1;
                _wavetableVolume = wavetableVolume;
                _wavetableIndex = wavetableIndex;
                _noiseMacroData = new sbyte[6];
                _subPattern = new UgePatternRow[64];

                ParseMacros(macros, panMacroOnChannel);
            }

            public UgeInstrument(string name, uint instrumentType,
                byte volSweepSpeed,                     // _gbEnvLen
                uint volSweepDir,                       // _gbEnvDir
                byte initialVolume,                    // _gbEnvVol
                uint length,                            // _gbSndLen
                byte gbFlags,                           // _gbFlags
                byte gmHWSeqLen,                       // _gbHWSeqLen
                List<FurInstrGBHWSeqCmd> gbHWSeqCmds, // _gbHWSeqCmds
                List<FurInstrMacro> macros,
                sbyte[] noiseMacroData,
                byte freqSweepDirection = 0x1,
                byte wavetableVol = 0x1,
                int panMacroOnChannel = 1
                )
            {
                _name.Val = name;
                _type = instrumentType;
                _typeEnum = (UgeInstrumentType)instrumentType;
                _initialVolume = initialVolume;
                _volSweepDir = volSweepDir;
                _volSweepSpeed = volSweepSpeed;
                _length = length;

                if (_length < 0x3F)
                    _lengthEnabled = true;
                else
                    _length = 0x3F; // Never go over the cap length value

                ParseMacros(macros, panMacroOnChannel);

                if (gbHWSeqCmds.Count > 0)
                {
                    switch (gbHWSeqCmds[0].GetHWSeqCmdType())
                    {
                        case FurFile.FurInstrGBHWSeqCmdType.SET_ENVELOPE:
                            break;
                        case FurFile.FurInstrGBHWSeqCmdType.SET_SWEEP:
                            // return (_gbHWSeqSweepSpeed, _gbHWSeqSweepDir, _gbHWSeqShiftVal);
                            var sweepParams = gbHWSeqCmds[0].GetHWSeqShiftParams();
                            _freqSweepDirection = (uint)sweepParams.Item2;
                            _freqSweepTime = sweepParams.Item1;
                            _freqSweepShift = sweepParams.Item3;
                            break;
                    }
                }
                else
                {
                    _freqSweepDirection = freqSweepDirection;
                }
                _wavetableVolume = wavetableVol;
                if (noiseMacroData != null)
                    _noiseMacroData = noiseMacroData;
                else
                    _noiseMacroData = new sbyte[6];
            }

            private void ParseMacros(List<FurInstrMacro> macros, int panMacroOnChannel)
            {
                _subPattern = new UgePatternRow[64];
                for (var i = 0; i < _subPattern.Length; i++)
                {
                    _subPattern[i] = new UgePatternRow(true);
                }

                // Keep track of the macro containing the longest loop point
                (int, int) highestJumpVal = (-9, -9); // Row, Loop Point

                // Iterate through all of the macros and populate the subpattern
                for (var macroIndex = macros.Count - 1; macroIndex >= 0; macroIndex--)
                {
                    var thisMacro = macros[macroIndex];
                    var code = thisMacro.GetMacroCode();
                    var data = thisMacro.GetMacroData();
                    var loopPoint = (byte)thisMacro.GetLoopPoint();
                    //var type = m.GetMacroType();

                    if (data.Count > 1)
                        _subPatternEnabled = true;

                    switch (code)
                    {
                        case FurFile.FurInstrMacroCode.PAN_L:
                            if (data.Count > 1)
                            {
                                for (var macroDataPos = 0; macroDataPos < data.Count; macroDataPos++)
                                {
                                    var panVal = (byte)data[macroDataPos];

                                    // Check if bitA is set
                                    bool rightSpeakerOn = (panVal & (1 << 0)) != 0;

                                    // Check if bitB is set
                                    bool leftSpeakerOn = (panVal & (1 << 1)) != 0;

                                    int bitA = panMacroOnChannel - 1;     // Bit position A
                                    int bitB = panMacroOnChannel + 3;     // Bit position B

                                    int panFinalVal = 0xFF;

                                    // Toggle bit A if rightSpeakerOn is true
                                    if (rightSpeakerOn)
                                        panFinalVal |= (1 << bitA); // Set bit A
                                    else
                                        panFinalVal &= ~(1 << bitA); // Clear bit A


                                    // Toggle bit B if leftSpeakerOn is true
                                    if (leftSpeakerOn)
                                        panFinalVal |= (1 << bitB); // Set bit B
                                    else
                                        panFinalVal &= ~(1 << bitB); // Clear bit B

                                    _subPattern[macroDataPos].SetEffect(UgeEffectTable.SET_PANNING, (byte)panFinalVal);
                                }
                            }
                            break;
                        case FurFile.FurInstrMacroCode.VOL:
                            _initialVolume = (byte)data[0];
                            break;
                        case FurFile.FurInstrMacroCode.ARP:
                            if (data.Count > 1)
                            {
                                for (var i = 0; i < data.Count; i++)
                                {
                                    sbyte arpVal = (sbyte)data[i];

                                    _subPattern[i].SetNote((UgeNoteTable)(0x24 + arpVal));
                                }
                            }
                            break;
                        case FurFile.FurInstrMacroCode.PITCH:
                            if (data.Count > 1)
                            {
                                for (var i = 0; i < data.Count; i++)
                                {
                                    sbyte pitchVal = (sbyte)data[i];
                                    UgeEffectTable pitchCmd;

                                    if (pitchVal >= 0)
                                    {
                                        pitchCmd = UgeEffectTable.PORTAMENTO_UP;
                                    }
                                    else
                                    {
                                        pitchCmd = UgeEffectTable.PORTAMENTO_DOWN;
                                        pitchVal = (sbyte)(0xFF - (0xFF + pitchVal));
                                    }

                                    _subPattern[i].SetEffect(pitchCmd, (byte)(pitchVal / 8));
                                }
                            }
                            break;
                        case FurFile.FurInstrMacroCode.DUTY:
                            _dutyCycle = (byte)data[0];

                            if (data.Count > 1)
                            {
                                for (var i = 0; i < data.Count; i++)
                                {
                                    _subPattern[i].SetEffect(UgeEffectTable.SET_DUTY_CYCLE, (byte)(data[i] * 0x40));
                                }
                            }
                            break;
                    }

                    if (data.Count > 1 && loopPoint < 0xFF)
                    {
                        if (highestJumpVal.Item2 < loopPoint)
                            highestJumpVal = (data.Count - 1, loopPoint);
                    }
                }

                // Set the macro's loop point based on the longest macro found was, if applicable
                if (highestJumpVal != (-9, -9))
                {
                    var loopOffset = 0;
                    if (highestJumpVal.Item2 == highestJumpVal.Item1)
                        loopOffset = 1;
                    _subPattern[highestJumpVal.Item1].SetJump(highestJumpVal.Item2 + loopOffset);
                } else if (macros.Count > 0)
                {
                    var macroLen = macros[0].GetMacroData().Count;
                    if (macroLen > 1)
                        _subPattern[macroLen].SetJump(macroLen);
                }
            }

            public byte[] EmitBytes(UgeHeader header)
            {
                List<byte> byteList = new List<byte>();

                switch (_type)
                {
                    case 0x0: // Pulse instrument
                        byteList.AddRange(BitConverter.GetBytes(_type));
                        byteList.AddRange(_name.EmitBytes());
                        byteList.AddRange(BitConverter.GetBytes(_length));
                        byteList.AddRange(BitConverter.GetBytes(_lengthEnabled));
                        byteList.Add(_initialVolume);
                        byteList.AddRange(BitConverter.GetBytes(_volSweepDir <= 0 ? 0x1 : 0x0));
                        byteList.Add(_volSweepSpeed);
                        byteList.AddRange(BitConverter.GetBytes(_freqSweepTime));
                        byteList.AddRange(BitConverter.GetBytes(_freqSweepDirection));
                        byteList.AddRange(BitConverter.GetBytes(_freqSweepShift));
                        byteList.Add(_dutyCycle);
                        byteList.AddRange(BitConverter.GetBytes(_wavetableVolume));
                        byteList.AddRange(BitConverter.GetBytes(_wavetableIndex));

                        if (header.VersionNum < 6)
                            byteList.AddRange(BitConverter.GetBytes((uint)0x0));

                        byteList.AddRange(BitConverter.GetBytes((uint)0x0));

                        if (header.VersionNum < 6)
                        {
                            byteList.AddRange(BitConverter.GetBytes((uint)0x0));
                        }
                        else
                        {
                            byteList.AddRange(BitConverter.GetBytes(_subPatternEnabled));
                            foreach (UgePatternRow subRow in _subPattern)
                                byteList.AddRange(subRow.EmitBytes(header));
                        }

                        if (header.VersionNum >= 4 && header.VersionNum < 6)
                        {
                            foreach (sbyte value in _noiseMacroData)
                            {
                                byteList.Add((byte)0x0);
                            }
                        }
                        break;
                    case 0x1: // Wave instrument
                        byteList.AddRange(BitConverter.GetBytes(_type));
                        byteList.AddRange(_name.EmitBytes());
                        byteList.AddRange(BitConverter.GetBytes(_length));
                        byteList.AddRange(BitConverter.GetBytes(_lengthEnabled));
                        // Unused stuff
                        byteList.Add(0x0);
                        byteList.AddRange(BitConverter.GetBytes((uint)0x0));
                        byteList.Add(0x0);
                        byteList.AddRange(BitConverter.GetBytes((uint)0x0));
                        byteList.AddRange(BitConverter.GetBytes((uint)0x0));
                        byteList.AddRange(BitConverter.GetBytes((uint)0x0));
                        byteList.Add(0x0);

                        byteList.AddRange(BitConverter.GetBytes(_wavetableVolume));
                        byteList.AddRange(BitConverter.GetBytes(_wavetableIndex));

                        if (header.VersionNum < 6)
                            byteList.AddRange(BitConverter.GetBytes((uint)0x0));

                        byteList.AddRange(BitConverter.GetBytes((uint)0x0));

                        if (header.VersionNum < 6)
                        {
                            byteList.AddRange(BitConverter.GetBytes((uint)0x0));
                        }
                        else
                        {
                            byteList.AddRange(BitConverter.GetBytes(_subPatternEnabled));
                            foreach (UgePatternRow subRow in _subPattern)
                                byteList.AddRange(subRow.EmitBytes(header));
                        }

                        if (header.VersionNum >= 4 && header.VersionNum < 6)
                        {
                            foreach (sbyte value in _noiseMacroData)
                            {
                                byteList.Add((byte)0x0);
                            }
                        }
                        break;
                    case 0x2: // Noise instrument
                        byteList.AddRange(BitConverter.GetBytes(_type));
                        byteList.AddRange(_name.EmitBytes());
                        byteList.AddRange(BitConverter.GetBytes(_length));
                        byteList.AddRange(BitConverter.GetBytes(_lengthEnabled));
                        byteList.Add(_initialVolume);
                        byteList.AddRange(BitConverter.GetBytes(_volSweepDir));
                        byteList.Add(_volSweepSpeed);
                        byteList.AddRange(BitConverter.GetBytes((uint)0x0));
                        byteList.AddRange(BitConverter.GetBytes((uint)0x0));
                        byteList.AddRange(BitConverter.GetBytes((uint)0x0));
                        byteList.Add((byte)0x0);
                        byteList.AddRange(BitConverter.GetBytes((uint)0x0));
                        byteList.AddRange(BitConverter.GetBytes((uint)0x0));

                        if (header.VersionNum < 6)
                            byteList.AddRange(BitConverter.GetBytes((uint)0x0));

                        byteList.AddRange(BitConverter.GetBytes(_noiseMode));

                        if (header.VersionNum < 6)
                            byteList.AddRange(BitConverter.GetBytes((uint)0x0));
                        else
                        {
                            byteList.AddRange(BitConverter.GetBytes(_subPatternEnabled));
                            foreach (UgePatternRow subRow in _subPattern)
                                byteList.AddRange(subRow.EmitBytes(header));
                        }

                        if (header.VersionNum >= 4 && header.VersionNum < 6)
                        {
                            foreach (sbyte value in _noiseMacroData)
                            {
                                byteList.Add((byte)value);
                            }
                        }
                        break;
                }

                return byteList.ToArray();
            }

            public void SetDutyCycle(byte value)
            {
                _dutyCycle = value;
            }
        }
    }
}