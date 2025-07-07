using System;

namespace fur2Uge
{
    /// <summary>
    /// Helper class for converting Furnace Tracker tempo settings to hUGETracker-compatible timer settings.
    /// Supports standard hUGETracker use as well as GBStudio, which has a fixed TimerDivider.
    /// </summary>
    public static class TempoConversionHelper
    {
        /// <summary>
        /// Result struct containing tempo conversion results and metadata.
        /// </summary>
        public struct UgeTempoResult
        {
            public int TicksPerRow;       // Matches Furnace Speed (first speed)
            public int TimerDivider;      // Computed for best BPM match (or fixed for GBStudio)
            public bool TimerEnabled;     // True if using timer (vs VBlank)
            public double SourceBpm;      // Furnace input BPM
            public double ResultBpm;      // Best matched hUGETracker BPM
            public bool WasApproximate;   // True if approximated due to custom tick rate or GBStudio BPM mismatch
            public string FurnaceSuggestion; // Suggestion for Furnace settings (if needed)
        }

        /// <summary>
        /// Converts Furnace tempo to hUGETracker or GBStudio tempo settings, matching BPM closely.
        /// Assumes TicksPerRow in hUGETracker equals Furnace Speed value.
        /// </summary>
        /// <param name="songTimeBase">Time base from Furnace song (raw value + 1 = actual rows per tick)</param>
        /// <param name="songAvgSpeed">Average Furnace Speed (rows per tick)</param>
        /// <param name="virtualTempoN">Virtual Tempo numerator</param>
        /// <param name="virtualTempoD">Virtual Tempo denominator</param>
        /// <param name="tickRateHz">Tick rate in Hz (Furnace song base frequency)</param>
        /// <param name="highlightA">Furnace HighlightA (rows per beat)</param>
        /// <param name="isGBStudio">True if generating settings for GBStudio (fixed TimerDivider = 192)</param>
        /// <returns>Conversion result including best hUGETracker or GBStudio timer settings</returns>
        public static UgeTempoResult ConvertFurTempoToUge(
            int songTimeBase,
            double songAvgSpeed,
            int virtualTempoN,
            int virtualTempoD,
            double tickRateHz,
            int highlightA,
            bool isGBStudio)
        {
            // HighlightA fallback
            double hl = highlightA > 0 ? highlightA : 4.0;

            // TimeBase (TimeBase + 1), minimum 1
            double timeBase = (songTimeBase + 1) > 1 ? (songTimeBase + 1) : 1.0;

            // Virtual tempo components, minimum 1
            double vN = Math.Max(virtualTempoN, 1);
            double vD = Math.Max(virtualTempoD, 1);

            // Calculate Furnace BPM with highlight divisor included:
            // BPM = (60 * hz) / (speed * highlightA * timeBase) * (vN / vD)
            double sourceBpm = (60.0 * tickRateHz) / (songAvgSpeed * hl * timeBase) * (vN / vD);

            // hUGETracker formula for BPM:
            // BPM = (4096 * 15) / ((256 - TimerDivider) * TicksPerRow)
            // TicksPerRow must match Furnace Speed (rounded integer)
            // TimerDivider varies to get closest BPM (except in GBStudio mode)

            int ticksPerRow = (int)Math.Round(songAvgSpeed);
            if (ticksPerRow < 1) ticksPerRow = 1;

            int timerDivider = 192;  // Default for GBStudio, will be overwritten for regular hUGETracker
            double resultBpm = 0.0;
            bool wasApproximate = false;
            string suggestion = null;

            if (isGBStudio)
            {
                // GBStudio forces TimerDivider = 192
                resultBpm = (4096.0 * 15.0) / ((256 - timerDivider) * ticksPerRow);

                // Mark as approximate if BPM differs significantly from source BPM
                double error = Math.Abs(resultBpm - sourceBpm);
                if (error > 0.5) // Threshold adjustable
                {
                    wasApproximate = true;
                }
            }
            else
            {
                // Regular hUGETracker mode: find best TimerDivider for closest BPM
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

                timerDivider = bestDivider;
                resultBpm = bestResultBpm;
            }

            var result = new UgeTempoResult
            {
                TicksPerRow = ticksPerRow,
                TimerDivider = timerDivider,
                TimerEnabled = true,
                SourceBpm = sourceBpm,
                ResultBpm = resultBpm,
                WasApproximate = wasApproximate,
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
                string furnaceSuggestion = null;

                for (int testVN = 100; testVN <= 200; testVN += 5)
                {
                    double testBpm =
                        (60.0 * 60.0) / (songAvgSpeed * hl * timeBase) * ((double)testVN / 150.0);

                    double error = Math.Abs(testBpm - sourceBpm);

                    if (error < bestSuggestionError)
                    {
                        bestSuggestionError = error;
                        bestVN = testVN;
                        furnaceSuggestion = $"Speed {(int)songAvgSpeed}, Virtual Tempo {testVN}/150\n(Keep HighlightA at {highlightA})";
                    }
                }

                result.FurnaceSuggestion =
                    $"Custom Base Tempo detected ({tickRateHz} Hz).\n" +
                    $"For better hUGETracker BPM match ({Math.Round(resultBpm, 2)}), try Furnace settings at standard Tick Rate 60 Hz:\n" +
                    furnaceSuggestion + "\n";
            }

            return result;
        }
    }
}
