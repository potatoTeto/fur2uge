
namespace Fur2Uge
{
    public class FurInstrSM
    {
        private short _smInitialSample;
        private bool _smUseSampleMap;
        private bool _smUseSample;
        private bool _smUseWave;
        private byte _smWaveformLen;
        private List<FurSampleMapEntry> _smMap;

        public FurInstrSM(short smInitialSample, bool smUseSampleMap, bool smUseSample, bool smUseWave, byte smWaveformLen, List<FurSampleMapEntry> smMap)
        {
            _smInitialSample = smInitialSample;
            _smUseSampleMap = smUseSampleMap;
            _smUseSample = smUseSample;
            _smUseWave = smUseWave;
            _smWaveformLen = smWaveformLen;
            _smMap = smMap;
        }
    }
}