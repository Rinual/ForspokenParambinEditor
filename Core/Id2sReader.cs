using System.Collections.Generic;
using System.IO;
using System.Text;
using ForspokenBinTool.Models;

namespace ForspokenBinTool.Core
{
    public static class Id2sReader
    {
        public static Id2sMap Read(string filePath)
        {
            var map = new Id2sMap();

            if (!File.Exists(filePath))
                return map;

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(fs))
            {
                byte one = reader.ReadByte();
                uint magic = reader.ReadUInt32();
                uint fileSize = reader.ReadUInt32();
                uint numberOfEntries = reader.ReadUInt32();

                for (int i = 0; i < numberOfEntries; i++)
                {
                    ushort internalId = reader.ReadUInt16();
                    short category = reader.ReadInt16();
                    string idString = ReadNullTerminatedString(reader);

                    uint fullFixid = (uint)(((ushort)category << 16) | internalId);

                    if (!string.IsNullOrWhiteSpace(idString) && !map.Entries.ContainsKey(fullFixid))
                    {
                        idString = idString.Replace("\0", "").Replace("\r", "").Replace("\n", "");
                        map.Entries.Add(fullFixid, idString);
                    }
                }
            }

            return map;
        }

        private static string ReadNullTerminatedString(BinaryReader reader)
        {
            var bytes = new List<byte>();
            byte b;
            while ((b = reader.ReadByte()) != 0)
            {
                bytes.Add(b);
            }
            return Encoding.UTF8.GetString(bytes.ToArray());
        }
    }
}