using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ForspokenBinTool.IO;
using ForspokenBinTool.Models;

namespace ForspokenBinTool.Core
{
    public class Repacker
    {
        public void Repack(string baseParambinPath, string tsvDirectory, string outputDirectory)
        {
            if (!File.Exists(baseParambinPath))
                throw new FileNotFoundException($"[ERROR] Base .parambin file not found: {baseParambinPath}");

            if (!Directory.Exists(tsvDirectory))
                throw new DirectoryNotFoundException($"[ERROR] TSV directory not found: {tsvDirectory}");

            Directory.CreateDirectory(outputDirectory);
            string outputFilePath = Path.Combine(outputDirectory, Path.GetFileName(baseParambinPath));

            Console.WriteLine($"Repacking {Path.GetFileName(baseParambinPath)} using TSVs from '{Path.GetFileName(tsvDirectory)}'...");
            Console.WriteLine($"Output target: {outputFilePath}");

            ArchiveHeader header;
            List<ParamTable> baseTables;

            using (var reader = new ParambinReader(baseParambinPath))
            {
                header = reader.ReadHeader();
                baseTables = reader.ReadAllTables(header);
            }

            using var fs = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write);
            using var bw = new BinaryWriter(fs);

            WriteHeader(bw, header);

            uint[] newTableOffsets = new uint[header.TableCount];

            for (int i = 0; i < baseTables.Count; i++)
            {
                ParamTable table = baseTables[i];
                bool isFlagged = SchemaConfig.IsTableFlagged(table.TableId);

                string baseName = $"Table_{i:D2}_{GlobalTextRegistry.ResolveForFilename(table.TableId)}";
                string[] matchingFiles = Directory.GetFiles(tsvDirectory, $"{baseName}*.tsv");
                string tsvPath = matchingFiles.FirstOrDefault();

                newTableOffsets[i] = (uint)(fs.Position - 0x20);

                if (tsvPath != null)
                {
                    string fileName = Path.GetFileName(tsvPath);
                    string firstLine = File.ReadLines(tsvPath).FirstOrDefault();

                    bool hasReadOnlyName = fileName.Contains("_READ_ONLY");
                    bool hasReadOnlyHeader = firstLine != null && firstLine.StartsWith("# [READ ONLY]");

                    if (hasReadOnlyName || hasReadOnlyHeader || (isFlagged && !SchemaConfig.UseSchemaOverrides))
                    {
                        Console.WriteLine($"[SKIPPING]: {fileName} [FLAGGED TABLE]");
                        Console.WriteLine($" -> Repacking Table {i} with the original data");
                        Console.WriteLine($"[DEBUG] Use --OverwriteSchema during unpack and repack to enable modding this file.");
                        WriteOriginalTable(bw, table);
                    }
                    else
                    {
                        Console.WriteLine($" -> Integrating modified TSV: {fileName}");
                        WriteRecompiledTable(bw, table, tsvPath);
                    }
                }
                else
                {
                    Console.WriteLine($"[MISSING TSV] Table {i} in {tsvDirectory}");
                    Console.WriteLine($" -> Repacking Table {i} with the original data");
                    WriteOriginalTable(bw, table);
                }
            }

            long finalLength = fs.Position;
            fs.Seek(0, SeekOrigin.Begin);
            header.Size = (int)finalLength;
            header.TableOffsets = newTableOffsets;
            WriteHeader(bw, header);

            Console.WriteLine(new string('-', 40));
            Console.WriteLine("Repack entirely complete!");
        }

        private void WriteHeader(BinaryWriter bw, ArchiveHeader header)
        {
            bw.BaseStream.Seek(0, SeekOrigin.Begin);

            byte[] idBytes = Encoding.UTF8.GetBytes(header.Identifier.PadRight(12, '\0'));
            bw.Write(idBytes);

            bw.Write(header.Size);
            bw.Write(header.ResourceId.Type);
            bw.Write(header.ResourceId.Primary);
            bw.Write(header.ResourceId.Secondary);
            bw.Write(header.ResourceId.Flag);

            bw.Write(header.Version);
            bw.Write(header.TableCount);

            foreach (var offset in header.TableOffsets)
            {
                bw.Write(offset);
            }
        }

        private void WriteOriginalTable(BinaryWriter bw, ParamTable table)
        {
            bw.Write(table.TableId);
            bw.Write((uint)table.Tags.Count);
            bw.Write((uint)table.Elements.Count);
            bw.Write(table.ElementSize);
            bw.Write(table.BooleanOffset);
            bw.Write(table.ArrayOffset);
            bw.Write(table.StringOffset);
            bw.Write(0);

            foreach (var tag in table.Tags)
            {
                bw.Write(tag.Id);
                bw.Write(tag.EngineFlag);
                bw.Write(tag.Offset);
            }

            foreach (var el in table.Elements)
            {
                bw.Write(el.Id);
                foreach (var val in el.Values)
                {
                    bw.Write(val);
                }
            }

            if (table.ArrayHeapRaw is { Length: > 0 })
                bw.Write(table.ArrayHeapRaw);

            if (table.StringHeapRaw is { Length: > 0 })
                bw.Write(table.StringHeapRaw);
        }

