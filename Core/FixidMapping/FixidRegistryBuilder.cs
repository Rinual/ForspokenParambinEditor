using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using ForspokenBinTool.Core.FixidMapping.Maps;

namespace ForspokenBinTool.Core.FixidMapping
{
    public enum MapSource
    {
        ScriptLabelMap,
        StringLabelMap,
        ReferenceLabelMap
    }

    // (this is mostly not needed now, we use Id2sReader to read parambincache string resolution for most, and FixidRegistry_id2dif for the extras not found there)
    public static class FixidRegistryBuilder
    {
        // =======================================================================
        // CONFIGURATION 
        // Define the exact order you want the maps to be written.
        // - The FIRST item is your baseline.
        // - The SECOND item overwrites any conflicts with the first.
        // - The THIRD item overwrites any conflicts with the first two.
        // To use fewer maps, simply delete them from this list.
        // MapSources = ScriptLabelMap, StringLabelMap, ReferenceLabelMap
        // =======================================================================

        public static MapSource[] MergeOrder = new MapSource[]
        {
            MapSource.ScriptLabelMap,     // 1st: Baseline
            MapSource.StringLabelMap,     // 2nd: Overwrites ScriptLabels
            MapSource.ReferenceLabelMap   // 3rd: Overwrites String and Script
        };

        // =======================================================================

        /// Builds the master Fixid registry based on the individual different FIXID mappings generated
        public static void GenerateCSharpMap(string outputCsPath)
        {
            var masterRegistry = new Dictionary<uint, string>();

            Console.WriteLine($"\n[FixidRegistry] Building Master Registry...");
            Console.WriteLine($"[FixidRegistry] Merging {MergeOrder.Length} maps in sequential order...");

            // Process the maps in the exact order defined in the configuration
            foreach (var source in MergeOrder)
            {
                switch (source)
                {
                    case MapSource.ScriptLabelMap:
                        MergeMap(masterRegistry, ScriptLabelMap.Map, "ScriptLabels");
                        break;
                    case MapSource.StringLabelMap:
                        MergeMap(masterRegistry, StringLabelMap.Map, "StringLabels");
                        break;
                    case MapSource.ReferenceLabelMap:
                        MergeMap(masterRegistry, ReferenceLabelMap.Map, "ReferenceLookups");
                        break;
                }
            }

            WriteCSharpFile(outputCsPath, masterRegistry);

            Console.WriteLine($"[FixidRegistry] Done! Master Registry contains {masterRegistry.Count} unique FIXIDs.");
            Console.WriteLine($"[FixidRegistry] Saved C# map to: {outputCsPath}");
        }

        private static void MergeMap(Dictionary<uint, string> master, Dictionary<uint, string> newMap, string sourceName)
        {
            if (newMap == null || newMap.Count == 0)
            {
                Console.WriteLine($"  -> Skipped {sourceName} (Empty).");
                return;
            }

            int addedCount = 0;
            int overwrittenCount = 0;

            foreach (var kvp in newMap)
            {
                if (master.ContainsKey(kvp.Key))
                {
                    if (master[kvp.Key] != kvp.Value)
                    {
                        overwrittenCount++;
                    }
                }
                else
                {
                    addedCount++;
                }

                master[kvp.Key] = kvp.Value;
            }

            Console.WriteLine($"  -> Processed {sourceName}: {addedCount} new additions, {overwrittenCount} overwritten.");
        }

        private static void WriteCSharpFile(string path, Dictionary<uint, string> map)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine();

            sb.AppendLine("namespace ForspokenBinTool.Core");
            sb.AppendLine("{");
            sb.AppendLine("    public static class FixidRegistry");
            sb.AppendLine("    {");
            sb.AppendLine("        public static readonly Dictionary<uint, string> Table = new()");
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