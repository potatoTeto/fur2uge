

namespace Fur2Uge
{
    public class FurWavetable
    {
        private string _wtBlockID;
        private int _wtBlockSize;
        private string _wtName;
        private int _wtWidth;
        private int _wtReserved;
        private int _wtHeight;
        private List<int> _wtData;

        public FurWavetable(string wtBlockID, int wtBlockSize, string wtName, int wtWidth, int wtReserved, int wtHeight, List<int> wtData)
        {
            _wtBlockID = wtBlockID;
            _wtBlockSize = wtBlockSize;
            _wtName = wtName;
            _wtWidth = wtWidth;
            _wtReserved = wtReserved;
            _wtHeight = wtHeight;
            _wtData = wtData;
        }

        public byte[] EmitBytes()
        {
            List<byte> bytes = new List<byte>();

            for (int i = 0; i < _wtData.Count; i++)
            {
                bytes.Add((byte)_wtData[i]);
            }

            return bytes.ToArray();
        }
    }
}