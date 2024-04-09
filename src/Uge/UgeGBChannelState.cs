

namespace Fur2Uge
{
    public class UgeGBChannelState
    {
        private bool _leftSpeakerOn = true;
        private bool _rightSpeakerOn = true;
        private byte _currVol = 0xF;
        private int _id;
        private int _currInstrID = 0;

        public UgeGBChannelState(int id)
        {
            _id = id;
        }

        public void SetPan(bool leftSpeakerOn, bool rightSpeakerOn)
        {
            _leftSpeakerOn = leftSpeakerOn;
            _rightSpeakerOn = rightSpeakerOn;
        }

        public byte GetPan()
        {
            // Calculate the value for the first nybble (leftSpeakerOn)
            byte firstNybble = (byte)((_leftSpeakerOn ? 1 : 0) << _id);

            // Calculate the value for the fifth nybble (rightSpeakerOn)
            byte fifthNybble = (byte)((_rightSpeakerOn ? 1 : 0) << (4 + _id));

            // Combine both nybbles using bitwise OR
            byte combinedByte = (byte)(firstNybble | fifthNybble);

            return combinedByte;
        }

        public void SetVol(byte vol)
        {
            _currVol = vol;
        }

        public byte GetVol()
        {
            return _currVol;
        }

        public int GetCurrInstrID()
        {
            return _currInstrID;
        }

        public void SetCurrInstrID(int value)
        {
            _currInstrID = value;
        }
    }
}