using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ForspokenBinTool.Core.FixidMapping.Generators
{
    public static class ScriptMapGenerator
    {
        public static void GenerateCSharpMap(string searchDirectory, string outputCsPath)
        {
            // prevents ID collision spam
            var map = new Dictionary<uint, string>
            {
                { 0, "RESOURCE" }
            };

            int collisionCount = 0;
            int autoResolvedCount = 0;

            if (!Directory.Exists(searchDirectory))
            {
                Console.WriteLine($"[ScriptMapGenerator] Directory not found: {searchDirectory}");
                return;
            }

            var xmlFiles = Directory.GetFiles(searchDirectory, "*.xml", SearchOption.AllDirectories);
            Console.WriteLine($"[ScriptMapGenerator] Found {xmlFiles.Length} XML files. Scanning...");

            foreach (var file in xmlFiles)
            {
                try
                {
                    var doc = XDocument.Load(file);

                    foreach (var element in doc.Descendants())
                    {
                        var fixidAttr = element.Attribute("fixid");
                        var typeAttr = element.Attribute("type");

                        if (fixidAttr != null && typeAttr != null && typeAttr.Value == "Fixid")
                        {
                            if (uint.TryParse(fixidAttr.Value, out uint fixid))
                            {
                                if (fixid == 0) continue;

                                string name = element.Value.Trim();
                                if (string.IsNullOrEmpty(name)) continue;

                                if (map.TryGetValue(fixid, out string existingName))
                                {                                   
                                    if (!string.Equals(existingName, name, StringComparison.OrdinalIgnoreCase))
                                    {
                                        // this is for partial names vs extended versions of the same name
                                        bool isNameSubstring = existingName.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0;
                                        bool isExistingSubstring = name.IndexOf(existingName, StringComparison.OrdinalIgnoreCase) >= 0;

                                        if (isNameSubstring || isExistingSubstring)
                                        {
                                            if (name.Length < existingName.Length)
                                            {
                                                map[fixid] = name;
                                            }
                                            autoResolvedCount++;
                                        }
                                        else
                                        {
                                            Console.WriteLine($"[WARNING] Collision ID {fixid}: '{existingName}' vs '{name}' in {Path.GetFileName(file)}");
                                            collisionCount++;
                                        }
                                    }
                                }
                                else
                                {
                                    map[fixid] = name;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Failed to parse {Path.GetFileName(file)}: {ex.Message}");
                }
            }

            WriteCSharpFile(outputCsPath, map);
            Console.WriteLine($"[ScriptMapGenerator] Done! Extracted {map.Count} unique IDs.");
            Console.WriteLine($"[ScriptMapGenerator] Auto-resolved {autoResolvedCount} substring collisions.");
            Console.WriteLine($"[ScriptMapGenerator] Found {collisionCount} unresolvable collisions.");
            Console.WriteLine($"[ScriptMapGenerator] Saved to: {outputCsPath}");
        }

        private static void WriteCSharpFile(string path, Dictionary<uint, string> map)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine();
            sb.AppendLine("namespace ForspokenBinTool.Core.FixidMapping.Maps");
            sb.AppendLine("{");
            sb.AppendLine("    public static class ScriptLabelMap");
            sb.AppendLine("    {");
            sb.AppendLine("        public static readonly Dictionary<uint, string> Map = new Dictionary<uint, string>");
            sb.AppendLine("        {");

            foreach (var kvp in map.OrderBy(k => k.Key))
            {
                string safeValue = kvp.Value.Replace("\"", "\\\"");
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