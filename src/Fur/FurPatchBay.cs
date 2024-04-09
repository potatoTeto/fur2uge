namespace Fur2Uge
{
    public partial class FurFile
    {
        public struct FurPatchBay
        {
            private byte[] _bytes;

            public FurPatchBay(byte[] bytes)
            {
                _bytes = bytes;
            }
        }
    }
}