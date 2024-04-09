using static Fur2Uge.FurFile;

namespace Fur2Uge
{
    public class FurInstrGBHWSeqCmd
    {
        private byte _gbHWSeqEnvVol;
        private byte _gbHWSeqSweepSpeed;
        private int _gbHWSeqSweepDir;
        private int _gbHWSeqEnvDir;
        private byte _gbHWSeqEnvLen;
        private byte _gbHWSeqSndLen;
        private byte _gbHWSeqShiftVal;
        private FurInstrGBHWSeqCmdType _gbHWSeqCmdType;

        public FurInstrGBHWSeqCmd(FurFile.FurInstrGBHWSeqCmdType gbHWSeqCmdType, byte gbHWSeqDat1, byte gbHWSeqDat2)
        {
            int bit0, bit1, bit2, bit3, bit4, bit5, bit6, bit7;
            _gbHWSeqCmdType = gbHWSeqCmdType;

            switch (gbHWSeqCmdType)
            {
                case FurInstrGBHWSeqCmdType.SET_ENVELOPE:
                    bit4 = GetBit(gbHWSeqDat1, 4) ? 1 : 0;
                    bit5 = GetBit(gbHWSeqDat1, 5) ? 1 : 0;
                    bit6 = GetBit(gbHWSeqDat1, 6) ? 1 : 0;
                    bit7 = GetBit(gbHWSeqDat1, 7) ? 1 : 0;
                    _gbHWSeqEnvVol = (byte)((bit7 << 3) | (bit6 << 2) | (bit5 << 1) | bit4);

                    _gbHWSeqEnvDir = GetBit(gbHWSeqDat1, 3) ? 1 : 0;

                    bit0 = GetBit(gbHWSeqDat1, 0) ? 1 : 0;
                    bit1 = GetBit(gbHWSeqDat1, 1) ? 1 : 0;
                    bit2 = GetBit(gbHWSeqDat1, 2) ? 1 : 0;
                    _gbHWSeqEnvLen = (byte)((bit2 << 2) | (bit1 << 1) | bit0);
                    _gbHWSeqSndLen = gbHWSeqDat2;

                    break;
                case FurInstrGBHWSeqCmdType.SET_SWEEP:
                    var param = gbHWSeqDat1;
                    var nothing = gbHWSeqDat2;
                    bit4 = GetBit(gbHWSeqDat1, 4) ? 1 : 0;
                    bit5 = GetBit(gbHWSeqDat1, 5) ? 1 : 0;
                    bit6 = GetBit(gbHWSeqDat1, 6) ? 1 : 0;
                    _gbHWSeqSweepSpeed = (byte)((bit6 << 2) | (bit5 << 1) | bit4);
                    _gbHWSeqSweepDir = GetBit(gbHWSeqDat1, 3) ? 1 : 0;

                    bit0 = GetBit(gbHWSeqDat1, 0) ? 1 : 0;
                    bit1 = GetBit(gbHWSeqDat1, 1) ? 1 : 0;
                    bit2 = GetBit(gbHWSeqDat1, 2) ? 1 : 0;
                    _gbHWSeqShiftVal = (byte)((bit2 << 2) | (bit1 << 1) | bit0);
                    break;
                case FurInstrGBHWSeqCmdType.WAIT:
                    break;
                case FurInstrGBHWSeqCmdType.WAIT_FOR_RELEASE:
                    break;
                case FurInstrGBHWSeqCmdType.LOOP:
                    break;
                case FurInstrGBHWSeqCmdType.LOOP_UNTIL_RELEASE:
                    break;
            }

        }

        public (byte, int, byte) GetHWSeqShiftParams()
        {
            return (_gbHWSeqSweepSpeed, _gbHWSeqSweepDir, _gbHWSeqShiftVal);
        }

        public FurInstrGBHWSeqCmdType GetHWSeqCmdType()
        {
            return _gbHWSeqCmdType;
        }
    }
}