
namespace Fur2Uge
{
    public class FurInstrFM
    {
        private bool _op0Enabled;
        private bool _op2Enabled;
        private bool _op1Enabled;
        private bool _op3Enabled;
        private byte _opCount;

        private byte _baseData1;
        private byte _baseData2;
        private byte _baseData3;

        private List<FurOpData> _furOPData;

        public FurInstrFM(byte flags, byte opCount, byte baseData1, byte baseData2, byte baseData3, List<FurOpData> furOPData)
        {
            // op order from 4 to 7: 0, 2, 1, 3
            // 2-op instruments: 0, 1, x, y
            _op0Enabled = FurFile.GetBit(flags, 4);        // Op0 Enabled  |   2-op 0 Enabled
            _op2Enabled = FurFile.GetBit(flags, 5);        // Op2 Enabled  |   2-op 1 Enabled
            _op1Enabled = FurFile.GetBit(flags, 6);        // Op1 Enabled  |   2-op x
            _op3Enabled = FurFile.GetBit(flags, 7);        // Op3 Enabled  |   2-op y

            _opCount = opCount;
            _baseData1 = baseData1;
            _baseData2 = baseData2;
            _baseData3 = baseData3;
            _furOPData = furOPData;
        }
    }
}