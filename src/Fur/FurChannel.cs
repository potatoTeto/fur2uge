

namespace fur2Uge
{
    public class FurChannel
    {
        private FurFile.FurChipType _chip;
        private int _chanID;
        private List<FurPatternData> _patterns;

        public FurChannel(FurFile.FurChipType chip, int chanID)
        {
            _chip = chip;
            _chanID = chanID;
            _patterns = new List<FurPatternData>();
        }

        public void AddPattern(FurPatternData thisPattern)
        {
            _patterns.Add(thisPattern);
        }

        public FurPatternData GetPattern(int orderID)
        {
            return _patterns[orderID];
        }
    }
}