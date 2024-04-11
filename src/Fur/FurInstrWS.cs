
namespace fur2Uge
{
    public class FurInstrWS
    {
        private int _wsFirstWave;
        private int _wsSecondWave;
        private byte _wsRateDivider;
        private byte _wsEffect;
        private bool _singleEffect;
        private bool _wsEnabled;
        private bool _wsGlobal;
        private int _wsSpeed;
        private byte _wsParam1;
        private byte _wsParam2;
        private byte _wsParam3;
        private byte _wsParam4;

        public FurInstrWS(int wsFirstWave, int wsSecondWave, byte wsRateDivider, byte wsEffect, bool singleEffect, bool wsEnabled, bool wsGlobal, int wsSpeed, byte wsParam1, byte wsParam2, byte wsParam3, byte wsParam4)
        {
            _wsFirstWave = wsFirstWave;
            _wsSecondWave = wsSecondWave;
            _wsRateDivider = wsRateDivider;
            _wsEffect = wsEffect;
            _singleEffect = singleEffect;
            _wsEnabled = wsEnabled;
            _wsGlobal = wsGlobal;
            _wsSpeed = wsSpeed;
            _wsParam1 = wsParam1;
            _wsParam2 = wsParam2;
            _wsParam3 = wsParam3;
            _wsParam4 = wsParam4;
        }

        public (int, int, byte, byte, bool, bool, bool, int, byte, byte, byte, byte) GetParams()
        {
            return (_wsFirstWave, _wsSecondWave, _wsRateDivider, _wsEffect, _singleEffect, _wsEnabled, _wsGlobal, _wsSpeed, _wsParam1, _wsParam2, _wsParam3, _wsParam4);
        }
    }
}