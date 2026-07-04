using System.Collections.Generic;
using System.Linq;

namespace ForspokenBinTool.Core.Detectors
{
    public static class IntegerDetector
    {
        public static bool Detect(List<uint> values)
        {

            if (values.Any(v => v >= 0xFFFFFF00 && v <= 0xFFFFFFFF))
            {
                return true;
            }

            return false;
        }
    }
}