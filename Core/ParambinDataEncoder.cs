using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ForspokenBinTool.IO;
using ForspokenBinTool.Models;
using ForspokenBinTool.Models.Enums;

namespace ForspokenBinTool.Core
{
    public static class ParambinDataEncoder
    {
        public static uint EncodeValue(
                    ParamTable table,
                    ParamTag tag,
                    string stringValue,
                    MemoryStream arrayHeap,
                    MemoryStream stringHeap)
        {
            if (SchemaConfig.TryGetDataType(table.TableId, tag.Id, out ParambinDataType type))
            {
                switch (type)
                {
                    case ParambinDataType.Fixid:
                        return ParseStrippedId(stringValue);

                    case ParambinDataType.Float:
                        if (float.TryParse(stringValue, out float fVal))
                        {
                            byte[] floatBytes = BitConverter.GetBytes(fVal);
                            return BitConverter.ToUInt32(floatBytes, 0);
                        }
                        return 0;

                    case ParambinDataType.String:
                        return EncodeString(stringValue, stringHeap);

                    case ParambinDataType.FixidArray:
                        var fixids = ParseArray(stringValue).Select(ParseStrippedId).ToArray();
                        return WriteArrayToHeap(fixids, arrayHeap);

                    case ParambinDataType.Integer:
                        if (int.TryParse(stringValue, out int iVal))
                            return unchecked((uint)iVal);
                        return 0;

                    case ParambinDataType.StringArray:
                        var strings = ParseArray(stringValue);
                        var stringOffsets = strings.Select(s => EncodeString(s, stringHeap)).ToArray();
                        return WriteArrayToHeap(stringOffsets, arrayHeap);

                    case ParambinDataType.IntegerArray:
                        var ints = ParseArray(stringValue).Select(s => int.TryParse(s, out int v) ? unchecked((uint)v) : 0).ToArray();
                        return WriteArrayToHeap(ints, arrayHeap);

                    case ParambinDataType.FloatArray:
                        var floats = ParseArray(stringValue).Select(s => float.TryParse(s, out float v) ? v : 0f).ToArray();
                        return WriteFloatArrayToHeap(floats, arrayHeap);
                }
            }

            // Fallback for unknown types
            if (uint.TryParse(stringValue, out uint rawVal))
                return rawVal;

            return ParseStrippedId(stringValue);
        }

        private static uint ParseStrippedId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;

            int spaceIndex = value.IndexOf(' ');
            string numPart = spaceIndex > 0 ? value.Substring(0, spaceIndex) : value;

            if (uint.TryParse(numPart, out uint result))
                return result;

            return 0;
        }

        private static string[] ParseArray(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "[]") return Array.Empty<string>();

            string trimmed = value.Trim('[', ']');
            return trimmed.Split(',').Select(s => s.Trim()).ToArray();
        }


        public static uint EncodeString(string value, MemoryStream stringHeap)
        {
            if (value == null) return 0;

            uint offset = (uint)stringHeap.Position;

            if (value == "")
            {
                stringHeap.WriteByte(0x00);
                return offset;
            }

            string normalizedValue = value;
            byte[] encoded = BinaryReaderExtensions.EncodeGameString(normalizedValue);

            stringHeap.Write(encoded, 0, encoded.Length);
            stringHeap.WriteByte(0x00);

            return offset;
        }

        private static uint WriteArrayToHeap(uint[] values, MemoryStream arrayHeap)
        {
            if (values.Length == 0) return 0;

            uint offsetIndex = (uint)(arrayHeap.Position / 4);

            arrayHeap.Write(BitConverter.GetBytes((uint)values.Length), 0, 4);
            foreach (var val in values)
            {
                arrayHeap.Write(BitConverter.GetBytes(val), 0, 4);
            }

            return offsetIndex;
        }

        private static uint WriteFloatArrayToHeap(float[] values, MemoryStream arrayHeap)
        {
            if (values.Length == 0) return 0;

            uint offsetIndex = (uint)(arrayHeap.Position / 4);

            arrayHeap.Write(BitConverter.GetBytes((uint)values.Length), 0, 4);
            foreach (var val in values)
            {
                arrayHeap.Write(BitConverter.GetBytes(val), 0, 4);
            }

            return offsetIndex;
        }
    }
}