
using System;

namespace Fur2Uge
{
    public class FurInstrument
    {
        private string _name;
        private string _finsFormatMagic;
        private int _blockSize;
        private int _instFormatVersion;
        private FurInstrumentType _instrType;
        private List<FurInstrMacro> _instrMacros;
        private FurInstrGB _furInstrGB;
        private FurInstrSM _furInstrSM;
        private FurInstrWS _furInstrWS;
        private FurInstrFM _furInstrFM;
        private int _id;

        public FurInstrument(string finsFormatMagic, int blockSize, int instFormatVersion, FurInstrumentType instrType)
        {
            _finsFormatMagic = finsFormatMagic;
            _blockSize = blockSize;
            _instFormatVersion = instFormatVersion;
            _instrType = instrType;

            _instrMacros = new List<FurInstrMacro>();
            _furInstrGB = new FurInstrGB(2, 1, 15, 2, 0x0, 0x0, new List<FurInstrGBHWSeqCmd>());
            _furInstrSM = new FurInstrSM(0, false, false, false, 32, new List<FurSampleMapEntry>());
        }

        public void SetName(string name)
        {
            _name = name;
        }

        public void AddMacro(FurInstrMacro thisMacro)
        {
            _instrMacros.Add(thisMacro);
        }

        public void SetInstrGB(FurInstrGB thisGBInstr)
        {
            _furInstrGB = thisGBInstr;
        }

        public void SetInstrSM(FurInstrSM thisSMInstr)
        {
            _furInstrSM = thisSMInstr;
        }

        public void SetInstrWS(FurInstrWS thisWSInstr)
        {
            _furInstrWS = thisWSInstr;
        }

        public FurInstrGB GetInstrGB()
        {
            return _furInstrGB;
        }

        public FurInstrWS GetInstrWS()
        {
            return _furInstrWS;
        }

        public FurInstrFM GetInstrFM()
        {
            return _furInstrFM;
        }

        public string GetName()
        {
            return _name;
        }

        public List<FurInstrMacro> GetMacros()
        {
            return _instrMacros;
        }

        public void AddFMInstData(FurInstrFM thisFMInst)
        {
            _furInstrFM = thisFMInst;
        }

        public FurInstrument ShallowCopy()
        {
            var newInst = (FurInstrument)this.MemberwiseClone();

            newInst._furInstrGB = new FurInstrGB(2, 1, 15, 2, 0x0, 0x0, new List<FurInstrGBHWSeqCmd>());

            return newInst;
        }

        public void SetGBVol(byte volVal)
        {
            _furInstrGB.SetEnvVol(volVal);
            _name += string.Format(" (Vol {0:X})", volVal);
        }

        public void SetID(int val)
        {
            _id = val;
        }

        public int GetID()
        {
            return _id;
        }
    }
}