        private void WriteRecompiledTable(BinaryWriter bw, ParamTable baseTable, string tsvPath)
        {
            var lines = ParseTsvLines(tsvPath);
            if (lines.Count < 1) return;

            using var arrayHeap = new MemoryStream();
            using var stringHeap = new MemoryStream();

            stringHeap.WriteByte(0x00);

            var newElements = new List<ParamElement>();
            uint valueCount = baseTable.ElementSize >= 4 ? (baseTable.ElementSize / 4) - 1 : 0;

            var headerRow = lines[0];
            var columnTags = new ParamTag[valueCount];

            for (int v = 0; v < valueCount; v++)
            {
                if (v + 1 < headerRow.Length)
                {
                    string colHeader = headerRow[v + 1];
                    int spaceIdx = colHeader.IndexOf(' ');
                    string idStr = spaceIdx > 0 ? colHeader[..spaceIdx] : colHeader;

                    if (uint.TryParse(idStr, out uint tagId))
                    {
                        columnTags[v] = baseTable.Tags.FirstOrDefault(t => t.Id == tagId);
                    }
                }
            }

            for (int i = 1; i < lines.Count; i++)
            {
                var row = lines[i];
                if (row.Length == 0 || string.IsNullOrWhiteSpace(row[0])) continue;

                var element = new ParamElement();

                string idString = row[0];
                int spaceIndex = idString.IndexOf(' ');
                element.Id = uint.Parse(spaceIndex > 0 ? idString[..spaceIndex] : idString);

                for (int v = 0; v < valueCount; v++)
                {
                    var tag = columnTags[v];

                    string cellValue = (v + 1 < row.Length) ? row[v + 1] : "0";
                    uint encodedValue = 0;

                    if (tag != null)
                    {
                        encodedValue = ParambinDataEncoder.EncodeValue(baseTable, tag, cellValue, arrayHeap, stringHeap);
                    }
                    else
                    {
                        uint.TryParse(cellValue, out encodedValue);
                    }

                    element.Values.Add(encodedValue);
                }
                newElements.Add(element);
            }

            bw.Write(baseTable.TableId);
            bw.Write((uint)baseTable.Tags.Count);
            bw.Write((uint)newElements.Count);
            bw.Write(baseTable.ElementSize);

            uint elementsByteSize = (uint)(newElements.Count * baseTable.ElementSize);
            uint tagsByteSize = (uint)(baseTable.Tags.Count * 8);

            uint arrayOffset = tagsByteSize + elementsByteSize;
            uint stringOffset = arrayOffset + (uint)arrayHeap.Length;

            bw.Write(baseTable.BooleanOffset);
            bw.Write(arrayOffset);
            bw.Write(stringOffset);
            bw.Write(0);

            foreach (var tag in baseTable.Tags)
            {
                bw.Write(tag.Id);
                bw.Write(tag.EngineFlag);
                bw.Write(tag.Offset);
            }

            foreach (var el in newElements)
            {
                bw.Write(el.Id);
                foreach (var val in el.Values)
                {
                    bw.Write(val);
                }
            }

            bw.Write(arrayHeap.ToArray());
            bw.Write(stringHeap.ToArray());
        }

        private List<string[]> ParseTsvLines(string path)
        {
            var result = new List<string[]>();
            string content = File.ReadAllText(path, Encoding.UTF8);

            var currentRow = new List<string>();
            var currentCell = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < content.Length; i++)
            {
                char c = content[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < content.Length && content[i + 1] == '"')
                    {
                        currentCell.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == '\t' && !inQuotes)
                {
                    currentRow.Add(currentCell.ToString());
                    currentCell.Clear();
                }
                else if ((c == '\n' || c == '\r') && !inQuotes)
                {
                    if (c == '\r' && i + 1 < content.Length && content[i + 1] == '\n')
                    {
                        i++;
                    }
                    currentRow.Add(currentCell.ToString());
                    result.Add(currentRow.ToArray());

                    currentRow.Clear();
                    currentCell.Clear();
                }
                else
                {
                    currentCell.Append(c);
                }
            }

            if (currentCell.Length > 0 || currentRow.Count > 0)
            {
                currentRow.Add(currentCell.ToString());
                result.Add(currentRow.ToArray());
            }

            return result;
        }
    }
}