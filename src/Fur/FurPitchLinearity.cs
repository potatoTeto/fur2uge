namespace Fur2Uge
{
    public partial class FurFile
    {
        public enum FurPitchLinearity
        {
            NON_LINEAR,
            ONLY_PITCH_CHANGE,  // (04xy/E5xx) linear
            FULL_LINEAR         // (>= Version 94)
        }
    }
}