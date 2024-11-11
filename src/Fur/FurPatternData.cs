


namespace fur2Uge
{
    public class FurPatternData
    {
        private List<FurPatternRowData> _rowData;
        private FurSong _parentSong;
        private FurChannel _channel;
        private int _len;
        private int _index;

        public FurPatternData(string ptnBlockID, FurSong ptnParentSong, FurChannel ptnChannel, int ptnLen, int ptnIndex)
        {
            _parentSong = ptnParentSong;
            _channel = ptnChannel;
            _len = ptnLen;
            _index = ptnIndex;

            _rowData = new List<FurPatternRowData>();
        }

        public void AppendRowData(FurPatternRowData thisRow)
        {
            _rowData.Add(thisRow);
        }

        public List<FurPatternRowData> GetAllRowData()
        {
            return _rowData;
        }

        public int GetLength()
        {
            return _len;
        }
    }
}