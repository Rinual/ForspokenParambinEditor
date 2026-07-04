using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ForspokenBinTool.Core.FixidMapping.Generators
{
    public static class Id2sMapGenerator
    {
        public static void GenerateCSharpMap(string id2sFilePath, string outputCsPath)
        {
            Console.WriteLine($"\n[Main] Building Id2sLabelMap from {Path.GetFileName(id2sFilePath)}...");

            if (!File.Exists(id2sFilePath))
            {
                Console.WriteLine($"[ERROR] ID2S file not found: {id2sFilePath}");
                return;
            }

            var map = new Dictionary<uint, string>();

            using (var fs = new FileStream(id2sFilePath, FileMode.Open, FileAccess.Read))
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

                    if (!string.IsNullOrWhiteSpace(idString) && !map.ContainsKey(fullFixid))
                    {
                        map.Add(fullFixid, idString);
                    }
                }
            }

            using (var writer = new StreamWriter(outputCsPath, false, Encoding.UTF8))
            {
                writer.WriteLine("using System.Collections.Generic;");
                writer.WriteLine();
                writer.WriteLine("namespace ForspokenBinTool.Core.FixidMapping.Maps");
                writer.WriteLine("{");
                writer.WriteLine("    public static class Id2sLabelMap");
                writer.WriteLine("    {");
                writer.WriteLine("        public static readonly Dictionary<uint, string> Map = new Dictionary<uint, string>");
                writer.WriteLine("        {");

                foreach (var kvp in map)
                {
                    string safeStr = kvp.Value
                        .Replace("\\", "\\\\")
                        .Replace("\"", "\\\"")
                        .Replace("\r", "\\r")
                        .Replace("\n", "\\n")
                        .Replace("\0", "");
                    writer.WriteLine($"            {{ {kvp.Key}, \"{safeStr}\" }},");
                }

                writer.WriteLine("        };");
                writer.WriteLine("    }");
                writer.WriteLine("}");

            }

            Console.WriteLine($"Successfully generated Id2sLabelMap.cs with {map.Count} entries.");
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