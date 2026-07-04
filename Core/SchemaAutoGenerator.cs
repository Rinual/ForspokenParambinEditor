using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ForspokenBinTool.IO;
using ForspokenBinTool.Models;
using ForspokenBinTool.Models.Enums;
using ForspokenBinTool.Core.Detectors;

namespace ForspokenBinTool.Core
{
    public static class SchemaAutoGenerator
    {
        public static void GeneratePhase1(
            string inputDirectory,
            string outCsFile)
        {
            if (!Directory.Exists(inputDirectory))
                return;

            var schemaDetections =
                new Dictionary<(uint, uint), ParambinDataType>();

            string[] files =
                Directory.GetFiles(
                    inputDirectory,
                    "*.parambin",
                    SearchOption.TopDirectoryOnly);

            Console.WriteLine(
                $"Starting Schema Generation on {files.Length} files...");

            foreach (string file in files)
            {
                try
                {
                    AnalyzeFile(file, schemaDetections);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"[Error] Failed {Path.GetFileName(file)}: {ex.Message}");
                }
            }

            WriteOutputs(outCsFile, schemaDetections);

            Console.WriteLine(
                $"Schema Generation Complete! Generated {schemaDetections.Count} schema entries.");
        }

        private static void AnalyzeFile(
            string filePath,
            Dictionary<(uint, uint), ParambinDataType> schemaDetections)
        {
            using var reader = new ParambinReader(filePath);

            ArchiveHeader header = reader.ReadHeader();

            List<ParamTable> tables =
                reader.ReadAllTables(header);

            foreach (ParamTable table in tables)
            {
                for (int tagIndex = 0;
                     tagIndex < table.Tags.Count;
                     tagIndex++)
                {
                    ParamTag tag = table.Tags[tagIndex];

                    ParambinDataType detectedType =
                        DetectionManager.EvaluateColumn(
                            table,
                            tagIndex);

                    if (detectedType == ParambinDataType.Unknown)
                        continue;

                    schemaDetections[
                        (table.TableId, tag.Id)
                    ] = detectedType;
                }
            }
        }

        private static void WriteOutputs(
            string csPath,
            Dictionary<(uint, uint), ParambinDataType> schema)
        {
            using var csWriter = new StreamWriter(csPath);

            csWriter.WriteLine("using System.Collections.Generic;");
            csWriter.WriteLine("using ForspokenBinTool.Models.Enums;");
            csWriter.WriteLine();
            csWriter.WriteLine("namespace ForspokenBinTool.Core");
            csWriter.WriteLine("{");
            csWriter.WriteLine("    public static class SchemaRegistry");
            csWriter.WriteLine("    {");
            csWriter.WriteLine("        public static readonly Dictionary<(uint TableId, uint TagId), ParambinDataType> Table = new()");
            csWriter.WriteLine("        {");

            var sortedKeys =
                schema.Keys
                      .OrderBy(x => x.Item1)
                      .ThenBy(x => x.Item2)
                      .ToList();

            for (int i = 0; i < sortedKeys.Count; i++)
            {
                var key = sortedKeys[i];

                string line =
                    $"            {{ ({key.Item1}, {key.Item2}), ParambinDataType.{schema[key]} }}";

                if (i < sortedKeys.Count - 1)
                    line += ",";

                csWriter.WriteLine(line);
            }

            csWriter.WriteLine("        };");
            csWriter.WriteLine("    }");
            csWriter.WriteLine("}");
        }
    }
}