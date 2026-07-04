using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ForspokenBinTool.Models;
using ForspokenBinTool.Core.FixidMapping.Maps; 

namespace ForspokenBinTool.Core.FixidMapping
{
    public enum ReferenceMapPriority
    {
        StringLabelMapPrioritized,
        ScriptLabelMapPrioritized
    }

    public static class ReferenceMapBuilder
    {
        // -----------------------------------------------------------------------
        // CONFIGURATION - Needs to have ScriptLabelMap.cs and STringLabelMap.cs already generated
        // Choose which map takes priority when resolving FIXIDs.
        // The prioritized map will overwrite any conflicting entries from the secondary map.
        // -----------------------------------------------------------------------
        public static ReferenceMapPriority MapPriority = ReferenceMapPriority.StringLabelMapPrioritized;

        private const uint NameFixidTag = 16785072; 

        public static void GenerateCSharpMap(List<ParamTable> tables, string outputCsPath)
        {
            var map = new Dictionary<uint, string>();

            Dictionary<uint, string> combinedStringMap = BuildCombinedStringMap();
            Console.WriteLine($"[ReferenceMapBuilder] Loaded combined string map. Total unique strings: {combinedStringMap.Count}");

            foreach (var table in tables)
            {
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

                if (hasDuplicateIds)
                {
                    continue; 
                }

                int nameTagIndex = -1;
                for (int i = 0; i < table.Tags.Count; i++)
                {
                    if (table.Tags[i].Id == NameFixidTag)
                    {
                        nameTagIndex = i;
                        break;
                    }
                }

                if (nameTagIndex == -1) continue;

                foreach (var element in table.Elements)
                {
                    uint elementId = element.Id;
                    uint targetStringFixid = element.Values[nameTagIndex];

                    if (targetStringFixid == 0) continue;

                    if (combinedStringMap.TryGetValue(targetStringFixid, out string resolvedName))
                    {
                        if (!string.IsNullOrWhiteSpace(resolvedName) && resolvedName != "<EmptyString>")
                        {
                            map[elementId] = resolvedName;
                        }
                    }
                }
            }

            WriteCSharpFile(outputCsPath, map);
            Console.WriteLine($"[ReferenceMapBuilder] Done! Built {map.Count} direct ElementID-to-Name references.");
            Console.WriteLine($"[ReferenceMapBuilder] Saved C# map to: {outputCsPath}");
        }

        private static Dictionary<uint, string> BuildCombinedStringMap()
        {
            var combinedMap = new Dictionary<uint, string>();

            if (MapPriority == ReferenceMapPriority.StringLabelMapPrioritized)
            {
                MergeMap(combinedMap, ScriptLabelMap.Map); 
                MergeMap(combinedMap, StringLabelMap.Map); 
            }
            else
            {
                MergeMap(combinedMap, StringLabelMap.Map); 
                MergeMap(combinedMap, ScriptLabelMap.Map); 
            }

            return combinedMap;
        }

        private static void MergeMap(Dictionary<uint, string> targetMap, Dictionary<uint, string> sourceMap)
        {
            foreach (var kvp in sourceMap)
            {
                targetMap[kvp.Key] = kvp.Value;
            }
        }

        private static void WriteCSharpFile(string path, Dictionary<uint, string> map)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine();
            sb.AppendLine("namespace ForspokenBinTool.Core.FixidMapping.Maps");
            sb.AppendLine("{");
            sb.AppendLine("    public static class ReferenceLabelMap");
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