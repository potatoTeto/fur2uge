namespace fur2Uge
{
    public partial class UgeFile
    {
        public partial class UgePatternRow
        {
            private bool _isSubpattern;
            public uint RowNote; // 0-72; 90 if not used
            public uint InstrumentValue; // Unused in subpatterns; 0 if not used
            public uint JumpCmdVal; // 0 if empty. Used in Subpatterns; Not used in Main Patterns if Version Number >= 6
            public uint EffectCode;
            public byte EffectParam;

            public UgePatternRow(bool isSubpattern)
            {
                _isSubpattern = isSubpattern;
                RowNote = 0x5A;
            }

            public byte[] EmitBytes(UgeHeader header)
            {
                List<byte> byteList = new List<byte>();
                byteList.AddRange(BitConverter.GetBytes(RowNote));
                if (!_isSubpattern)
                {
                    byteList.AddRange(BitConverter.GetBytes(InstrumentValue));
                    if (header.VersionNum >= 6)
                        byteList.AddRange(BitConverter.GetBytes((uint)0x0));
                }
                else
                {
                    byteList.AddRange(BitConverter.GetBytes((uint)0x0));
                    byteList.AddRange(BitConverter.GetBytes(JumpCmdVal));
                }
                byteList.AddRange(BitConverter.GetBytes(EffectCode));
                byteList.Add(EffectParam);
                return byteList.ToArray();
            }

            public void SetNote(UgeNoteTable note)
            {
                RowNote = (uint)note;
            }

            public void SetEffect(UgeEffectTable effect, byte effectData)
            {
                EffectCode = (uint)effect;
                EffectParam = (byte)effectData;
            }

            public void SetInstrument(int instrumentIndex)
            {
                InstrumentValue = (byte)(instrumentIndex + 1);
            }

            public void SetJump(int loopPoint)
            {
                JumpCmdVal = (byte)loopPoint;
            }
        }
    }
}