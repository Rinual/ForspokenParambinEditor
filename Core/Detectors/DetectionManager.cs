using System.Collections.Generic;
using ForspokenBinTool.Models;
using ForspokenBinTool.Models.Enums;

namespace ForspokenBinTool.Core.Detectors
{
    public static class DetectionManager
    {
        public static ParambinDataType EvaluateColumn(ParamTable table, int tagIndex)
        {
            List<uint> values = GetColumnValues(table, tagIndex);

            if (values.Count == 0)
                return ParambinDataType.Unknown;

            if (FixidDetector.Detect(values))
                return ParambinDataType.Fixid;

            if (StringDetector.Detect(table, values))
                return ParambinDataType.String;

            ParambinDataType arrayType = ArrayDetector.Detect(table, values);
            if (arrayType != ParambinDataType.Unknown)
                return arrayType;

            if (FloatDetector.Detect(values))
                return ParambinDataType.Float;

            if (IntegerDetector.Detect(values))
                return ParambinDataType.Integer;

            return ParambinDataType.Unknown;
        }

        public static ParambinDataType EvaluateScalar(ParamTable table, List<uint> values)
        {
            if (values.Count == 0) return ParambinDataType.Unknown;

            if (FixidDetector.Detect(values)) return ParambinDataType.Fixid;
            if (StringDetector.Detect(table, values)) return ParambinDataType.String;
            if (FloatDetector.Detect(values)) return ParambinDataType.Float;

            return ParambinDataType.Integer;
        }

        private static List<uint> GetColumnValues(ParamTable table, int tagIndex)
        {
            var values = new List<uint>();

            foreach (var element in table.Elements)
            {
                if (tagIndex < element.Values.Count)
                {
                    values.Add((uint)element.Values[tagIndex]);
                }
            }

            return values;
        }
    }
}