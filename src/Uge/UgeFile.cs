using System.Text;
using System.Threading.Channels;

namespace Fur2Uge
{
    public enum UgeNoteTable
    {
        CN3, CS3, DN3, DS3, EN3, FN3, FS3, GN3, GS3, AN3, AS3, BN3,
        CN4, CS4, DN4, DS4, EN4, FN4, FS4, GN4, GS4, AN4, AS4, BN4,
        CN5, CS5, DN5, DS5, EN5, FN5, FS5, GN5, GS5, AN5, AS5, BN5,
        CN6, CS6, DN6, DS6, EN6, FN6, FS6, GN6, GS6, AN6, AS6, BN6,
        CN7, CS7, DN7, DS7, EN7, FN7, FS7, GN7, GS7, AN7, AS7, BN7,
        CN8, CS8, DN8, DS8, EN8, FN8, FS8, GN8, GS8, AN8, AS8, BN8,
        BLANK = 0x5A, MAX
    };

    public enum UgeEffectTable
    {
        ARPEGGIO = 0x0,
        PORTAMENTO_UP = 0x1,
        PORTAMENTO_DOWN = 0x2,
        TONE_PORTAMENTO = 0x3,
        VIBRATO = 0x4,
        SET_MASTER_VOL = 0x5,
        CALL_ROUTINE = 0x6,
        NOTE_DELAY = 0x7,
        SET_PANNING = 0x8,
        SET_DUTY_CYCLE = 0x9,
        VOLUME_SLIDE = 0xA,
        POSITION_JUMP = 0xB,
        SET_VOL = 0xC,
        PATTERN_BREAK = 0xD,
        NOTE_CUT = 0xE,
        SET_SPEED = 0xF,
        MAX
    };

    public enum GBChannel
    {
        PULSE_1,
        PULSE_2,
        WAVETABLE,
        NOISE
    };

    public partial class UgeFile
    {
        private List<byte> outData;
        private UgeInstrument[] _ugeDutyInstruments;
        private UgeInstrument[] _ugeWaveInstruments;
        private UgeInstrument[] _ugeNoiseInstruments;
        private UgeWavetable[] _ugeWavetables;
        private UgeSongPatternController _ugeSongPatternController;
        private UgeSongOrderManager _ugeSongOrderManager;
        private UgeRoutine[] _ugeRoutines;

        public struct shortstring
        {
            public byte ReadableLen;
            public string Val = string.Empty;

            public shortstring(string val, byte readableLen = 60)
            {
                Val = val;
                ReadableLen = readableLen;
            }

            public string? Trim(string inStr)
            {
                var str = inStr;
                if (inStr != null)
                {
                    if (inStr.Length > 255)
                        str = Val.Substring(0, 254);
                }
                else
                    str = string.Empty;

                if (str.Length <= 0)
                {
                    return null;
                }
                else
                {
                    while (str.Length < 255)
                    {
                        str += ' ';
                    }
                }
                return str;
            }

            public byte[] EmitBytes()
            {
                List<byte> byteList = new List<byte>();
                var str = Trim(Val);

                if (str != null)
                {
                    ReadableLen = (byte)Math.Clamp(str.Trim().Length, 0, 255);
                    byteList.Add(ReadableLen);
                    byteList.AddRange(Encoding.UTF8.GetBytes(str));
                }
                else
                {
                    byteList.Add(0x5);
                    byteList.AddRange(Encoding.UTF8.GetBytes("Blank"));
                    for (var i = 0; i < 250; i++)
                        byteList.Add((byte)0x0);
                }
                return byteList.ToArray();
            }
        }

        public struct UgeHeader
        {
            public uint VersionNum;
            public shortstring SongName;
            public shortstring SongArtist;
            public shortstring SongComment;

            public byte[] EmitBytes()
            {
                List<byte> byteList = new List<byte>();
                byteList.AddRange(BitConverter.GetBytes(VersionNum));
                byteList.AddRange(SongName.EmitBytes());
                byteList.AddRange(SongArtist.EmitBytes());
                byteList.AddRange(SongComment.EmitBytes());
                return byteList.ToArray();
            }
        }

