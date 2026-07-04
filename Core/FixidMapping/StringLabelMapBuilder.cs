using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ForspokenBinTool.Models;
using ForspokenBinTool.IO;

namespace ForspokenBinTool.Core.FixidMapping
{
    public static class StringLabelMapBuilder
    {
        //this map will improve over time, if TagID is more properly mapped later, right now its a bit meh
        private static readonly uint[] StringTagIds = {
            738201011, // GameText
            16779977,  // Label
            16857085   // Label2
        };
        

        public static void GenerateCSharpMap(List<ParamTable> primaryTables, List<ParamTable> fallbackTables, string outputCsPath, string collisionLogPath)
        {
            var map = new Dictionary<uint, string>();
            int collisionCount = 0;
            int autoResolvedCount = 0;
            var collisionLogs = new List<string>();

            // Phase 1: Scan all primary files
            foreach (var table in primaryTables)
            {
                ProcessTable(table, map, ref collisionCount, ref autoResolvedCount, isFallback: false, collisionLogs);
            }

            // Phase 2: Scan Japanese/Fallback files 
            foreach (var table in fallbackTables)
            {
                ProcessTable(table, map, ref collisionCount, ref autoResolvedCount, isFallback: true, collisionLogs);
            }

            if (collisionLogs.Count > 0)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(collisionLogPath));
                File.WriteAllLines(collisionLogPath, collisionLogs, new UTF8Encoding(true));
                Console.WriteLine($"[StringLabelMapBuilder] Saved {collisionLogs.Count / 4} collision logs to: {collisionLogPath}");
            }

            WriteCSharpFile(outputCsPath, map);
            Console.WriteLine($"[StringLabelMapBuilder] Done! Extracted {map.Count} unique string IDs.");
            Console.WriteLine($"[StringLabelMapBuilder] Found {collisionCount} unresolvable collisions in primary files.");
            Console.WriteLine($"[StringLabelMapBuilder] Saved C# map to: {outputCsPath}");
        }

        private static void ProcessTable(ParamTable table, Dictionary<uint, string> map, ref int collisionCount, ref int autoResolvedCount, bool isFallback, List<string> collisionLogs)
        {
            if (table == null || table.StringHeapRaw == null || table.StringHeapRaw.Length == 0)
                return;

            bool hasDuplicateIds = false;
            var seenIds = new HashSet<uint>();
            foreach (var element in table.Elements)
            {
                if (!seenIds.Add(element.Id))
                {
                    hasDuplicateIds = true;
                    break;
                }
            }

            var validTagIndices = new List<int>();
            for (int i = 0; i < table.Tags.Count; i++)
            {
                uint tagId = table.Tags[i].Id;

                if (StringTagIds.Contains(tagId))
                {
                    // If the tag is the volatile Label tag AND the table has duplicate IDs, skip it
                    if (tagId == 16779977 && hasDuplicateIds)
                    {
                        continue;
                    }

                    validTagIndices.Add(i);
                }
            }

            if (validTagIndices.Count == 0) return;

            foreach (var element in table.Elements)
            {
                uint fixid = element.Id;

                foreach (int tagIndex in validTagIndices)
                {
                    uint tagId = table.Tags[tagIndex].Id; 
                    uint stringOffset = element.Values[tagIndex];
                    string text = ExtractAndDecodeString(table, stringOffset);

                    // allows empty game text strings to display as such, but discards the others
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        if (tagId == 738201011) 
                        {
                            text = "<EmptyString>";
                        }
                        else
                        {
                            continue; 
                        }
                    }

                    if (map.TryGetValue(fixid, out string existingText))
                    {
                        if (existingText == text) continue;

                        if (isFallback)
                        {
                            if (existingText == "<EmptyString>" && text != "<EmptyString>")
                            {
                                map[fixid] = text;
                            }
                        }
                        else
                        {
                            if (existingText == "<EmptyString>" && text != "<EmptyString>")
                            {
                                map[fixid] = text; // text overwrites an empty string 
                            }
                            else if (existingText != "<EmptyString>" && text != "<EmptyString>")
                            {
                                // collision logging
                                collisionLogs.Add($"Table {table.TableId} | ID {fixid}");
                                collisionLogs.Add($"  Existing : {existingText}");
                                collisionLogs.Add($"  New      : {text}");
                                collisionLogs.Add(new string('-', 50));

                                collisionCount++;
                            }
                        }
                    }
                    else
                    {
                        map[fixid] = text;
                    }
                }
            }
        }

        private static string ExtractAndDecodeString(ParamTable table, uint offset)
        {
            if (offset >= table.StringHeapRaw.Length)
                return string.Empty;

            int length = 0;
            while (offset + length < table.StringHeapRaw.Length && table.StringHeapRaw[offset + length] != 0x00)
            {
                length++;
            }

            if (length == 0)
                return string.Empty;

            byte[] stringBytes = new byte[length];
            Array.Copy(table.StringHeapRaw, offset, stringBytes, 0, length);

            string decodedText = BinaryReaderExtensions.DecodeGameString(stringBytes);
            string finalText = decodedText?.Trim() ?? string.Empty;

            // Cap the string length 
            if (finalText.Length > 150)
            {
                return finalText.Substring(0, 150) + "...";
            }

            return finalText;
        }

        private static void WriteCSharpFile(string path, Dictionary<uint, string> map)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine();
            sb.AppendLine("namespace ForspokenBinTool.Core.FixidMapping.Maps");
            sb.AppendLine("{");
            sb.AppendLine("    public static class StringLabelMap");
            sb.AppendLine("    {");
            sb.AppendLine("        public static readonly Dictionary<uint, string> Map = new Dictionary<uint, string>");
            sb.AppendLine("        {");

            foreach (var kvp in map.OrderBy(k => k.Key))
            {
                string safeValue = kvp.Value
                    .Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r")
                    .Replace("\t", "\\t");

                sb.AppendLine($"            {{ {kvp.Key}, \"{safeValue}\" }},");
            }

            sb.AppendLine("        };");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, sb.ToString());
        }
    }
}