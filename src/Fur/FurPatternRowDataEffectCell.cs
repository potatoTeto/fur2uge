






namespace fur2Uge
{
    public class FurPatternRowDataEffectCell {
        private readonly bool _rowFXPresent;
        private readonly bool _rowFxValPresent;
        private readonly int _rowFXData;
        private readonly int _rowFXValData;

        public FurPatternRowDataEffectCell(bool rowFXPresent, bool rowFxValPresent, int rowFXData, int rowFXValData)
        {
            _rowFXPresent = rowFXPresent;
            _rowFxValPresent = rowFxValPresent;
            _rowFXData = rowFXData;
            _rowFXValData = rowFXValData;
        }

        internal byte GetCommand()
        {
            return (byte)_rowFXData;
        }

        internal bool GetCommandIsPresent()
        {
            return _rowFXPresent;
        }

        internal byte GetValue()
        {
            if (_rowFXValData > -9999 && _rowFxValPresent)
                return (byte)_rowFXValData;
            else
                return 0;
        }

        internal bool GetValueIsPresent()
        {
            return _rowFxValPresent;
        }
    }
}