namespace Fur2Uge
{
    public class FurGrooveEntry
    {
        private byte _grooveLen;
        private byte[] _groovePattern;

        public FurGrooveEntry(byte grooveLen, byte[] groovePattern)
        {
            _grooveLen = grooveLen;
            _groovePattern = groovePattern;
        }
    }
}