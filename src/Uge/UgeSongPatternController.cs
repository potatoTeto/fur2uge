
namespace fur2Uge
{
    public class UgeSongPatternController
    {
        private uint _initialTicksPerRow;
        private bool _timerBasedTempoEnabled;
        private uint _timerBasedTempoDivider;
        private UgeFile.UgeHeader _header;
        private Dictionary<int, UgeSongPattern> _patterns;

        public UgeSongPatternController(byte initialTicksPerRow, byte numOrderRows, UgeFile.UgeHeader header, bool isSubpattern)
        {
            _initialTicksPerRow = initialTicksPerRow;

            _header = header;
            _timerBasedTempoEnabled = false;
            _timerBasedTempoDivider = 0;

            _patterns = new Dictionary<int, UgeSongPattern>();
        }

        public void AppendSongPattern(int patternIndex)
        {
            _patterns[patternIndex] = new UgeSongPattern((uint)patternIndex, false);
        }

        public void SetInitialTicksPerRow(uint value)
        {
            _initialTicksPerRow = value;
        }

        public void SetTimerBasedTempoDivider(uint divider)
        {
            _timerBasedTempoDivider = divider;
        }

        public void SetTimerEnabled(bool enabled)
        {
            _timerBasedTempoEnabled = enabled;
        }


        public byte[] EmitBytes(UgeFile.UgeHeader header)
        {
            List<byte> byteList = new List<byte>();
            byteList.AddRange(BitConverter.GetBytes(_initialTicksPerRow));
            if (header.VersionNum >= 6)
            {
                //byteList.Add(_timerBasedTempoEnabled ? (byte)1 : (byte)0);
                byteList.AddRange(BitConverter.GetBytes(_timerBasedTempoEnabled));
                byteList.AddRange(BitConverter.GetBytes(_timerBasedTempoDivider));
            }

            byteList.AddRange(BitConverter.GetBytes((uint)_patterns.Count));

            foreach (UgeSongPattern pattern in _patterns.Values)
            {
                byteList.AddRange(pattern.EmitBytes(header));
            }

            return byteList.ToArray();
        }

        public void SetEffect(GBChannel channel, byte orderPosition, int row, UgeEffectTable effect, byte effectData)
        {
            try
            {
                _patterns[orderPosition].SetEffect(row, effect, effectData);
            }
            catch (KeyNotFoundException)
            {
                throw new Exception(string.Format("Error: SetEffect({0}, {1}, {2}). Please file an issue to the developer on the fur2uge GitHub: This should not happen!", row, effect, effectData));
            }
        }

        public void SetNote(GBChannel channel, byte orderPosition, int row, UgeNoteTable note)
        {
            if ((int)note == 180)
                SetEffect(channel, orderPosition, row, UgeEffectTable.NOTE_CUT, 0x0);
            else
            {
                if (channel == GBChannel.NOISE)
                    note += 19; // Remap the pitch to be 1:1 with (or at least closer to) Furnace's noise freq table
                try
                {
                    _patterns[orderPosition].SetNote(row, note);
                }
                catch (KeyNotFoundException)
                {
                    throw new Exception(string.Format("Error: SetNote({0}, {1}). Please file an issue to the developer on the fur2uge GitHub: This should not happen!", row, note));
                }
            }
        }

        public void SetInstrument(GBChannel channel, byte orderPosition, int row, int instrumentIndex)
        {
            try
            {
                _patterns[orderPosition].SetInstrument(row, instrumentIndex);
            }
            catch (KeyNotFoundException)
            {
                throw new Exception(string.Format("Error: SetInstrument({0}, {1}). Please file an issue to the developer on the fur2uge GitHub: This should not happen!", row, instrumentIndex));
            }
        }
    }
}