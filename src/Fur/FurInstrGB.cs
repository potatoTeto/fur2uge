



namespace Fur2Uge
{
    public class FurInstrGB
    {
        private byte _gbEnvLen;
        private int _gbEnvDir;
        private byte _gbEnvVol;
        private byte _gbSndLen;
        private byte _gbFlags;
        private byte _gbHWSeqLen;
        private List<FurInstrGBHWSeqCmd> _gbHWSeqCmds;

        public FurInstrGB(byte gbEnvLen, int gbEnvDir, byte gbEnvVol, byte gbInstSndLen, byte gbInstFlags, byte gbInstHWSeqLen, List<FurInstrGBHWSeqCmd> gbHWSeqCmds)
        {
            _gbEnvLen = gbEnvLen;
            _gbEnvDir = gbEnvDir;
            _gbEnvVol = gbEnvVol;
            _gbSndLen = gbInstSndLen;
            _gbFlags = gbInstFlags;
            _gbHWSeqLen = gbInstHWSeqLen;
            _gbHWSeqCmds = gbHWSeqCmds;
        }

        public (byte, int, byte, byte, byte, byte, List<FurInstrGBHWSeqCmd>) GetParams()
        {
            return (_gbEnvLen, _gbEnvDir, _gbEnvVol, _gbSndLen, _gbFlags, _gbHWSeqLen, _gbHWSeqCmds);
        }

        public (byte, List<FurInstrGBHWSeqCmd>) GetHWSeq()
        {
            return (_gbHWSeqLen, _gbHWSeqCmds);
        }

        public byte GetEnvVol()
        {
            return _gbEnvVol;
        }

        public void SetEnvVol(byte val)
        {
            _gbEnvVol = val;
        }
    }
}