using System.Collections.Generic;

namespace ForspokenBinTool.Core.Detectors
{
    public static class FixidDetector
    {
        public static bool Detect(List<uint> validValues)
        {
            int totalEvaluated = 0;
            int fixidCount = 0;

            foreach (var val in validValues)
            {
                if (val == 0 || val == uint.MaxValue) continue;

                totalEvaluated++;

                if (IsKnownOrLikelyFixid(val))
                {
                    fixidCount++;
                }
            }

            if (totalEvaluated == 0) return false;

            float confidenceRatio = (float)fixidCount / totalEvaluated;
            return confidenceRatio >= 0.9f;
        }

        private const uint FIXID_PADDING = 1000000;

        private static bool IsKnownOrLikelyFixid(uint val)
        {
            // known fixid ranges, FIXIDs are a combo of a InternalId Int16 and a category Int16, though representing them that way is not particullarly helpful
            return
                (val >= 16_777_216 - FIXID_PADDING && val <= 17_622_105 + FIXID_PADDING) ||
                (val >= 67_109_769 - FIXID_PADDING && val <= 74_126_866 + FIXID_PADDING) ||
                (val >= 251_658_240 - FIXID_PADDING && val <= 252_459_145 + FIXID_PADDING) ||
                (val >= 318_767_104 - FIXID_PADDING && val <= 318_778_549 + FIXID_PADDING) ||
                (val >= 385_875_968 - FIXID_PADDING && val <= 385_896_120 + FIXID_PADDING) ||
                (val >= 654_311_424 - FIXID_PADDING && val <= 654_312_928 + FIXID_PADDING) ||
                (val >= 671_088_837 - FIXID_PADDING && val <= 672_270_253 + FIXID_PADDING) ||
                (val >= 721_420_288 - FIXID_PADDING && val <= 721_420_323 + FIXID_PADDING) ||
                (val >= 738_197_504 - FIXID_PADDING && val <= 738_250_742 + FIXID_PADDING) ||
                (val >= 754_974_720 - FIXID_PADDING && val <= 754_976_863 + FIXID_PADDING);
        }

    }
}