namespace Fur2Uge
{
    public class UgeSongOrderManager
    {
        public byte Id;
        private List<uint> _orders;

        public UgeSongOrderManager(byte id)
        {
            Id = id;
            _orders = new List<uint>();
        }

        public void AddOrder(byte index)
        {
            _orders.Add(index);
        }

        public void ClearOrders()
        {
            _orders.Clear();
        }

        public byte[] EmitBytes(UgeFile.UgeHeader header)
        {
            List<byte> byteList = new List<byte>();

            byteList.AddRange(BitConverter.GetBytes((uint)(_orders.Count + 1))); // Off by one to account for hUGETracker workaround
            foreach (uint orderIndex in _orders)
            {
                byteList.AddRange(BitConverter.GetBytes(orderIndex));
            }
            byteList.AddRange(BitConverter.GetBytes((uint)0x0)); // Filler bytes to account for hUGETracker bug.
            return byteList.ToArray();
        }

        public int GetOrderCount()
        {
            return _orders.Count;
        }
    }
}