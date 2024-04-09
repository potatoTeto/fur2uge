namespace Fur2Uge
{
    public partial class FurFile
    {
        public class FurHeader
        {
            private string? _magic = null;  // 16 bytes long
            private int _version = 0x0;
            private int? _reserved = 0x0;
            private int? _songInfoPointer = null;    // 4 bytes long
            private byte[]? _reserved2 = null;  // 8 bytes long

            public FurHeader(string magic, int version, int? reserved, int songInfoPointer, byte[]? reserved2)
            {
                _magic = magic;
                _version = version;
                _reserved = reserved;
                _songInfoPointer = songInfoPointer;
                _reserved2 = reserved2;
            }

            public int GetVersion()
            {
                return _version;
            }
        }
    }
}