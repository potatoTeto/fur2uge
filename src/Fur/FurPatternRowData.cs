




namespace fur2Uge
{
    public class FurPatternRowData
    {
        private int _rowIndex;
        private int _rowNoteVal;
        private string _rowNoteName;
        private int _rowInstrVal;
        private int _rowVolVal;
        private int _rowFX0;
        private int _rowFX0Val;
        private int _rowFX1;
        private int _rowFX1Val;
        private int _rowFX2;
        private int _rowFX2Val;
        private int _rowFX3;
        private int _rowFX3Val;
        private int _rowFX4;
        private int _rowFX4Val;
        private int _rowFX5;
        private int _rowFX5Val;
        private int _rowFX6;
        private int _rowFX6Val;
        private int _rowFX7;
        private int _rowFX7Val;

        public FurPatternRowData()
        {
        }

        public void SetData(int rowIndex, int rowNoteVal, string rowNoteName, int rowInstrVal, int rowVolVal, int rowFX0, int rowFX0Val, int rowFX1, int rowFX1Val, int rowFX2, int rowFX2Val, int rowFX3, int rowFX3Val, int rowFX4, int rowFX4Val, int rowFX5, int rowFX5Val, int rowFX6, int rowFX6Val, int rowFX7, int rowFX7Val)
        {
            _rowIndex = rowIndex;
            _rowNoteVal = rowNoteVal;
            _rowNoteName = rowNoteName;
            _rowInstrVal = rowInstrVal;
            _rowVolVal = rowVolVal;
            _rowFX0 = rowFX0;
            _rowFX0Val = rowFX0Val;
            _rowFX1 = rowFX1;
            _rowFX1Val = rowFX1Val;
            _rowFX2 = rowFX2;
            _rowFX2Val = rowFX2Val;
            _rowFX3 = rowFX3;
            _rowFX3Val = rowFX3Val;
            _rowFX4 = rowFX4;
            _rowFX4Val = rowFX4Val;
            _rowFX5 = rowFX5;
            _rowFX5Val = rowFX5Val;
            _rowFX6 = rowFX6;
            _rowFX6Val = rowFX6Val;
            _rowFX7 = rowFX7;
            _rowFX7Val = rowFX7Val;
        }

        public int GetNoteVal()
        {
            return _rowNoteVal;
        }

        public int GetRowIndex()
        {
            return _rowIndex;
        }

        public int GetInstrumentVal()
        {
            return _rowInstrVal;
        }

        public int GetVolume()
        {
            return _rowVolVal;
        }

        public List<byte> GetEffectData()
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(new byte[]{
                (byte)_rowFX0, (byte)_rowFX0Val,
                (byte)_rowFX1, (byte)_rowFX1Val,
                (byte)_rowFX2, (byte)_rowFX2Val,
                (byte)_rowFX3, (byte)_rowFX3Val,
                (byte)_rowFX4, (byte)_rowFX4Val,
                (byte)_rowFX5, (byte)_rowFX5Val,
                (byte)_rowFX6, (byte)_rowFX6Val,
                (byte)_rowFX7, (byte)_rowFX7Val});
            return bytes;
        }
    }
}