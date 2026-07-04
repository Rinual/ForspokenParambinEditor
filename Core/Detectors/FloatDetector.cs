using System;
using System.Collections.Generic;
using System.Linq;

namespace ForspokenBinTool.Core.Detectors
{
    public static class FloatDetector
    {
        public static bool Detect(List<uint> values)
        {
            // Strip out 0 (0.0f) and uint.MaxValue (-1) so they don't drag down the confidence ratio.
            var candidates = values.Where(v => v != 0 && v != uint.MaxValue).ToList();

            if (candidates.Count == 0)
                return false;

            int validFloats = 0;
            int totalEvaluated = 0;

            foreach (uint val in candidates)
            {
                totalEvaluated++;

                float fValue = BitConverter.Int32BitsToSingle(unchecked((int)val));

                if (float.IsNaN(fValue) || float.IsInfinity(fValue))
                    continue;

                float absValue = Math.Abs(fValue);

                if (absValue >= 0.00001f && absValue <= 100000.0f)
                {
                    validFloats++;
                }
            }

            if (totalEvaluated == 0) return false;

            float confidence = (float)validFloats / totalEvaluated;

            return confidence >= 0.90f;
        }
    }
}