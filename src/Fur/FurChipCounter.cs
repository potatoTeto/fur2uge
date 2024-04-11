
namespace fur2Uge
{
    public partial class FurFile
    {
        public struct FurChipCounter
        {
            public FurChipType Chip = FurChipType.END_OF_LIST;
            public int ChanCount = 0;

            public FurChipCounter(FurChipType _chip, int _chanCount)
            {
                Chip = _chip;
                ChanCount = _chanCount;
            }
        }
    }
}