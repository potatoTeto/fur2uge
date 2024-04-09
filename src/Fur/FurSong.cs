

namespace Fur2Uge
{
    public class FurSong
    {
        public int TimeBase;
        public int Speed1;
        public int Speed2;
        public int InitialArpTime;
        public float TicksPerSecond;
        public int PatternLen;
        public int OrdersLen;
        public int HighlightA;
        public int HighlightB;

        public int[,]? OrderTable;

        public Dictionary<int, int> EffectColumnCounts = new Dictionary<int, int>();
        public Dictionary<int, string> ChanNames = new Dictionary<int, string>();
        public Dictionary<int, string> ChanShortNames = new Dictionary<int, string>();
        public Dictionary<int, bool> ChanHideStatus = new Dictionary<int, bool>();
        public Dictionary<int, bool> ChanCollapseStatus = new Dictionary<int, bool>();

        public int VirtualTempoNumerator;
        public int VirtualTempoDenominator;

        public int SpeedPatternLen;
        public int[] SpeedPattern;

        public List<FurChannel> Channels = new List<FurChannel>();

        public void AddChanCollapseStatus(int chanID, byte channelCollapseStatus)
        {
            ChanCollapseStatus.Add(chanID, channelCollapseStatus > 0x0);
        }

        public void AddChanHideStatus(int chanID, byte channelHideStatus)
        {
            ChanHideStatus.Add(chanID, channelHideStatus > 0x0);
        }

        public void AddChanName(int chanID, string chanName)
        {
            ChanNames.Add(chanID, chanName);
        }

        public void AddChanShortName(int chanID, string chanName)
        {
            ChanShortNames.Add(chanID, chanName);
        }

        public void AddEffectColumnCount(int chanID, byte effectColumnCount)
        {
            EffectColumnCounts.Add(chanID, effectColumnCount);
        }

        public void InitDataA(byte firstTimeBase, byte firstSpeed1, byte firstSpeed2, byte firstInitialArpTime, float firstTicksPerSecond, ushort firstPatternLen, ushort firstOrdersLen, byte firstHighlightA, byte firstHighlightB)
        {
            TimeBase = firstTimeBase;
            Speed1 = firstSpeed1;
            Speed2 = firstSpeed2;
            InitialArpTime = firstInitialArpTime;
            TicksPerSecond = firstTicksPerSecond;
            PatternLen = firstPatternLen;
            OrdersLen = firstOrdersLen;
            HighlightA = firstHighlightA;
            HighlightB = firstHighlightB;
        }

        public void PopulateOrderTable(int[,] orderTable)
        {
            OrderTable = orderTable;
        }

        public void SetVirtualTempo(ushort vtNum, ushort vtDenom)
        {
            VirtualTempoNumerator = vtNum;
            VirtualTempoDenominator = vtDenom;
        }

        public void SetSpeedPattern(byte speedPatternLen, byte[] speedPattern)
        {
            SpeedPatternLen = speedPatternLen;
            SpeedPattern = new int[speedPattern.Length];
            for (var i = 0; i < speedPattern.Length; i++)
            {
                SpeedPattern[i] = speedPattern[i];
            }
        }

        public void AddChan(FurFile.FurChipType chip, int chanID)
        {
            FurChannel thisChannel = new FurChannel(chip, chanID);
            Channels.Add(thisChannel);
        }

        public FurChannel GetChannel(int chanID)
        {
            return Channels[chanID];
        }
    }
}