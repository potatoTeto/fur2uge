

namespace fur2Uge
{
    public class FurChannel
    {
        private FurFile.FurChipType _chip;
        private int _chanID;
        private Dictionary<int, FurPatternData> _patterns;

        public FurChannel(FurFile.FurChipType chip, int chanID)
        {
            _chip = chip;
            _chanID = chanID;
            _patterns = new Dictionary<int, FurPatternData>();
        }

        public void AddPattern(int patternID, FurPatternData thisPattern)
        {
            _patterns.Add(patternID, thisPattern);
        }

        public FurPatternData GetPattern(int orderID)
        {
            return _patterns[orderID];
        }
    }
}