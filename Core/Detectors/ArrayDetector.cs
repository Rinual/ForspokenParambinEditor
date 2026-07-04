using System;
using System.Collections.Generic;
using System.Linq;
using ForspokenBinTool.Models;
using ForspokenBinTool.Models.Enums;

namespace ForspokenBinTool.Core.Detectors
{
    public static class ArrayDetector
    {
        public static ParambinDataType Detect(ParamTable table, List<uint> values)
        {
            if (table.ArrayHeapRaw == null || table.ArrayHeapRaw.Length == 0)
                return ParambinDataType.Unknown;

            var candidates = values
                .Where(v => v != uint.MaxValue)
                .ToList();

            if (candidates.Count == 0)
                return ParambinDataType.Unknown;

            if (candidates.Distinct().Count() <= 1)
                return ParambinDataType.Unknown;

            int arraysWithData = 0;

            uint previousOffset = 0;
            uint previousCount = 0;
            bool firstValue = true;

            var allInnerValues = new List<uint>();

            foreach (uint offset in candidates)
            {
                if (!firstValue && offset <= previousOffset)
                    return ParambinDataType.Unknown;

                long byteOffset = offset * 4L;

                if (byteOffset < 0 || byteOffset + 4 > table.ArrayHeapRaw.Length)
                    return ParambinDataType.Unknown;

                uint count = BitConverter.ToUInt32(
                    table.ArrayHeapRaw,
                    (int)byteOffset);

                if (count > 5000)
                    return ParambinDataType.Unknown;

                long arrayEnd = byteOffset + 4 + (count * 4L);

                if (arrayEnd > table.ArrayHeapRaw.Length)
                    return ParambinDataType.Unknown;

                if (!firstValue)
                {
                    uint minimumNextOffset =
                        previousOffset + 1 + previousCount;

                    if (offset < minimumNextOffset)
                        return ParambinDataType.Unknown;
                }

                if (count > 0)
                {
                    arraysWithData++;

                    for (int i = 0; i < count; i++)
                    {
                        uint innerValue = BitConverter.ToUInt32(
                            table.ArrayHeapRaw,
                            (int)(byteOffset + 4 + (i * 4)));

                        allInnerValues.Add(innerValue);
                    }
                }

                previousOffset = offset;
                previousCount = count;
                firstValue = false;
            }

            if (arraysWithData == 0)
                return ParambinDataType.Unknown;

            if (allInnerValues.Count == 0)
                return ParambinDataType.Unknown;

            ParambinDataType innerType =
                DetectionManager.EvaluateScalar(table, allInnerValues);

            return innerType switch
            {
                ParambinDataType.Fixid => ParambinDataType.FixidArray,
                ParambinDataType.String => ParambinDataType.StringArray,
                ParambinDataType.Float => ParambinDataType.FloatArray,
                _ => ParambinDataType.IntegerArray
            };
        }
    }
}