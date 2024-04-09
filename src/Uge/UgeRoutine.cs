
using System.Text;

namespace Fur2Uge
{
    public class UgeRoutine
    {
        private string data = string.Empty;

        public UgeRoutine()
        {
        }

        public void SetData(string str)
        {
            data = str;
        }

        public byte[] EmitBytes(UgeFile.UgeHeader header)
        {
            List<byte> byteList = new List<byte>();
            byteList.AddRange(BitConverter.GetBytes((uint)data.Length));
            byteList.AddRange(Encoding.UTF8.GetBytes(data));
            return byteList.ToArray();
        }
    }
}