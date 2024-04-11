namespace fur2Uge
{
    public class UgeSongOrderManager
    {
        private int[,]? _orders = null;

        public void SetOrderTable(int[,] orders)
        {
            _orders = orders;
        }

        public byte[] EmitBytes(UgeFile.UgeHeader header)
        {
            List<byte> byteList = new List<byte>();

            int orderTableHeight = _orders.GetLength(1);

            for (var chanID = 0; chanID < 4; chanID++)
            {
                byteList.AddRange(BitConverter.GetBytes((uint)(orderTableHeight + 1))); // The height of the order table, but off by one to account for hUGETracker workaround
                for (int orderRow = 0; orderRow < orderTableHeight; orderRow++)
                {
                    uint orderIndex = (uint)_orders[chanID, orderRow];
                    byteList.AddRange(BitConverter.GetBytes(orderIndex));
                }
                byteList.AddRange(BitConverter.GetBytes((uint)0x0)); // Filler bytes to account for hUGETracker bug.
            }
            return byteList.ToArray();
        }
    }
}