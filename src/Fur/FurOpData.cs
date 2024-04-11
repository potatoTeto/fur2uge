namespace fur2Uge
{
    public class FurOpData
    {
        private byte _opData1;
        private byte _opData2;
        private byte _opData3;
        private byte _opData4;
        private byte _opData5;
        private byte _opData6;
        private byte _opData7;
        private byte _opData8;
        private byte[] opData;

        public FurOpData(byte[] opData)
        {
            _opData1 = opData[0];
            _opData2 = opData[1];
            _opData3 = opData[2];
            _opData4 = opData[3];
            _opData5 = opData[4];
            _opData6 = opData[5];
            _opData7 = opData[6];
            _opData8 = opData[7];
        }
    }
}