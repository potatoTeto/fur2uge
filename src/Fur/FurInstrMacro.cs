

using static Fur2Uge.FurFile;

namespace Fur2Uge
{
    public class FurInstrMacro
    {
        private FurInstrMacroCode _macroCode;
        private int _macroLen;
        private byte _macroLoop;
        private byte _macroRelease;
        private byte _macroMode;
        private int _instantRelease;
        private FurInstrMacroType _macroType;
        private int _macroWindowIsOpen;
        private byte _macroDelay;
        private byte _macroSpeed;
        private List<int> _macroData;

        public FurInstrMacro(FurInstrMacroCode macroCode, byte macroLoop, byte macroRelease, byte macroMode, int instantRelease, FurInstrMacroType macroType, int macroWindowIsOpen, byte macroDelay, byte macroSpeed, List<int> macroData)
        {
            _macroCode = macroCode;
            _macroLoop = macroLoop;
            _macroRelease = macroRelease;
            _macroMode = macroMode;
            _instantRelease = instantRelease;
            _macroType = macroType;
            _macroWindowIsOpen = macroWindowIsOpen;
            _macroDelay = macroDelay;
            _macroSpeed = macroSpeed;
            _macroData = macroData;
            _macroLen = macroData.Count;
        }

        public FurInstrMacroCode GetMacroCode()
        {
            return _macroCode;
        }

        public FurInstrMacroType GetMacroType()
        {
            return _macroType;
        }

        public List<int> GetMacroData()
        {
            return _macroData;
        }

        public int GetLoopPoint()
        {
            return _macroLoop;
        }
    }
}