using System.Collections.Generic;
using System.Linq;
using ForspokenBinTool.Models;

namespace ForspokenBinTool.Core.Detectors
{
    public static class StringDetector
    {
        public static bool Detect(ParamTable table, List<uint> values)
        {
            if (table.StringHeapRaw == null || table.StringHeapRaw.Length <= 1)
                return false;

            if (values.Count == 0)
                return false;

            if (values.Any(v => v == 0))
                return false;

            var candidates = values
                .Where(v => v != uint.MaxValue)
                .ToList();

            if (candidates.Count == 0)
                return false;

            int uniqueCount = candidates.Distinct().Count();
            if (uniqueCount < candidates.Count)
                return false;

            // 1 element rows suck, if a string heap exists in it, first string value would always just be 1... so cant trust that
            if (table.ElementCount == 1)
                return false;

            uint previousOffset = 0;
            bool isFirst = true;

            foreach (uint offset in candidates)
            {
                if (offset >= table.StringHeapRaw.Length)
                    return false;

                if (!isFirst)
                {
                    if (offset <= previousOffset)
                        return false;
                }

                if (offset > 0 && table.StringHeapRaw[offset - 1] != 0x00)
                    return false;

                bool foundTerminator = false;
                for (long i = offset; i < table.StringHeapRaw.Length; i++)
                {
                    if (table.StringHeapRaw[i] == 0x00)
                    {
                        foundTerminator = true;
                        break;
                    }
                }

                if (!foundTerminator)
                    return false;

                previousOffset = offset;
                isFirst = false;
            }

            return true;
        }
    }
}