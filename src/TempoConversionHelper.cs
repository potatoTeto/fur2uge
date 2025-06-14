using System;
using System.Linq;

namespace fur2Uge
{
    /// <summary>
    /// Helper class for converting Furnace Tracker tempo settings to hUGETracker-compatible timer settings.
    /// </summary>
    public static class TempoConversionHelper
    {
        /// <summary>
        /// Result struct containing tempo conversion results and metadata.
        /// </summary>
        public struct UgeTempoResult
        {
            public int TicksPerRow;       // Matches Furnace Speed (first speed)
            public int TimerDivider;      // Computed for best BPM match
            public bool TimerEnabled;     // True if using timer (vs VBlank)
            public double SourceBpm;      // Furnace input BPM
            public double ResultBpm;      // Best matched hUGETracker BPM
            public bool WasApproximate;   // True if approximated due to custom tick rate
            public string FurnaceSuggestion; // Suggestion for Furnace settings (if needed)
        }

        /// <summary>
        /// Converts Furnace tempo to hUGETracker tempo settings, matching BPM closely.
        /// Assumes TicksPerRow in hUGETracker equals Furnace Speed value.
        /// </summary>
        /// <param name="timeBaseFromSong">Time base from Furnace song (raw value + 1 = actual rows per tick)</param>
        /// <param name="highlightA">Furnace HighlightA (rows per beat)</param>
        /// <param name="speedPattern">Furnace speed pattern array (only first value used as Speed)</param>
        /// <param name="virtualTempoN">Virtual Tempo numerator</param>
        /// <param name="virtualTempoD">Virtual Tempo denominator</param>
        /// <param name="tickRateHz">Tick rate in Hz (Furnace song base frequency)</param>
        /// <returns>Conversion result including best hUGETracker timer settings</returns>
        public static UgeTempoResult ConvertFurTempoToUge(
            int timeBaseFromSong,
            int baseSpeed,
            double[] speedPattern,
            int virtualTempoN,
            int virtualTempoD,
            double tickRateHz,
            int highlightA)
        {
            // Use first speed value as Furnace Speed (= TicksPerRow in hUGETracker)
            double speed = (speedPattern != null && speedPattern.Length > 0) ? speedPattern[0] : 1.0;

            // HighlightA fallback
            double hl = highlightA > 0 ? highlightA : 4.0;

            // TimeBase (TimeBase + 1), minimum 1
            double timeBase = (timeBaseFromSong + 1) > 1 ? (timeBaseFromSong + 1) : 1.0;

            // Virtual tempo components, minimum 1
            double vN = Math.Max(virtualTempoN, 1);
            double vD = Math.Max(virtualTempoD, 1);

            // Calculate Furnace BPM with highlight divisor included:
            // BPM = (60 * hz) / (speed * highlightA * timeBase) * (vN / vD)
            double sourceBpm = (60.0 * tickRateHz) / (speed * hl * timeBase) * (vN / vD);

            // The hUGETracker formula for BPM is:
            // BPM = (4096 * 15) / ((256 - TimerDivider) * TicksPerRow)
            // Since TicksPerRow must match Furnace Speed (rounded integer),
            // we only vary TimerDivider to get closest BPM.

            int ticksPerRow = (int)Math.Round(speed);
            if (ticksPerRow < 1) ticksPerRow = 1;

            double bestError = double.MaxValue;
            int bestDivider = 0;
            double bestResultBpm = 0.0;

            // TimerDivider valid range: 1..254
            for (int divider = 1; divider < 255; divider++)
            {
                double bpm = (4096.0 * 15.0) / ((256 - divider) * ticksPerRow);
                double error = Math.Abs(bpm - sourceBpm);

                if (error < bestError)
                {
                    bestError = error;
                    bestDivider = divider;
                    bestResultBpm = bpm;
                }
            }

            var result = new UgeTempoResult
            {
                TicksPerRow = ticksPerRow,
                TimerDivider = bestDivider,
                TimerEnabled = true,
                SourceBpm = sourceBpm,
                ResultBpm = bestResultBpm,
                WasApproximate = false,
                FurnaceSuggestion = null
            };

            // Detect custom tick rate (not 50 or 60 Hz)
            bool isCustomHz = Math.Abs(tickRateHz - 60.0) > 0.01 && Math.Abs(tickRateHz - 50.0) > 0.01;

            if (isCustomHz)
            {
                result.WasApproximate = true;

                // Suggest Furnace settings assuming 60 Hz tick rate
                // Keep HighlightA same as input, and TicksPerRow (Speed) same as input
                // Only adjust Virtual Tempo numerator (100-200) for best BPM match
                // Virtual Tempo denominator fixed at 150 (common default)

                double bestSuggestionError = double.MaxValue;
                int bestVN = virtualTempoN; // fallback to input
                string suggestion = null;

                for (int testVN = 100; testVN <= 200; testVN += 5)
                {
                    double testBpm =
                        (60.0 * 60.0) / (speed * hl * timeBase) * ((double)testVN / 150.0);

                    double error = Math.Abs(testBpm - sourceBpm);

                    if (error < bestSuggestionError)
                    {
                        bestSuggestionError = error;
                        bestVN = testVN;
                        suggestion = $"Speed {(int)speed}, Virtual Tempo {testVN}/150\n(Keep HighlightA at {highlightA})";
                    }
                }

                result.FurnaceSuggestion =
                    $"Custom Base Tempo detected ({tickRateHz} Hz).\n" +
                    $"For better hUGETracker BPM match ({Math.Round(bestResultBpm, 2)}), try Furnace settings at standard Tick Rate 60 Hz:\n" +
                    suggestion + "\n";
            }

            return result;
        }
    }
}
