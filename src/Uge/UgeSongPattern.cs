using static fur2Uge.UgeFile;

namespace fur2Uge
{
    public class UgeSongPattern
    {
        uint _patternIndex;
        UgePatternRow[] ugePatternRows;

        public UgeSongPattern(uint patternIndex, bool isSubpattern)
        {
            _patternIndex = patternIndex;
            ugePatternRows = new UgePatternRow[64];

            for (var i = 0; i < ugePatternRows.Length; i++)
            {
                ugePatternRows[i] = new UgePatternRow(isSubpattern);
            }
        }

        public byte[] EmitBytes(UgeHeader header)
        {
            List<byte> byteList = new List<byte>();
            byteList.AddRange(BitConverter.GetBytes(_patternIndex));
            foreach (UgePatternRow thisRow in ugePatternRows)
            {
                byteList.AddRange(thisRow.EmitBytes(header));
            }
            return byteList.ToArray();
        }

        public void SetNote(int row, UgeNoteTable note)
        {
            ugePatternRows[row].SetNote(note);
        }

        public void SetEffect(int row, UgeEffectTable effect, byte effectData)
        {
            ugePatternRows[row].SetEffect(effect, effectData);
        }

        public void SetInstrument(int row, int instrumentIndex)
        {
            ugePatternRows[row].SetInstrument(instrumentIndex);
        }
    }
}