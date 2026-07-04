using ForspokenBinTool.IO;
using ForspokenBinTool.Models;
using ForspokenBinTool.Models.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace ForspokenBinTool.Core
{
    public static class ParambinDataAccess
    {
        public static string FormatValue(
                    ParamTable table,
                    ParamTag tag,
                    uint value)
        {
            if (SchemaConfig.TryGetDataType(table.TableId, tag.Id, out ParambinDataType type))
            {
                switch (type)
                {
                    case ParambinDataType.Fixid:
                        return GlobalTextRegistry.Resolve(value);

                    case ParambinDataType.Float:
                        float fVal = BitConverter.Int32BitsToSingle(unchecked((int)value));
                        return fVal.ToString("R", CultureInfo.InvariantCulture);

                    case ParambinDataType.String:
                        return ReadString(table, value);

                    case ParambinDataType.FixidArray:
                        return "[" + string.Join(",",
                            ReadUIntArray(table, value)
                                .ConvertAll(GlobalTextRegistry.Resolve)) + "]";

                    case ParambinDataType.Integer:
                        return unchecked((int)value).ToString();

                    case ParambinDataType.StringArray:
                        return "[" + string.Join(",",
                            ReadUIntArray(table, value)
                                .ConvertAll(v => ReadString(table, v))) + "]";

                    case ParambinDataType.IntegerArray:
                        return "[" + string.Join(",",
                            ReadUIntArray(table, value).ConvertAll(v => unchecked((int)v))) + "]";

                    case ParambinDataType.FloatArray:
                        return "[" + string.Join(",",
                            ReadFloatArray(table, value)) + "]";

                    default:
                        return value.ToString();
                }
            }

            return value.ToString();
        }

        private static string ReadString(ParamTable table, uint offset)
        {
            if (table.StringHeapRaw == null ||
                table.StringHeapRaw.Length == 0)
            {
                return string.Empty;
            }

            if (offset >= table.StringHeapRaw.Length)
            {
                return $"<INVALID_STRING:{offset}>";
            }

            int end = (int)offset;

            while (end < table.StringHeapRaw.Length &&
                   table.StringHeapRaw[end] != 0)
            {
                end++;
            }

            int length = end - (int)offset;

            if (length <= 0)
                return string.Empty;

            byte[] rawStringBytes = new byte[length];

            Buffer.BlockCopy(
                table.StringHeapRaw,
                (int)offset,
                rawStringBytes,
                0,
                length);

            return BinaryReaderExtensions.DecodeGameString(rawStringBytes);
        }

        private static List<uint> ReadUIntArray(ParamTable table, uint offset)
        {
            var result = new List<uint>();

            if (table.ArrayHeapRaw == null ||
                table.ArrayHeapRaw.Length == 0)
                return result;

            int byteOffset = (int)(offset * 4);

            if (byteOffset < 0 ||
                byteOffset + 4 > table.ArrayHeapRaw.Length)
                return result;

            uint count = BitConverter.ToUInt32(table.ArrayHeapRaw, byteOffset);

            int payloadStart = byteOffset + 4;
            int payloadEnd = payloadStart + (int)(count * 4);

            if (payloadEnd > table.ArrayHeapRaw.Length)
                return result;

            for (int i = 0; i < count; i++)
            {
                uint v = BitConverter.ToUInt32(
                    table.ArrayHeapRaw,
                    payloadStart + i * 4);

                result.Add(v);
            }

            return result;
        }

        private static List<float> ReadFloatArray(ParamTable table, uint offset)
        {
            var result = new List<float>();

            if (table.ArrayHeapRaw == null ||
                table.ArrayHeapRaw.Length == 0)
                return result;

            int byteOffset = (int)(offset * 4);

            if (byteOffset < 0 ||
                byteOffset + 4 > table.ArrayHeapRaw.Length)
                return result;

            uint count = BitConverter.ToUInt32(table.ArrayHeapRaw, byteOffset);

            int payloadStart = byteOffset + 4;
            int payloadEnd = payloadStart + (int)(count * 4);

            if (payloadEnd > table.ArrayHeapRaw.Length)
                return result;

            for (int i = 0; i < count; i++)
            {
                float v = BitConverter.ToSingle(
                    table.ArrayHeapRaw,
                    payloadStart + i * 4);

                result.Add(v);
            }

            return result;
        }
    }
}
