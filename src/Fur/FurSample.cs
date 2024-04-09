
using static Fur2Uge.FurFile;

namespace Fur2Uge
{
    public class FurSample
    {
        private string _smpBlockID;
        private int _smpBlockSize;
        private string _smpName;
        private int _smpLen;
        private int _smpCompatibilityRate;
        private int _smpC4Rate;
        private FurFile.FurSampleDepth _smpDepth;
        private byte _smpLoopDirection;
        private byte _smpFlags;
        private byte _smpFlags2;
        private int _smpLoopStart;
        private int _smpLoopEnd;
        private List<byte> _sampleBlocks;

        private const int BRR_CHUNK_SIZE = 9;

        public FurSample(string smpBlockID, int smpBlockSize, string smpName, int smpLen, int smpCompatibilityRate, int smpC4Rate, FurFile.FurSampleDepth smpDepth, byte smpLoopDirection, byte smpFlags, byte smpFlags2, int smpLoopStart, int smpLoopEnd, List<byte> sampleBlocks)
        {
            _smpBlockID = smpBlockID;
            _smpBlockSize = smpBlockSize;
            _smpName = smpName;
            _smpLen = smpLen;
            _smpCompatibilityRate = smpCompatibilityRate;
            _smpC4Rate = smpC4Rate;
            _smpDepth = smpDepth;
            _smpLoopDirection = smpLoopDirection;
            _smpFlags = smpFlags;
            _smpFlags2 = smpFlags2;
            _smpLoopStart = smpLoopStart;
            _smpLoopEnd = smpLoopEnd;
            _sampleBlocks = sampleBlocks;
        }

        public int[] Decode()
        {
            List<short> decompressedBytes = new List<short>();

            switch (_smpDepth)
            {
                case FurSampleDepth.ZX_SPECTRUM_OVERLAY_DRUM_1BIT:
                    break;
                case FurSampleDepth.ONE_BIT_NES_DPCM_1BIT:
                    break;
                case FurSampleDepth.YMZ_ADPCM:
                    break;
                case FurSampleDepth.QSOUND_ADPCM:
                    break;
                case FurSampleDepth.ADPCM_A:
                    break;
                case FurSampleDepth.ADPCM_B:
                    break;
                case FurSampleDepth.K05_ADPCM:
                    break;
                case FurSampleDepth.EIGHT_BIT_PCM:
                    break;
                case FurSampleDepth.BRR_SNES:
                    /// TODO: Figure out why this is breaking...
                    byte[] oops = { 0x95, 0x01 };
                    byte[] fullBRR = oops.Concat(_sampleBlocks.ToArray()).ToArray();
                    decompressedBytes = DecodeBrr(fullBRR, false);
                    break;
                case FurSampleDepth.VOX:
                    break;
                case FurSampleDepth.EIGHT_BIT_ULAW_PCM:
                    break;
                case FurSampleDepth.C219_PCM:
                    break;
                case FurSampleDepth.IMA_ADPCM:
                    break;
                case FurSampleDepth.SIXTEEN_BIT_PCM:
                    break;
            }
            _sampleBlocks.Clear();
            foreach (byte block in decompressedBytes)
            {
                _sampleBlocks.Add(block);
            }

            File.WriteAllBytes("test.wav", _sampleBlocks.ToArray());
            return new int[] { 0, 0 };
        }

        /// Original implementation by gocha
        /// Source: https://github.com/gocha/split700/blob/1a055bd4496fbdba09ef58498ccfdfb4cabccaef/src/brr2wav.cpp#L69
        public static List<short> DecodeBrr(byte[] brr, bool? ptrLooped)
        {
            List<short> rawSamples = new List<short>();

            int[] prev = { 0, 0 };
            int decodedSize = 0;
            while (decodedSize + BRR_CHUNK_SIZE <= brr.Length)
            {
                byte[] brrChunk = new byte[BRR_CHUNK_SIZE];
                Array.Copy(brr, decodedSize, brrChunk, 0, BRR_CHUNK_SIZE);

                byte flags = brrChunk[0];
                bool chunkEnd = (flags & 1) != 0;
                bool chunkLoop = (flags & 2) != 0;
                byte filter = (byte)((flags >> 2) & 3);
                byte range = (byte)(flags >> 4);
                bool validRange = (range <= 0x0c);

                int S1 = prev[0];
                int S2 = prev[1];

                for (int byteIndex = 0; byteIndex < 8; byteIndex++)
                {
                    int8_t sample1 = new int8_t(brrChunk[1 + byteIndex]);
                    int8_t sample2 = new int8_t((byte)(sample1.Value << 4));

                    sample1 = new int8_t((byte)(sample1.Value >> 4));
                    sample2 = new int8_t((byte)(sample2.Value >> 4));

                    for (int nybble = 0; nybble < 2; nybble++)
                    {
                        int outValue;

                        outValue = nybble != 0 ? sample2.Value : sample1.Value;
                        outValue = validRange ? ((outValue << range) >> 1) : (outValue & ~0x7FF);

                        switch (filter)
                        {
                            case 0: // Direct
                                break;

                            case 1: // 15/16
                                outValue += S1 + ((-S1) >> 4);
                                break;

                            case 2: // 61/32 - 15/16
                                outValue += (S1 << 1) + ((-((S1 << 1) + S1)) >> 5) - S2 + (S2 >> 4);
                                break;

                            case 3: // 115/64 - 13/16
                                outValue += (S1 << 1) + ((-(S1 + (S1 << 2) + (S1 << 3))) >> 6) - S2 + (((S2 << 1) + S2) >> 4);
                                break;
                        }

                        outValue = SClip15(SClamp16(outValue));

                        S2 = S1;
                        S1 = outValue;

                        rawSamples.Add((short)(outValue << 1));
                    }
                }

                prev[0] = S1;
                prev[1] = S2;

                decodedSize += BRR_CHUNK_SIZE;

                if (chunkEnd)
                {
                    if (ptrLooped.HasValue)
                    {
                        ptrLooped = chunkLoop;
                    }
                    break;
                }
            }

            return rawSamples;
        }

        /// Original implementation by gocha
        /// Source: https://github.com/gocha/split700/blob/master/src/SPCSampDir.cpp#L8

        private struct int8_t
        {
            public int8_t(byte value)
            {
                Value = (sbyte)(value & 0xFF); // Ensure sign extension
            }

            public static implicit operator int8_t(byte value)
            {
                return new int8_t(value);
            }

            public static implicit operator sbyte(int8_t value)
            {
                return value.Value;
            }

            public static int8_t operator <<(int8_t value, int shift)
            {
                return new int8_t((byte)(value.Value << shift));
            }

            public static int8_t operator >>(int8_t value, int shift)
            {
                return new int8_t((byte)(value.Value >> shift));
            }

            public sbyte Value;
        }

        private static int SClip15(int value)
        {
            return (value & 0x4000) != 0 ? (value | ~0x3FFF) : (value & 0x3FFF);
        }

        private static int SClamp16(int value)
        {
            return value > 0x7FFF ? 0x7FFF : (value < -0x8000 ? -0x8000 : value);
        }
    }
}