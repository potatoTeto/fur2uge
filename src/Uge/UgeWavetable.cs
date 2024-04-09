
namespace Fur2Uge
{
    public class UgeWavetable
    {
        byte[] _data;

        public UgeWavetable(byte[]? data = null)
        {
            if (data == null)
            {
                _data = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0,
                                0, 0, 0, 0, 0, 0, 0, 0,
                                0, 0, 0, 0, 0, 0, 0, 0,
                                0, 0, 0, 0, 0, 0, 0, 0}; // 32 blocks of data
            }
            else
            {
                _data = new byte[data.Length];

                // Mask out the upper nybble before passing the data.
                // This is done to prevent garbage data
                // from crashing hUGETracker
                for (var i = 0; i < data.Length; i++)
                {
                    byte b = data[i];
                    _data[i] = (byte)(b & 0x0F);
                }
            }
        }

        public byte[] EmitBytes(UgeFile.UgeHeader header)
        {
            List<byte> byteList = new List<byte>();

            byteList.AddRange(_data);

            if (header.VersionNum < 3)
                byteList.Add((byte)0x0);

            return byteList.ToArray();
        }
    }
}