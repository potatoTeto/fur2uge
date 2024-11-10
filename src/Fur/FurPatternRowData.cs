




namespace fur2Uge
{
    public class FurPatternRowData
    {
        private int _rowIndex;
        private int _rowNoteVal;
        private string _rowNoteName;
        private int _rowInstrVal;
        private int _rowVolVal;
        private FurPatternRowDataEffectCell[] _effectData = new FurPatternRowDataEffectCell[8];

        public void SetData(int rowIndex, int rowNoteVal, string rowNoteName, int rowInstrVal, int rowVolVal,
            int rowFX0Present, int rowFX0ValPresent, int rowFX0, int rowFX0Val,
            int rowFX1Present, int rowFX1ValPresent, int rowFX1, int rowFX1Val,
            int rowFX2Present, int rowFX2ValPresent, int rowFX2, int rowFX2Val,
            int rowFX3Present, int rowFX3ValPresent, int rowFX3, int rowFX3Val,
            int rowFX4Present, int rowFX4ValPresent, int rowFX4, int rowFX4Val,
            int rowFX5Present, int rowFX5ValPresent, int rowFX5, int rowFX5Val,
            int rowFX6Present, int rowFX6ValPresent, int rowFX6, int rowFX6Val,
            int rowFX7Present, int rowFX7ValPresent, int rowFX7, int rowFX7Val)
        {
            _rowIndex = rowIndex;
            _rowNoteVal = rowNoteVal;
            _rowNoteName = rowNoteName;
            _rowInstrVal = rowInstrVal;
            _rowVolVal = rowVolVal;

            _effectData[0] = new FurPatternRowDataEffectCell(rowFX0Present > 0, rowFX0ValPresent > 0, rowFX0, rowFX0Val);
            _effectData[1] = new FurPatternRowDataEffectCell(rowFX1Present > 0, rowFX1ValPresent > 0, rowFX1, rowFX1Val);
            _effectData[2] = new FurPatternRowDataEffectCell(rowFX2Present > 0, rowFX2ValPresent > 0, rowFX2, rowFX2Val);
            _effectData[3] = new FurPatternRowDataEffectCell(rowFX3Present > 0, rowFX3ValPresent > 0, rowFX3, rowFX3Val);
            _effectData[4] = new FurPatternRowDataEffectCell(rowFX4Present > 0, rowFX4ValPresent > 0, rowFX4, rowFX4Val);
            _effectData[5] = new FurPatternRowDataEffectCell(rowFX5Present > 0, rowFX5ValPresent > 0, rowFX5, rowFX5Val);
            _effectData[6] = new FurPatternRowDataEffectCell(rowFX6Present > 0, rowFX6ValPresent > 0, rowFX6, rowFX6Val);
            _effectData[7] = new FurPatternRowDataEffectCell(rowFX7Present > 0, rowFX7ValPresent > 0, rowFX7, rowFX7Val);
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

        public FurPatternRowDataEffectCell[] GetEffectData()
        {
            return _effectData;
        }
    }
}