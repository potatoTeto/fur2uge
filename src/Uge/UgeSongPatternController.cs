namespace Fur2Uge
{
    public class UgeSongPatternController
    {
        private uint _initialTicksPerRow;
        private bool _timerBasedTempoEnabled;
        private uint _timerBasedTempoDivider;
        private UgeFile.UgeHeader _header;
        private UgeGBChannel[] _gbChannels;

        public UgeSongPatternController(byte initialTicksPerRow, byte numOrderRows, UgeFile.UgeHeader header, bool isSubpattern, UgeGBChannel[] gbChannels)
        {
            _initialTicksPerRow = initialTicksPerRow;

            _gbChannels = gbChannels;

            foreach (var channel in _gbChannels)
            {
                channel.AddNewPattern(numOrderRows, isSubpattern);
            }

            _header = header;
            _timerBasedTempoEnabled = false;
            _timerBasedTempoDivider = 0;
        }

        public void SetInitialTicksPerRow(uint value)
        {
            _initialTicksPerRow = value;
        }

        public byte[] EmitPatternHeaderBytes(UgeFile.UgeHeader header)
        {
            List<byte> byteList = new List<byte>();
            byteList.AddRange(BitConverter.GetBytes(_initialTicksPerRow));
            if (header.VersionNum >= 6)
            {
                //byteList.Add(_timerBasedTempoEnabled ? (byte)1 : (byte)0);
                byteList.AddRange(BitConverter.GetBytes(_timerBasedTempoEnabled));
                byteList.AddRange(BitConverter.GetBytes(_timerBasedTempoDivider));
            }
            return byteList.ToArray();
        }

        public void SetEffect(GBChannel channel, byte orderPosition, int row, UgeEffectTable effect, byte effectData)
        {
            _gbChannels[(int)channel].GetPatterns()[orderPosition].SetEffect(row, effect, effectData);
        }

        public void SetNote(GBChannel channel, byte orderPosition, int row, UgeNoteTable note)
        {
            if ((int)note == 180)
                SetEffect(channel, orderPosition, row, UgeEffectTable.NOTE_CUT, 0x0);
            else
            {
                if (channel == GBChannel.NOISE)
                    note += 19; // Remap the pitch to be 1:1 with (or at least closer to) Furnace's noise freq table
                _gbChannels[(int)channel].GetPatterns()[orderPosition].SetNote(row, note);
            }
        }

        public void SetInstrument(GBChannel channel, byte orderPosition, int row, int instrumentIndex)
        {
            _gbChannels[(int)channel].GetPatterns()[orderPosition].SetInstrument(row, instrumentIndex);
        }
        /*
         
         
        public void AddNewPatternRow(bool isSubpattern)
        {
            var i = 0;
            foreach(var channel in _gbChannels)
            {
                var _patterns = channel.GetPatterns();
                var thisPattern = new UgeSongPattern((uint)(_patterns.Count + i), isSubpattern);
                _patterns.Add(thisPattern);
                i++;
            }
        }
         */
    }
}