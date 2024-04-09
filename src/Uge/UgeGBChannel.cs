namespace Fur2Uge
{
    public class UgeGBChannel
    {
        private byte _chanID;
        private UgeSongOrderManager _orderManager;
        private List<UgeSongPattern> _patterns;

        public UgeGBChannel(byte chanID)
        {
            _chanID = chanID;
            _patterns = new List<UgeSongPattern>();
            _orderManager = new UgeSongOrderManager(chanID);
        }

        public void AddNewOrderRow()
        {
            _orderManager.AddOrder((byte)(_orderManager.GetOrderCount() * 4 + _chanID));
        }

        public UgeSongOrderManager GetSongOrderManager()
        {
            return _orderManager;
        }

        public void AddNewPattern(byte numPatterns, bool isSubpattern)
        {
            for (byte i = 0; i < numPatterns; i++)
            {
                var thisPattern = new UgeSongPattern((uint)_patterns.Count * 4 + _chanID, isSubpattern);
                //AddNewOrderRow();
                _patterns.Add(thisPattern);
            }
        }

        public uint GetPatternCount()
        {
            return (uint)_patterns.Count;
        }

        public byte[] EmitAllPatternBytes(UgeFile.UgeHeader header)
        {
            List<byte> byteList = new List<byte>();

            foreach (UgeSongPattern pattern in _patterns)
                byteList.AddRange(pattern.EmitBytes(header));

            return byteList.ToArray();
        }

        public List<UgeSongPattern> GetPatterns()
        {
            return _patterns;
        }

        public byte[] EmitPatternBytes(UgeFile.UgeHeader header, int patternIndex)
        {
            List<byte> byteList = new List<byte>();
            UgeSongPattern pattern = _patterns[patternIndex];
            byteList.AddRange(pattern.EmitBytes(header));
            return byteList.ToArray();
        }
    }
}