        public UgeHeader Header;

        public UgeFile()
        {
            Header.VersionNum = 6;
            Header.SongName.Val = string.Empty;
            Header.SongArtist.Val = string.Empty;
            Header.SongComment.Val = string.Empty;

            _ugeDutyInstruments = new UgeInstrument[15];
            _ugeWaveInstruments = new UgeInstrument[15];
            _ugeNoiseInstruments = new UgeInstrument[15];
            for (int i = 0; i < _ugeDutyInstruments.Length; i++)
            {
                _ugeDutyInstruments[i] = new UgeInstrument(0);
                _ugeWaveInstruments[i] = new UgeInstrument(1);
                _ugeNoiseInstruments[i] = new UgeInstrument(2);
            }

            _ugeWavetables = new UgeWavetable[16];
            for (int i = 0; i < _ugeWavetables.Length; i++)
            {
                _ugeWavetables[i] = new UgeWavetable();
            }

            _ugeSongPatternController = new UgeSongPatternController(7, 1, Header, false);
            _ugeSongOrderManager = new UgeSongOrderManager();

            _ugeRoutines = new UgeRoutine[16];
            for (int i = 0; i < _ugeRoutines.Length; i++)
            {
                _ugeRoutines[i] = new UgeRoutine();
            }
        }

        public void SetWavetable(int index, byte[] data)
        {
            _ugeWavetables[index] = new UgeWavetable(data);
        }

        public UgeSongPatternController GetUgeSongPatternController()
        {
            return _ugeSongPatternController;
        }

        public void Write(string fOut)
        {
            outData = new List<byte>();
            outData.AddRange(Header.EmitBytes());
            foreach (UgeInstrument dutyInsts in _ugeDutyInstruments)
                outData.AddRange(dutyInsts.EmitBytes(Header));
            foreach (UgeInstrument waveInsts in _ugeWaveInstruments)
                outData.AddRange(waveInsts.EmitBytes(Header));
            foreach (UgeInstrument noiseInsts in _ugeNoiseInstruments)
                outData.AddRange(noiseInsts.EmitBytes(Header));
            foreach (UgeWavetable wavetable in _ugeWavetables)
                outData.AddRange(wavetable.EmitBytes(Header));

            outData.AddRange(_ugeSongPatternController.EmitBytes(Header));
            
            outData.AddRange(_ugeSongOrderManager.EmitBytes(Header));

            foreach (UgeRoutine routine in _ugeRoutines)
                outData.AddRange(routine.EmitBytes(Header));

            File.WriteAllBytes(fOut, outData.ToArray());
        }

        public void SetSpeed(int value)
        {
            _ugeSongPatternController.SetInitialTicksPerRow((uint)value);
        }

        public void SetPulseInstr(int instrIndex, UgeInstrument ugePulse)
        {
            try
            {
                _ugeDutyInstruments[instrIndex] = ugePulse;
            }
            catch (IndexOutOfRangeException)
            {
                throw new Exception("You have too many Pulse instruments! Limit is 15; Aborting...");
            }
        }

        public void SetNoiseInstr(int instrIndex, UgeInstrument ugeNoise)
        {
            try
            {
                _ugeNoiseInstruments[instrIndex] = ugeNoise;
            }
            catch (IndexOutOfRangeException)
            {
                throw new Exception("You have too many Noise instruments! Limit is 15; Aborting...");
            }
        }

        public void SetWaveInstr(int instrIndex, UgeInstrument ugeWave)
        {
            try
            {
                _ugeWaveInstruments[instrIndex] = ugeWave;
            }
            catch (IndexOutOfRangeException)
            {
                throw new Exception("You have too many Wave instruments! Limit is 15; Aborting...");
            }
        }

        public void SetOrderTable(int[,] ugeOrderTable)
        {
            _ugeSongOrderManager.SetOrderTable(ugeOrderTable);
        }

        public void AppendSongPattern(int ugeOrderID)
        {
            _ugeSongPatternController.AppendSongPattern(ugeOrderID);
        }
    }
}