using System;
using System.Collections.Generic;
using System.IO;
using ForspokenBinTool.IO;
using ForspokenBinTool.Models;

namespace ForspokenBinTool.Core
{
    public class Unpacker
    {
        public static bool ExportWithResolvedFixids { get; set; } = true;

        public void UnpackAll(string inputDirectory, string outputDirectory)
        {
            if (!Directory.Exists(inputDirectory))
            {
                throw new DirectoryNotFoundException($"Input directory not found: {inputDirectory}");
            }

            string[] parambinFiles = Directory.GetFiles(
                inputDirectory,
                "*.parambin",
                SearchOption.TopDirectoryOnly);

            if (parambinFiles.Length == 0)
            {
                Console.WriteLine($"No .parambin files found in directory: {inputDirectory}");
                return;
            }

            Console.WriteLine($"Found {parambinFiles.Length} .parambin files. Starting batch unpack...");

            foreach (string filePath in parambinFiles)
            {
                try
                {
                    Console.WriteLine(new string('-', 40));
                    Unpack(filePath, outputDirectory);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Error] Failed to unpack '{Path.GetFileName(filePath)}'");
                    Console.WriteLine(ex);
                }
            }

            Console.WriteLine(new string('-', 40));
            Console.WriteLine("Batch unpack entirely complete!");
        }

        public void Unpack(string inputFilePath, string outputDirectory)
        {
            if (!File.Exists(inputFilePath))
            {
                throw new FileNotFoundException($"Input file not found: {inputFilePath}");
            }

            string rawFileName = Path.GetFileNameWithoutExtension(inputFilePath);
            int lastUnderscore = rawFileName.LastIndexOf('_');

            string cleanFolderName = lastUnderscore > 0
                ? rawFileName[..lastUnderscore]
                : rawFileName;

            string targetDirectory = Path.Combine(outputDirectory, cleanFolderName);
            Directory.CreateDirectory(targetDirectory);

            Console.WriteLine($"Unpacking: {Path.GetFileName(inputFilePath)} into folder '{cleanFolderName}'...");

            using var reader = new ParambinReader(inputFilePath);
            ArchiveHeader header = reader.ReadHeader();

            Console.WriteLine($"Discovered {header.TableCount} tables in archive.");

            List<ParamTable> tables = reader.ReadAllTables(header);

            for (int i = 0; i < tables.Count; i++)
            {
                ExportTableToTsv(tables[i], targetDirectory, i);
            }

            Console.WriteLine("Unpack complete!");
        }

        private static readonly char[] TsvSpecialChars = { '\t', '\n', '\r', '"' };

        private static string EscapeTsv(string value)
        {
            value ??= string.Empty;

            if (value.IndexOfAny(TsvSpecialChars) == -1)
                return value;

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private void ExportTableToTsv(ParamTable table, string targetDirectory, int tableIndex)
        {
            string tableFixidName = GlobalTextRegistry.ResolveForFilename(table.TableId);

            // 1. Check if the table is flagged in our schema override registry
            bool isFlagged = SchemaConfig.IsTableFlagged(table.TableId);
            string suffix = (isFlagged && !SchemaConfig.UseSchemaOverrides) ? "_READ_ONLY" : "";

            // 2. Append the suffix to the file name if it's read-only
            string fileName = $"Table_{tableIndex:D2}_{tableFixidName}{suffix}.tsv";
            string filePath = Path.Combine(targetDirectory, fileName);

            int physicalValueCount = table.Elements.Count > 0 ? table.Elements[0].Values.Count : 0;

            using var writer = new StreamWriter(filePath);

            if (isFlagged && !SchemaConfig.UseSchemaOverrides)
            {
                Console.WriteLine($"[READ ONLY] Some row(s) in Table_{tableIndex:D2}_{table.TableId} failed auto-schema mapping and will be displayed as uInt32.");
                Console.WriteLine("[DEBUG] Use --OverwriteSchema during unpacking and repacking to use manual overrides and allow modding this file");
                writer.WriteLine("# [READ ONLY] This table failed auto-schema mapping, the original table will be repacked.");
                writer.WriteLine("# Use --OverwriteSchema during unpacking and repacking to continue modding this file");
                writer.WriteLine("# This will allow the table to be modded, but the layout is not guaranteed");
            }

            var headerColumns = new List<string> { "ElementID" };

            for (int i = 0; i < physicalValueCount; i++)
            {
                if (i < table.Tags.Count)
                {
                    var tag = table.Tags[i];
                    string headerText = ExportWithResolvedFixids
                        ? GlobalTextRegistry.Resolve(tag.Id)
                        : tag.Id.ToString();

                    string tagName = TagRegistry.GetTagName(tag.Id);
                    if (!string.IsNullOrWhiteSpace(tagName))
                    {
                        headerText += $" [{tagName}]";
                    }

                    // Check Overrides First, then Schema Registry
                    string resolvedType = null;
                    if (SchemaConfig.UseSchemaOverrides && SchemaConfig.RegistryOverrides.TryGetValue((table.TableId, tag.Id), out var overrideType))
                    {
                        resolvedType = overrideType.ToString();
                        Console.WriteLine($"  -> [DEBUG] Used overide on {fileName} ({table.TableId})");
                    }
                    else if (SchemaRegistry.Table.TryGetValue((table.TableId, tag.Id), out var standardType))
                    {
                        resolvedType = standardType.ToString();
                    }

                    if (resolvedType != null)
                    {
                        headerText += $" <{resolvedType}>";
                    }

                    headerColumns.Add(headerText);
                }
                else
                {
                    headerColumns.Add($"[Unknown_Slot_{i}]");
                }
            }

            writer.WriteLine(string.Join("\t", headerColumns));

            foreach (var element in table.Elements)
            {
                var rowColumns = new List<string>
                {
                    ExportWithResolvedFixids
                        ? GlobalTextRegistry.Resolve(element.Id)
                        : element.Id.ToString()
                };

                for (int i = 0; i < element.Values.Count; i++)
                {
                    uint value = element.Values[i];

                    if (i < table.Tags.Count)
                    {
                        rowColumns.Add(
                            EscapeTsv(
                                ParambinDataAccess.FormatValue(
                                    table,
                                    table.Tags[i],
                                    value)));
                    }
                    else
                    {
                        rowColumns.Add(EscapeTsv(value.ToString()));
                    }
                }

                writer.WriteLine(string.Join("\t", rowColumns));
            }

            Console.WriteLine($"  -> Exported {fileName} ({table.ElementCount} rows)");
        }
    }
}