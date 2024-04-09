namespace Fur2Uge
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
            private uint _length;
            private bool _lengthEnabled;
            private byte _initialVolume;
            private uint _volSweepDir; // 0 = Increase, 1 = Decrease
            private byte _volSweepSpeed;
            private uint _freqSweepTime;
            private uint _freqSweepDirection; // 1 = Enabled, 0 = Disabled
            private uint _freqSweepShift;
            private byte _dutyCycle;
            private uint _wavetableVolume;
            private uint _wavetableIndex;

            // Write the below if Header Version Number < 6:
            private bool _subPatternEnabled;
            private UgePatternRow[] _subRows;

            // Only use if Version Number >=4 && Version Number < 6
            public sbyte[] _noiseMacroData;

            private uint _noiseMode; // (0 = 15 bit, 1 = 7 bit)

            public UgeInstrument(uint instrumentType)
            {
                _name.Val = string.Empty;
                _type = instrumentType;
                _typeEnum = (UgeInstrumentType)instrumentType;
                _initialVolume = 0xF;
                _volSweepDir = 0x1;
                _freqSweepDirection = 0x1;
                _wavetableVolume = 0x1;
                _noiseMacroData = new sbyte[6];
                _subRows = new UgePatternRow[64];
                for (var i = 0; i < _subRows.Length; i++)
                {
                    _subRows[i] = new UgePatternRow(true);
                }
            }

            public UgeInstrument(string name, uint instrumentType, uint wavetableVolume, uint wavetableIndex, List<FurInstrMacro> macros)
            {
                _name.Val = name;
                _type = instrumentType;
                _typeEnum = (UgeInstrumentType)instrumentType;
                _initialVolume = 0xF;
                _volSweepDir = 0x1;
                _freqSweepDirection = 0x1;
                _wavetableVolume = wavetableVolume;
                _wavetableIndex = wavetableIndex;
                _noiseMacroData = new sbyte[6];
                _subRows = new UgePatternRow[64];

                ParseMacros(macros);

                for (var i = 0; i < _subRows.Length; i++)
                {
                    _subRows[i] = new UgePatternRow(true);
                }
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
                byte wavetableVol = 0x1
                )
            {
                _name.Val = name;
                _type = instrumentType;
                _typeEnum = (UgeInstrumentType)instrumentType;
                _initialVolume = initialVolume;
                _volSweepDir = volSweepDir;
                _volSweepSpeed = volSweepSpeed;
                _length = length;

                ParseMacros(macros);

                if (_length < 64)
                    _lengthEnabled = true;

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
                _subRows = new UgePatternRow[64];
                for (var i = 0; i < _subRows.Length; i++)
                {
                    _subRows[i] = new UgePatternRow(true);
                }
            }

            private void ParseMacros(List<FurInstrMacro> macros)
            {
                foreach (FurInstrMacro m in macros)
                {
                    var code = m.GetMacroCode();
                    var data = m.GetMacroData();
                    //var type = m.GetMacroType();

                    switch (code)
                    {
                        case FurFile.FurInstrMacroCode.VOL:
                            _initialVolume = (byte)data[0];
                            break;
                        case FurFile.FurInstrMacroCode.DUTY:
                            _dutyCycle = (byte)data[0];
                            break;
                    }
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
                        byteList.AddRange(BitConverter.GetBytes(_volSweepDir));
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
                            foreach (UgePatternRow subRow in _subRows)
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
                            foreach (UgePatternRow subRow in _subRows)
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
                            foreach (UgePatternRow subRow in _subRows)
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
        }
    }
}